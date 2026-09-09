using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct ItemData
{
    public string itemName;
    public string itemTag;
    public Sprite itemSprite;
    [TextArea] public string description;
}

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform groundCheck;

    [Header("Движение")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float airControlMultiplier = 0.4f;

    [Header("Прыжок")]
    [SerializeField] private bool canJump = true;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float extraGravity = 12f;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Обзор мышью")]
    [SerializeField] private float mouseSensitivity = 2.5f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;
    [SerializeField] private bool lockCursor = true;

    [Header("Спринт / Стамина")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 22f;
    [SerializeField] private float staminaRegenRate = 14f;
    [SerializeField] private float staminaRegenDelay = 1f;
    [SerializeField] private float minStaminaToSprintAgain = 15f;

    [Header("FOV при беге")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float sprintFOV = 70f;
    [SerializeField] private float fovChangeSpeed = 8f;

    [Header("Head Bobbing")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private float walkBobSpeed = 8f;
    [SerializeField] private float walkBobAmount = 0.045f;
    [SerializeField] private float sprintBobSpeed = 13f;
    [SerializeField] private float sprintBobAmount = 0.09f;
    [SerializeField] private float bobSmoothing = 9f;

    [Header("Прицел (точка в центре экрана)")]
    [SerializeField] private bool showCrosshair = true;
    [SerializeField] private float crosshairSize = 5f;
    [SerializeField] private Color crosshairColor = Color.white;

    [Header("Полоса стамины")]
    [SerializeField] private bool showStaminaBar = true;
    [SerializeField] private bool showOnlyWhenSprinting = true;
    [SerializeField] private string staminaLabelText = "STAMINA";
    [SerializeField] private int staminaLabelFontSize = 14;
    [SerializeField] private Vector2 staminaBarSize = new Vector2(200f, 16f);
    [SerializeField] private float labelSpacing = 10f;
    [SerializeField] private float staminaBarBottomOffset = 40f;
    [SerializeField] private float borderWidth = 2f;
    [SerializeField] private float indicatorLineWidth = 3f;
    [SerializeField] private Color staminaUiColor = Color.white;

    [Header("UI / Escape Menu")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Slider gammaSlider;
    [SerializeField] private Slider fpsSlider;
    [SerializeField] private Slider soundSlider;
    [SerializeField] private Slider sensitivitySlider;

    [Header("Инвентарь (TMP)")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image[] slotImages;
    [SerializeField] private ItemData[] itemDatabase;
    [SerializeField] private float interactDistance = 3f;

    [Header("Тест Инвентаря")]
    [SerializeField] private bool giveTestItem;
    [SerializeField] private string testItemTag;

    private Rigidbody rb;
    private float pitch;
    private float yaw;
    private bool grounded;
    private bool jumpRequested;

    private float currentStamina;
    private bool isSprinting;
    private bool sprintLocked;
    private float regenTimer;

    private float bobTimer;
    private Vector3 cameraInitialLocalPos;

    private Texture2D whiteTex;
    private Texture2D dotTex;
    private bool isPaused;
    private bool isInventoryOpen;

    private float deltaTime;

    private int[] inventorySlots;
    private string lookAtItemName = "";
    private GameObject lookAtObject = null;
    private int lookAtItemDbIndex = -1;

    public float StaminaNormalized => maxStamina > 0f ? currentStamina / maxStamina : 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        currentStamina = maxStamina;
        yaw = transform.eulerAngles.y;

        if (cameraTransform != null)
            cameraInitialLocalPos = cameraTransform.localPosition;

        if (playerCamera != null)
            playerCamera.fieldOfView = normalFOV;

        SetCursorState(true);

        whiteTex = new Texture2D(1, 1);
        whiteTex.SetPixel(0, 0, Color.white);
        whiteTex.Apply();

        dotTex = CreateDotTexture(16);

        inventorySlots = new int[slotImages.Length];
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            inventorySlots[i] = -1;
            if (slotImages[i] != null)
            {
                slotImages[i].sprite = null;
                slotImages[i].color = Color.black;
            }
        }

        SetupUI();
    }

    private void SetupUI()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        if (descriptionText != null) descriptionText.text = "";

        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(LoadMainMenu);
        if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(CloseSettings);

        if (fpsSlider != null)
        {
            fpsSlider.minValue = 0f;
            fpsSlider.maxValue = 144f;
            fpsSlider.wholeNumbers = true;
            fpsSlider.value = 60f;
            fpsSlider.onValueChanged.AddListener(OnFPSChanged);
            OnFPSChanged(fpsSlider.value);
        }

        if (gammaSlider != null)
        {
            gammaSlider.minValue = 0f;
            gammaSlider.maxValue = 2f;
            gammaSlider.value = 1f;
            gammaSlider.onValueChanged.AddListener(OnGammaChanged);
            OnGammaChanged(gammaSlider.value);
        }

        if (soundSlider != null)
        {
            soundSlider.minValue = 0f;
            soundSlider.maxValue = 1f;
            soundSlider.value = AudioListener.volume;
            soundSlider.onValueChanged.AddListener(OnSoundChanged);
            OnSoundChanged(soundSlider.value);
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 0.1f;
            sensitivitySlider.maxValue = 10f;
            sensitivitySlider.value = mouseSensitivity;
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            OnSensitivityChanged(sensitivitySlider.value);
        }
    }

    private void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        if (giveTestItem)
        {
            giveTestItem = false;
            int dbIndex = GetItemDbIndexByTag(testItemTag);
            if (dbIndex != -1) AddItemToInventory(dbIndex);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isInventoryOpen)
            {
                ToggleInventory();
            }
            else if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                TogglePause();
            }
        }

        if (!isPaused && Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        if (isPaused || isInventoryOpen) return;

        HandleMouseLook();
        HandleSprintState();
        HandleStamina();
        HandleHeadBob();
        HandleFOV();
        HandleInteraction();

        if (canJump && Input.GetButtonDown("Jump"))
            jumpRequested = true;
    }

    private void FixedUpdate()
    {
        if (isPaused || isInventoryOpen) return;

        grounded = CheckGrounded();
        Move();

        if (jumpRequested && grounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
        jumpRequested = false;

        rb.AddForce(Vector3.up * -extraGravity, ForceMode.Acceleration);
    }

    private void HandleInteraction()
    {
        lookAtItemName = "";
        lookAtObject = null;
        lookAtItemDbIndex = -1;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, interactDistance))
        {
            string hitTag = hit.collider.tag;
            int dbIndex = GetItemDbIndexByTag(hitTag);

            if (dbIndex != -1)
            {
                lookAtItemName = itemDatabase[dbIndex].itemName;
                lookAtObject = hit.collider.gameObject;
                lookAtItemDbIndex = dbIndex;

                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (AddItemToInventory(lookAtItemDbIndex))
                    {
                        Destroy(lookAtObject);
                        lookAtItemName = "";
                    }
                }
            }
        }
    }

    private int GetItemDbIndexByTag(string tagToFind)
    {
        for (int i = 0; i < itemDatabase.Length; i++)
        {
            if (itemDatabase[i].itemTag == tagToFind)
                return i;
        }
        return -1;
    }

    private bool AddItemToInventory(int dbIndex)
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] == -1)
            {
                inventorySlots[i] = dbIndex;
                if (slotImages[i] != null)
                {
                    slotImages[i].sprite = itemDatabase[dbIndex].itemSprite;
                    slotImages[i].color = Color.white;
                }
                return true;
            }
        }
        return false;
    }

    public void OnInventorySlotClicked(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < inventorySlots.Length)
        {
            int dbIndex = inventorySlots[slotIndex];
            if (dbIndex != -1 && descriptionText != null)
            {
                descriptionText.text = itemDatabase[dbIndex].description;
            }
            else if (descriptionText != null)
            {
                descriptionText.text = "";
            }
        }
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        if (isInventoryOpen)
        {
            Time.timeScale = 0f;
            if (inventoryPanel != null) inventoryPanel.SetActive(true);
            if (descriptionText != null) descriptionText.text = "";
            SetCursorState(false);
        }
        else
        {
            Time.timeScale = 1f;
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            SetCursorState(true);
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            SetCursorState(false);
        }
        else
        {
            ResumeGame();
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        SetCursorState(true);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(1);
    }

    private void OnFPSChanged(float value)
    {
        QualitySettings.vSyncCount = 0;
        int fps = Mathf.RoundToInt(value);
        Application.targetFrameRate = (fps <= 0) ? -1 : fps;
    }

    private void OnGammaChanged(float value)
    {
        RenderSettings.ambientIntensity = value;
        RenderSettings.reflectionIntensity = value;
    }

    private void OnSoundChanged(float value)
    {
        AudioListener.volume = value;
    }

    private void OnSensitivityChanged(float value)
    {
        mouseSensitivity = value;
    }

    private void SetCursorState(bool isLocked)
    {
        if (lockCursor)
        {
            Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isLocked;
        }
    }

    private bool CheckGrounded()
    {
        Vector3 origin = groundCheck != null ? groundCheck.position : transform.position;
        return Physics.CheckSphere(origin, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = transform.right * h + transform.forward * v;
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
        Vector3 targetVelocity = inputDir * targetSpeed;

        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 velocityDiff = targetVelocity - new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        float control = grounded ? 1f : airControlMultiplier;
        Vector3 force = velocityDiff * acceleration * control;

        rb.AddForce(new Vector3(force.x, 0f, force.z), ForceMode.Acceleration);
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleSprintState()
    {
        bool hasMoveInput = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f;

        if (sprintLocked && currentStamina >= minStaminaToSprintAgain)
            sprintLocked = false;

        isSprinting = grounded && hasMoveInput && !sprintLocked && currentStamina > 0f && Input.GetKey(sprintKey);
    }

    private void HandleStamina()
    {
        if (isSprinting)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            regenTimer = 0f;

            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                sprintLocked = true;
            }
        }
        else
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= staminaRegenDelay)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }
        }
    }

    private void HandleHeadBob()
    {
        if (!enableHeadBob || cameraTransform == null) return;

        bool isMoving = grounded && (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f);

        Vector3 targetLocalPos;

        if (isMoving)
        {
            float speed = isSprinting ? sprintBobSpeed : walkBobSpeed;
            float amount = isSprinting ? sprintBobAmount : walkBobAmount;

            bobTimer += Time.deltaTime * speed;

            float offsetY = Mathf.Sin(bobTimer) * amount;
            float offsetX = Mathf.Cos(bobTimer * 0.5f) * amount * 0.5f;

            targetLocalPos = cameraInitialLocalPos + new Vector3(offsetX, offsetY, 0f);
        }
        else
        {
            bobTimer = 0f;
            targetLocalPos = cameraInitialLocalPos;
        }

        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetLocalPos, Time.deltaTime * bobSmoothing);
    }

    private void HandleFOV()
    {
        if (playerCamera == null) return;

        float targetFOV = isSprinting ? sprintFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovChangeSpeed);
    }

    private Texture2D CreateDotTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01(radius - dist);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return tex;
    }

    private void OnGUI()
    {
        if (isPaused || isInventoryOpen) return;

        DrawCrosshair();
        DrawStaminaBar();
        DrawFPSCounter();
        DrawInteractionText();
    }

    private void DrawInteractionText()
    {
        if (!string.IsNullOrEmpty(lookAtItemName))
        {
            GUIStyle interactStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22, // Размер шрифта
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            interactStyle.normal.textColor = Color.white;

            string text = $"{lookAtItemName} - [E]";
            Vector2 size = interactStyle.CalcSize(new GUIContent(text));

            // Ставим по центру экрана по X и смещаем чуть ниже центра по Y
            float x = (Screen.width - size.x) / 2f;
            float y = (Screen.height / 2f) + (crosshairSize / 2f) + 30f;

            GUI.Label(new Rect(x, y, size.x, size.y), text, interactStyle);
        }
    }

    private void DrawFPSCounter()
    {
        float currentFPS = 1f / deltaTime;
        GUIStyle fpsStyle = new GUIStyle();
        fpsStyle.fontSize = 16;
        fpsStyle.fontStyle = FontStyle.Bold;
        fpsStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        fpsStyle.alignment = TextAnchor.UpperRight;

        GUI.Label(new Rect(Screen.width - 110, 10, 100, 30), $"FPS: {Mathf.RoundToInt(currentFPS)}", fpsStyle);
    }

    private void DrawCrosshair()
    {
        if (!showCrosshair || dotTex == null) return;

        Rect rect = new Rect(
            Screen.width / 2f - crosshairSize / 2f,
            Screen.height / 2f - crosshairSize / 2f,
            crosshairSize,
            crosshairSize);

        GUI.color = crosshairColor;
        GUI.DrawTexture(rect, dotTex);
        GUI.color = Color.white;
    }

    private void DrawStaminaBar()
    {
        if (!showStaminaBar) return;
        if (showOnlyWhenSprinting && !isSprinting && currentStamina >= maxStamina) return;

        float barWidth = staminaBarSize.x;
        float barHeight = staminaBarSize.y;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = staminaLabelFontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        labelStyle.normal.textColor = staminaUiColor;

        Vector2 labelSize = labelStyle.CalcSize(new GUIContent(staminaLabelText));

        float totalWidth = labelSize.x + labelSpacing + barWidth;
        float startX = (Screen.width - totalWidth) / 2f;

        float y = Screen.height - staminaBarBottomOffset - barHeight;
        float labelX = startX;
        float barX = labelX + labelSize.x + labelSpacing;

        Rect labelRect = new Rect(labelX, y + (barHeight - labelSize.y) / 2f, labelSize.x, labelSize.y);
        GUI.Label(labelRect, staminaLabelText, labelStyle);

        DrawOutline(new Rect(barX, y, barWidth, barHeight), staminaUiColor, borderWidth);

        float normalizedOffset = 1f - StaminaNormalized;
        float innerWidth = barWidth - (borderWidth * 2f) - indicatorLineWidth;
        float lineX = barX + borderWidth + (normalizedOffset * innerWidth);

        DrawRect(new Rect(lineX, y + borderWidth, indicatorLineWidth, barHeight - (borderWidth * 2f)), staminaUiColor);
    }

    private void DrawOutline(Rect rect, Color color, float width)
    {
        DrawRect(new Rect(rect.x, rect.y, rect.width, width), color);
        DrawRect(new Rect(rect.x, rect.y + rect.height - width, rect.width, width), color);
        DrawRect(new Rect(rect.x, rect.y, width, rect.height), color);
        DrawRect(new Rect(rect.x + rect.width - width, rect.y, width, rect.height), color);
    }

    private void DrawRect(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, whiteTex);
        GUI.color = Color.white;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = groundCheck != null ? groundCheck.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, groundCheckRadius);
    }
}