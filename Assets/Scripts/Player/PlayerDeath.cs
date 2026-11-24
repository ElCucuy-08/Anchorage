using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityStandardAssets.Characters.FirstPerson;

/// <summary>
/// Смерть игрока: здоровье, реакция на врагов и процедурная анимация падения камеры.
/// Компонент автоматически вешается на объект с тегом Player (см. PlayerDeathBootstrap),
/// поэтому сцену менять не нужно.
/// </summary>
[DisallowMultipleComponent]
public class PlayerDeath : MonoBehaviour
{
    [Header("Здоровье")]
    [SerializeField] private float maxHealth = 100f;
    [Tooltip("Сколько здоровья у игрока в начале игры и после возрождения.")]
    [SerializeField] private float startingHealth = 50f;
    [SerializeField] private float spawnGraceTime = 1f;
    [SerializeField] private float respawnInvulnerability = 2.5f;
    [Tooltip("Пауза между ударами врагов, чтобы касание не снимало здоровье каждый кадр.")]
    [SerializeField] private float damageCooldown = 1f;
    [Tooltip("Сколько здоровья снимает касание врага.")]
    [SerializeField] private float enemyTouchDamage = 10f;

    [Header("Когда наступает смерть")]
    [Tooltip("Дистанция, на которой враг наносит игроку урон.")]
    [SerializeField] private float catchDistance = 1.8f;
    [SerializeField] private float enemyScanInterval = 0.2f;
    [SerializeField] private bool dieOnFall = true;
    [SerializeField] private float fallDeathHeight = -50f;

    [Header("Анимация падения")]
    [SerializeField] private float hitFreezeDuration = 0.14f;
    [SerializeField] private float collapseDuration = 0.95f;
    [SerializeField] private float cameraGroundOffset = 0.3f;
    [SerializeField] private float fallRoll = 82f;
    [SerializeField] private float fallPitch = 28f;
    [SerializeField] private float fovKick = 16f;
    [Range(0.05f, 1f)]
    [SerializeField] private float deathSlowMotion = 0.4f;

    [Header("Экран смерти")]
    [SerializeField] private string deathTitle = "ВЫ ПОГИБЛИ";
    [SerializeField] private string defaultDeathReason = "Вас обидели";
    [SerializeField] private string restartHint = "R или клик — попробовать снова";
    [SerializeField] private float damageFlashStrength = 0.4f;

    private float health;
    private bool isDead;
    private string deathReason;

    private Vector3 spawnPosition;
    private bool spawnCaptured;

    private Camera playerCamera;
    private Transform cameraTransform;
    private Transform cameraParent;
    private Vector3 cameraOriginalLocalPosition;
    private Quaternion cameraOriginalLocalRotation;
    private float cameraOriginalFov = 60f;
    private float cameraOriginalNearClip = 0.3f;

    private readonly List<MonoBehaviour> disabledPlayerScripts = new List<MonoBehaviour>();

    private float deathStartTime;
    private float invulnerableUntil;
    private float nextDamageTime;
    private float nextEnemyScan;
    private float damageFlash;
    private float healFlash;
    private Coroutine deathRoutine;

    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;

    private Texture2D vignetteTexture;
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle hintStyle;
    private GUIStyle healthStyle;

    public float Health => health;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        health = Mathf.Clamp(startingHealth, 1f, maxHealth);
        invulnerableUntil = Time.unscaledTime + spawnGraceTime;
        CaptureSpawnPoint();
    }

    private void Update()
    {
        if (damageFlash > 0f)
            damageFlash = Mathf.MoveTowards(damageFlash, 0f, Time.unscaledDeltaTime * 3f);

        if (healFlash > 0f)
            healFlash = Mathf.MoveTowards(healFlash, 0f, Time.unscaledDeltaTime * 3f);

        if (isDead)
        {
            // Небольшая задержка, чтобы случайное нажатие не пропустило анимацию.
            if (Time.unscaledTime - deathStartTime > 0.6f && RestartPressed())
                Respawn();

            return;
        }

        if (dieOnFall && transform.position.y <= fallDeathHeight)
        {
            Kill("Вы упали в бездну");
            return;
        }

        if (Time.unscaledTime < invulnerableUntil)
            return;

        if (Time.unscaledTime >= nextEnemyScan)
        {
            nextEnemyScan = Time.unscaledTime + enemyScanInterval;
            DetectEnemies();
        }
    }

    /// <summary>Наносит урон. Когда здоровье заканчивается — запускает смерть.</summary>
    public void TakeDamage(float amount, string reason = null)
    {
        if (isDead || Time.unscaledTime < invulnerableUntil || Time.unscaledTime < nextDamageTime)
            return;

        nextDamageTime = Time.unscaledTime + damageCooldown;
        health = Mathf.Max(0f, health - Mathf.Max(0f, amount));
        damageFlash = 1f;

        if (health <= 0f)
            Kill(reason);
    }

    /// <summary>Касание врага: снимает стандартный урон с общей паузой между ударами.</summary>
    public void TouchEnemy(string reason = "Вас поймали")
    {
        TakeDamage(enemyTouchDamage, reason);
    }

    /// <summary>Лечит игрока (например, за поднятую зажигалку).</summary>
    public void Heal(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        float before = health;
        health = Mathf.Min(maxHealth, health + amount);

        if (health > before)
            healFlash = 1f;
    }

    /// <summary>Мгновенно убивает игрока и запускает анимацию смерти.</summary>
    public void Kill(string reason = null)
    {
        if (isDead)
            return;

        isDead = true;
        health = 0f;
        deathReason = string.IsNullOrEmpty(reason) ? defaultDeathReason : reason;
        deathStartTime = Time.unscaledTime;

        Debug.Log("[PlayerDeath] Смерть: " + deathReason);

        deathRoutine = StartCoroutine(DeathRoutine());
    }

    /// <summary>Возвращает игрока на точку старта и убирает экран смерти.</summary>
    public void Respawn()
    {
        if (!isDead)
            return;

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        RestoreCamera();
        Time.timeScale = 1f;

        // Телепортируем игрока. Вращение не трогаем, чтобы не сбить MouseLook.
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        transform.position = spawnPosition;

        if (controller != null)
            controller.enabled = true;

        health = Mathf.Clamp(startingHealth, 1f, maxHealth);
        isDead = false;
        damageFlash = 0f;
        healFlash = 0f;
        invulnerableUntil = Time.unscaledTime + respawnInvulnerability;
        nextDamageTime = 0f;
        nextEnemyScan = 0f;

        EnablePlayerScripts();

        // Возвращаем управление курсором в то состояние, в котором игра была до смерти.
        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;

        Debug.Log("[PlayerDeath] Респавн на " + spawnPosition);
    }

    private void CaptureSpawnPoint()
    {
        if (spawnCaptured)
            return;

        spawnPosition = transform.position;
        spawnCaptured = true;
    }

    private void DetectEnemies()
    {
        NavMeshAgent[] agents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
        float catchSqrDistance = catchDistance * catchDistance;
        Vector3 myPosition = transform.position;

        for (int i = 0; i < agents.Length; i++)
        {
            NavMeshAgent agent = agents[i];

            if (agent == null || !agent.isActiveAndEnabled)
                continue;

            if (agent.transform == transform || agent.transform.IsChildOf(transform))
                continue;

            // Считаем полное расстояние, чтобы враг с другого этажа
            // не мог убить игрока через перекрытие.
            Vector3 delta = agent.transform.position - myPosition;

            if (delta.sqrMagnitude <= catchSqrDistance)
            {
                TouchEnemy();
                return;
            }
        }
    }

    private IEnumerator DeathRoutine()
    {
        CaptureSpawnPoint();

        // Замедляем время для кинематографичного падения.
        Time.timeScale = deathSlowMotion;

        DisablePlayerScripts();

        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PrepareCamera(out Vector3 cameraStartPosition, out Quaternion cameraStartRotation, out Vector3 groundPosition);
        float fovStart = cameraOriginalFov;

        float elapsed = 0f;
        float totalDuration = hitFreezeDuration + collapseDuration;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (playerCamera != null)
            {
                if (elapsed < hitFreezeDuration)
                {
                    // Резкая встряска в момент удара.
                    float strength = 1f - Mathf.Clamp01(elapsed / hitFreezeDuration);
                    cameraTransform.position = cameraStartPosition + Random.insideUnitSphere * (0.055f * strength);
                    cameraTransform.rotation = cameraStartRotation * Quaternion.Euler(
                        Random.Range(-1.8f, 1.8f) * strength,
                        Random.Range(-1.8f, 1.8f) * strength,
                        Random.Range(-2.5f, 2.5f) * strength);

                    playerCamera.fieldOfView = fovStart + fovKick * 0.45f * strength;
                }
                else
                {
                    float progress = Mathf.Clamp01((elapsed - hitFreezeDuration) / collapseDuration);

                    // Ускоренное падение к полу.
                    float fall = progress * progress;
                    Vector3 position = Vector3.Lerp(cameraStartPosition, groundPosition, fall);

                    // Небольшой отскок при касании пола.
                    if (progress > 0.82f)
                    {
                        float bounce = Mathf.Sin((progress - 0.82f) / 0.18f * Mathf.PI);
                        position += Vector3.up * (bounce * 0.07f);
                    }

                    // Боковой увод камеры.
                    position += cameraStartRotation * new Vector3(
                        Mathf.Sin(progress * 9f) * 0.05f * (1f - progress),
                        0f,
                        0f);

                    cameraTransform.position = position;

                    float smooth = Mathf.SmoothStep(0f, 1f, progress);
                    float roll = Mathf.Lerp(0f, fallRoll, smooth);
                    float pitch = Mathf.Lerp(0f, fallPitch, smooth);
                    float wobble = Mathf.Sin(progress * 14f) * 3f * (1f - progress);

                    cameraTransform.rotation = cameraStartRotation * Quaternion.Euler(pitch, wobble, roll);

                    playerCamera.fieldOfView = Mathf.Lerp(
                        fovStart + fovKick * 0.45f,
                        fovStart,
                        Mathf.SmoothStep(0f, 1f, progress));
                }
            }

            yield return null;
        }

        // Финальная поза: камера лежит на полу.
        if (playerCamera != null)
        {
            cameraTransform.position = groundPosition;
            cameraTransform.rotation = cameraStartRotation * Quaternion.Euler(fallPitch, 0f, fallRoll);
            playerCamera.fieldOfView = cameraOriginalFov;
        }

        deathRoutine = null;
    }

    private void PrepareCamera(out Vector3 startPosition, out Quaternion startRotation, out Vector3 groundPosition)
    {
        playerCamera = Camera.main;

        if (playerCamera == null)
        {
            startPosition = Vector3.zero;
            startRotation = Quaternion.identity;
            groundPosition = Vector3.zero;
            return;
        }

        cameraTransform = playerCamera.transform;
        cameraParent = cameraTransform.parent;
        cameraOriginalLocalPosition = cameraTransform.localPosition;
        cameraOriginalLocalRotation = cameraTransform.localRotation;
        cameraOriginalFov = playerCamera.fieldOfView;
        cameraOriginalNearClip = playerCamera.nearClipPlane;

        // Чтобы пол не подрезался, когда камера лежит на нём.
        playerCamera.nearClipPlane = 0.05f;

        startPosition = cameraTransform.position;
        startRotation = cameraTransform.rotation;
        groundPosition = FindGroundPosition(startPosition);

        // Отсоединяем камеру, чтобы она падала независимо от игрока.
        cameraTransform.SetParent(null, true);
    }

    private Vector3 FindGroundPosition(Vector3 cameraPosition)
    {
        // Запасной вариант: камера примерно на уровне колен/пола.
        Vector3 fallback = new Vector3(cameraPosition.x, transform.position.y - 0.7f, cameraPosition.z);

        RaycastHit[] hits = Physics.RaycastAll(
            cameraPosition + Vector3.up * 0.05f,
            Vector3.down,
            8f,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        float nearest = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider collider = hits[i].collider;

            if (collider == null ||
                collider.transform == transform ||
                collider.transform.IsChildOf(transform))
                continue;

            if (hits[i].distance < nearest)
            {
                nearest = hits[i].distance;
                fallback = hits[i].point + Vector3.up * cameraGroundOffset;
            }
        }

        return fallback;
    }

    private void RestoreCamera()
    {
        if (playerCamera == null || cameraTransform == null)
            return;

        cameraTransform.SetParent(cameraParent, false);
        cameraTransform.localPosition = cameraOriginalLocalPosition;
        cameraTransform.localRotation = cameraOriginalLocalRotation;
        playerCamera.fieldOfView = cameraOriginalFov;
        playerCamera.nearClipPlane = cameraOriginalNearClip;
    }

    private void DisablePlayerScripts()
    {
        disabledPlayerScripts.Clear();

        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
        {
            if (behaviour == null || behaviour == this || !behaviour.enabled)
                continue;

            // Отключаем только скрипты, управляющие игроком и инвентарём,
            // чтобы случайно не задеть NavMeshSurface и прочие системы.
            if (behaviour is FirstPersonController ||
                behaviour is Inventory ||
                behaviour is FlashlightToggle ||
                behaviour is PlayerMovement)
            {
                behaviour.enabled = false;
                disabledPlayerScripts.Add(behaviour);
            }
        }
    }

    private void EnablePlayerScripts()
    {
        for (int i = 0; i < disabledPlayerScripts.Count; i++)
        {
            if (disabledPlayerScripts[i] != null)
                disabledPlayerScripts[i].enabled = true;
        }

        disabledPlayerScripts.Clear();
    }

    // Любая клавиша или клик мышью: так рестарт сработает, даже если
    // R не дошла до Game view (например, фокус ушёл в редактор).
    private static bool RestartPressed()
    {
        return Input.anyKeyDown;
    }

    private void OnGUI()
    {
        EnsureGuiStyles();

        if (healFlash > 0f)
            DrawFullScreen(new Color(0.15f, 0.7f, 0.25f, healFlash * 0.3f));

        if (damageFlash > 0f)
            DrawFullScreen(new Color(0.55f, 0f, 0f, damageFlash * damageFlashStrength));

        if (!isDead)
        {
            DrawHealthBar();
            return;
        }

        EnsureVignetteTexture();

        float elapsed = Time.unscaledTime - deathStartTime;

        // Белая вспышка в момент удара.
        float hitFlash = 1f - Mathf.Clamp01(elapsed / 0.18f);
        if (hitFlash > 0f)
            DrawFullScreen(new Color(0.9f, 0.92f, 1f, hitFlash * 0.85f));

        // Красная пелена.
        float redFade = Mathf.Clamp01((elapsed - 0.05f) / 0.45f) *
                        (1f - Mathf.Clamp01((elapsed - 0.7f) / 1.3f));
        if (redFade > 0f)
            DrawFullScreen(new Color(0.45f, 0f, 0f, redFade * 0.5f));

        // Затемнение и виньетка.
        float fade = Mathf.Clamp01((elapsed - 0.35f) / 0.95f);
        if (fade > 0f)
        {
            DrawFullScreen(new Color(0f, 0f, 0f, fade * 0.82f));

            Color oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, fade);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), vignetteTexture, ScaleMode.StretchToFill);
            GUI.color = oldColor;
        }

        // Заголовок и подсказка.
        float titleAlpha = Mathf.Clamp01((elapsed - 0.75f) / 0.55f);
        if (titleAlpha > 0f)
        {
            DrawCenteredText(deathTitle, titleStyle, Screen.height * 0.41f,
                new Color(0.86f, 0.06f, 0.06f, titleAlpha));

            float subtitleAlpha = Mathf.Clamp01((elapsed - 1f) / 0.5f);
            if (subtitleAlpha > 0f)
            {
                DrawCenteredText(deathReason, subtitleStyle, Screen.height * 0.51f,
                    new Color(0.88f, 0.84f, 0.8f, subtitleAlpha));
            }

            float hintAlpha = Mathf.Clamp01((elapsed - 1.6f) / 0.6f) *
                              (0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 3f));

            if (hintAlpha > 0f)
            {
                DrawCenteredText(restartHint, hintStyle, Screen.height * 0.68f,
                    new Color(1f, 1f, 1f, hintAlpha * 0.8f));
            }
        }
    }

    private void DrawHealthBar()
    {
        float width = Mathf.Min(Screen.width * 0.22f, 260f);
        float height = Mathf.Max(10f, Screen.height * 0.016f);
        float x = Screen.width * 0.025f;
        float y = Screen.height * 0.035f;

        float fraction = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;
        Color fill = fraction > 0.5f
            ? new Color(0.35f, 0.78f, 0.36f)
            : fraction > 0.25f
                ? new Color(0.95f, 0.76f, 0.2f)
                : new Color(0.88f, 0.2f, 0.18f);

        Color oldColor = GUI.color;

        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(x - 2f, y - 2f, width + 4f, height + 4f), Texture2D.whiteTexture);

        GUI.color = fill;
        GUI.DrawTexture(new Rect(x, y, width * fraction, height), Texture2D.whiteTexture);

        GUI.color = oldColor;

        GUI.Label(new Rect(x, y + height * 1.4f, width, height * 2f),
            "ЗДОРОВЬЕ " + Mathf.CeilToInt(health) + " / " + Mathf.RoundToInt(maxHealth), healthStyle);
    }

    private void DrawFullScreen(Color color)
    {
        Color oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    private void DrawCenteredText(string text, GUIStyle style, float centerY, Color color)
    {
        Rect rect = new Rect(0f, centerY - style.fontSize, Screen.width, style.fontSize * 2f);

        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, color.a * 0.85f);
        GUI.Label(new Rect(rect.x + 3f, rect.y + 3f, rect.width, rect.height), text, style);

        GUI.color = color;
        GUI.Label(rect, text, style);
        GUI.color = oldColor;
    }

    private void EnsureGuiStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = Mathf.Max(26, Mathf.RoundToInt(Screen.height * 0.085f))
        };

        subtitleStyle = new GUIStyle(titleStyle)
        {
            fontStyle = FontStyle.Normal,
            fontSize = Mathf.Max(15, Mathf.RoundToInt(Screen.height * 0.03f))
        };

        hintStyle = new GUIStyle(titleStyle)
        {
            fontStyle = FontStyle.Normal,
            fontSize = Mathf.Max(13, Mathf.RoundToInt(Screen.height * 0.024f))
        };

        healthStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = Mathf.Max(12, Mathf.RoundToInt(Screen.height * 0.022f))
        };
        healthStyle.normal.textColor = new Color(0.92f, 0.92f, 0.9f);
    }

    private void EnsureVignetteTexture()
    {
        if (vignetteTexture != null)
            return;

        const int size = 256;
        vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "DeathVignette",
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxDistance = center.magnitude;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((distance - 0.32f) / 0.68f));
                pixels[y * size + x] = new Color32(0, 0, 0, (byte)(alpha * 255f));
            }
        }

        vignetteTexture.SetPixels32(pixels);
        vignetteTexture.Apply(false, false);
    }

    private void OnDestroy()
    {
        if (isDead)
            Time.timeScale = 1f;

        if (vignetteTexture != null)
            Destroy(vignetteTexture);
    }
}

/// <summary>
/// Автоматически добавляет PlayerDeath к игроку после загрузки сцены,
/// поэтому сцену не нужно редактировать вручную.
/// </summary>
public static class PlayerDeathBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttachToPlayer();
    }

    private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        AttachToPlayer();
    }

    private static void AttachToPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            return;

        if (player.GetComponent<PlayerDeath>() == null)
            player.AddComponent<PlayerDeath>();

        if (player.GetComponent<ObjectiveHud>() == null)
            player.AddComponent<ObjectiveHud>();
    }
}
