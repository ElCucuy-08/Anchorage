using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

/// <summary>
/// Добавляет лампам мягкое свечение (ореол) и случайное мерцание.
/// Компонент автоматически вешается в рантайме на все источники света,
/// у которых есть плафон с материалом M_Light / M_LampGlow (фонари, прожекторы, вышки).
/// </summary>
[DisallowMultipleComponent]
public class LampGlow : MonoBehaviour
{
    [Header("Свечение")]
    [Tooltip("Диаметр ореола в мировых единицах.")]
    public float glowDiameter = 4.5f;

    [Range(0f, 1f)]
    [Tooltip("Прозрачность ореола.")]
    public float glowAlpha = 0.65f;

    [Tooltip("Оттенок ореола (умножается на цвет лампы).")]
    public Color glowTint = Color.white;

    [Header("Мерцание")]
    [Tooltip("Мерцает ли эта лампа.")]
    public bool flicker;

    [Range(0f, 1f)]
    [Tooltip("Вероятность того, что лампа будет мерцать.")]
    public float flickerChance = 0.35f;

    Light _light;
    Renderer _shade;
    SpriteRenderer _glow;
    MaterialPropertyBlock _mpb;
    Camera _camera;

    Color _glowColor;
    Color _baseEmission = Color.black;
    float _baseIntensity = 1f;

    float _level = 1f;
    float _target = 1f;
    bool _burst;
    float _idleTimer;
    float _burstTimer;
    float _stepTimer;
    System.Random _rng;

    static Sprite _glowSprite;

    public bool IsInitialized => _light != null;

    /// <summary>Вызывается бутстрапом после создания/назначения компонента.</summary>
    public void Setup(Light source, Renderer shade)
    {
        _light = source;
        _shade = shade;
        _baseIntensity = source != null ? source.intensity : 1f;
        _mpb = new MaterialPropertyBlock();

        PrepareEmission(shade);

        int seed = StableSeed(transform.position, transform);
        flicker = ((seed & 0x7fffffff) % 100) < Mathf.RoundToInt(flickerChance * 100f);
        _rng = new System.Random(seed ^ Environment.TickCount);

        _glowColor = glowTint * (source != null ? source.color : Color.white);
        _glowColor.a = glowAlpha;

        CreateGlow();
        Apply(1f);

        if (flicker)
            _idleTimer = Range(1.5f, 8f);
    }

    void PrepareEmission(Renderer shade)
    {
        if (shade == null) return;

        Color emission = Color.black;
        var materials = shade.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat == null || !mat.HasProperty("_EmissionColor")) continue;

            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            Color current = mat.GetColor("_EmissionColor");
            if (current.maxColorComponent > emission.maxColorComponent)
                emission = current;
        }

        if (emission.maxColorComponent < 0.01f)
            emission = new Color(1.25f, 1f, 0.65f, 1f);

        _baseEmission = emission;
    }

    void CreateGlow()
    {
        if (_light == null) return;
        if (_glowSprite == null) _glowSprite = BuildGlowSprite();

        var go = new GameObject("Lamp Glow");
        go.transform.SetParent(_light.transform, true);
        go.transform.position = _shade != null ? _shade.bounds.center : _light.transform.position;

        _glow = go.AddComponent<SpriteRenderer>();
        _glow.sprite = _glowSprite;
        _glow.color = _glowColor;
        _glow.shadowCastingMode = ShadowCastingMode.Off;
        _glow.receiveShadows = false;
        _glow.lightProbeUsage = LightProbeUsage.Off;
        _glow.reflectionProbeUsage = ReflectionProbeUsage.Off;
        _glow.sortingOrder = 1;

        // Спрайт создаётся размером 1x1 мировых единиц, поэтому scale = диаметр.
        Vector3 parentScale = _light.transform.lossyScale;
        float diameter = Mathf.Max(0.5f, glowDiameter);
        go.transform.localScale = new Vector3(
            diameter / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
            diameter / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)),
            diameter / Mathf.Max(0.0001f, Mathf.Abs(parentScale.z)));
    }

    void Update()
    {
        if (_light == null) return;

        if (_glow != null)
            _glow.enabled = _light.enabled;

        if (!flicker)
        {
            if (_level != 1f)
            {
                _level = 1f;
                Apply(1f);
            }
            return;
        }

        float dt = Time.deltaTime;

        if (_burst)
        {
            _burstTimer -= dt;
            _stepTimer -= dt;

            if (_stepTimer <= 0f)
            {
                _stepTimer = Range(0.03f, 0.13f);

                double r = _rng.NextDouble();
                if (r < 0.12)
                    _target = 0f;                          // короткое полное погасание
                else if (r < 0.45)
                    _target = Range(0.20f, 0.50f);         // сильная просадка
                else if (r < 0.75)
                    _target = Range(0.55f, 0.80f);         // заметное дрожание
                else
                    _target = Range(0.85f, 1f);            // лёгкое подрагивание
            }

            if (_burstTimer <= 0f)
            {
                _burst = false;
                _target = 1f;
                _idleTimer = Range(2.5f, 9f);
            }
        }
        else
        {
            _idleTimer -= dt;
            if (_idleTimer <= 0f)
            {
                _burst = true;
                _burstTimer = Range(0.25f, 1.5f);
                _stepTimer = 0f;
                _target = 1f;
            }
        }

        float speed = _target < _level ? 24f : 9f;
        _level = Mathf.MoveTowards(_level, _target, dt * speed);
        Apply(_level);
    }

    void Apply(float level)
    {
        level = Mathf.Clamp01(level);

        if (_light != null)
            _light.intensity = _baseIntensity * level;

        if (_glow != null)
        {
            Color c = _glowColor;
            c.a = glowAlpha * level;
            _glow.color = c;
        }

        if (_shade != null && _mpb != null)
        {
            Color e = _baseEmission * level;
            e.a = _baseEmission.a;
            _mpb.SetColor("_EmissionColor", e);
            _shade.SetPropertyBlock(_mpb);
        }
    }

    void LateUpdate()
    {
        if (_glow == null) return;

        // Спрайт всегда разворачиваем к камере, чтобы ореол было видно с любого ракурса.
        if (_camera == null) _camera = ResolveCamera();
        if (_camera != null)
            _glow.transform.rotation = _camera.transform.rotation;
    }

    static Camera ResolveCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
            cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
        return cam;
    }

    float Range(float min, float max) => min + (float)_rng.NextDouble() * (max - min);

    static int StableSeed(Vector3 p, Transform t)
    {
        unchecked
        {
            int x = Mathf.RoundToInt(p.x * 10f);
            int y = Mathf.RoundToInt(p.y * 10f);
            int z = Mathf.RoundToInt(p.z * 10f);
            int h = (x * 73856093) ^ (y * 19349663) ^ (z * 83492791);

            // Добавляем путь в иерархии: если несколько ламп стоят в одной точке
            // (например, в старой сцене), они всё равно мерцают независимо.
            int depth = 0;
            while (t != null && depth < 32)
            {
                h = (h * 397) ^ HashString(t.gameObject.name);
                h = (h * 397) ^ t.GetSiblingIndex();
                t = t.parent;
                depth++;
            }

            // Финальное перемешивание: соседние лампы не должны получать похожие seed.
            h ^= h >> 16;
            h *= 0x7feb352d;
            h ^= h >> 15;
            h *= unchecked((int)0x846ca68b);
            h ^= h >> 16;

            return h;
        }
    }

    static int HashString(string s)
    {
        unchecked
        {
            int h = 17;
            for (int i = 0; i < s.Length; i++)
                h = h * 31 + s[i];
            return h;
        }
    }

    static Sprite BuildGlowSprite()
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "LampGlowTexture",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                float falloff = Mathf.Clamp01(1f - d);
                falloff = falloff * falloff * (3f - 2f * falloff);

                float core = Mathf.Clamp01(1f - d * 3.2f);
                float a = Mathf.Clamp01(falloff * 0.7f + core * 0.7f);

                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, true);

        var sprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size,
            0,
            SpriteMeshType.FullRect);
        sprite.name = "LampGlowSprite";
        return sprite;
    }
}

/// <summary>
/// Находит лампы в загруженной сцене и вешает на них LampGlow.
/// </summary>
[Preserve]
public static class LampGlowBootstrap
{
    static bool _subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        if (!_subscribed)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            _subscribed = true;
        }

        ApplyToScene();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyToScene();

    static void ApplyToScene()
    {
        Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light == null) continue;
            if (light.type != LightType.Point && light.type != LightType.Spot) continue;

            LampGlow lamp = light.GetComponent<LampGlow>();
            if (lamp != null && lamp.IsInitialized) continue;

            Renderer shade = FindShade(light);
            if (shade == null) continue;

            if (lamp == null)
                lamp = light.gameObject.AddComponent<LampGlow>();

            lamp.Setup(light, shade);
        }
    }

    static Renderer FindShade(Light light)
    {
        // Свет может стоять и на самой лампе, и на дочернем LightSource.
        Renderer shade = FindShadeIn(light.transform);
        if (shade == null && light.transform.parent != null)
            shade = FindShadeIn(light.transform.parent);
        return shade;
    }

    static Renderer FindShadeIn(Transform root)
    {
        Renderer fallback = null;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;

            Material[] materials = r.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                Material mat = materials[m];
                if (mat != null &&
                    (mat.name.StartsWith("M_Light", StringComparison.OrdinalIgnoreCase) ||
                     mat.name.StartsWith("M_LampGlow", StringComparison.OrdinalIgnoreCase)))
                    return r;
            }

            string n = r.gameObject.name;
            if (n == "Lamp" || n == "Head" || n == "Spotlight")
                fallback = r;
        }

        // Страховка: не вешаем свечение на что попало (например, на фонарик игрока).
        string rootName = root.name;
        bool looksLikeLamp =
            rootName.IndexOf("Lamp", StringComparison.OrdinalIgnoreCase) >= 0 ||
            rootName.IndexOf("Floodlight", StringComparison.OrdinalIgnoreCase) >= 0 ||
            rootName.IndexOf("Watchtower", StringComparison.OrdinalIgnoreCase) >= 0;

        return looksLikeLamp ? fallback : null;
    }
}
