using UnityEngine;

/// <summary>
/// FPS-контроллер игрока.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("маленькие негритянские дети")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform groundCheck;

    [Header("шаги гиги")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float airControlMultiplier = 0.4f;

    [Header("Прыг")]
    [SerializeField] private bool canJump = true;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float extraGravity = 12f;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("мыш")]
    [SerializeField] private float mouseSensitivity = 2.5f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;
    [SerializeField] private bool lockCursor = true;

    [Header("Спринт")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 22f;
    [SerializeField] private float staminaRegenRate = 14f;
    [SerializeField] private float staminaRegenDelay = 1f;
    [SerializeField] private float minStaminaToSprintAgain = 15f;

    [Header("фов")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float sprintFOV = 70f;
    [SerializeField] private float fovChangeSpeed = 8f;

    [Header("пг")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private float walkBobSpeed = 8f;
    [SerializeField] private float walkBobAmount = 0.045f;
    [SerializeField] private float sprintBobSpeed = 13f;
    [SerializeField] private float sprintBobAmount = 0.09f;
    [SerializeField] private float bobSmoothing = 9f;

    [Header("Прицеу")]
    [SerializeField] private bool showCrosshair = true;
    [SerializeField] private float crosshairSize = 5f;
    [SerializeField] private Color crosshairColor = Color.white;

    [Header("стамина ебуча")]
    [SerializeField] private bool showStaminaBar = true;
    [Tooltip("Отображать шкалу только когда игрок бежит или стамина восстанавливается")]
    [SerializeField] private bool showOnlyWhenSprinting = true;
    [SerializeField] private string staminaLabelText = "STAMINA";
    [SerializeField] private int staminaLabelFontSize = 14;
    [SerializeField] private Vector2 staminaBarSize = new Vector2(200f, 16f);
    [SerializeField] private float labelSpacing = 10f;
    [SerializeField] private float staminaBarBottomOffset = 40f;
    [SerializeField] private float borderWidth = 2f;
    [SerializeField] private float indicatorLineWidth = 3f;
    [SerializeField] private Color staminaUiColor = Color.white;

    private Rigidbody rb;
    private float pitch;
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

    public float StaminaNormalized => maxStamina > 0f ? currentStamina / maxStamina : 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Start()
    {
        currentStamina = maxStamina;

        if (cameraTransform != null)
            cameraInitialLocalPos = cameraTransform.localPosition;

        if (playerCamera != null)
            playerCamera.fieldOfView = normalFOV;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        whiteTex = new Texture2D(1, 1);
        whiteTex.SetPixel(0, 0, Color.white);
        whiteTex.Apply();

        dotTex = CreateDotTexture(16);
    }

    private void Update()
    {
        if (lockCursor)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        HandleMouseLook();
        HandleSprintState();
        HandleStamina();
        HandleHeadBob();
        HandleFOV();

        if (canJump && Input.GetButtonDown("Jump"))
            jumpRequested = true;
    }

    private void FixedUpdate()
    {
        grounded = CheckGrounded();
        Move();

        if (jumpRequested && grounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
        jumpRequested = false;

        rb.AddForce(Vector3.up * -extraGravity, ForceMode.Acceleration);
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

        transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

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
        DrawCrosshair();
        DrawStaminaBar();
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
        DrawRect(new Rect(rect.x, rect.y, rect.width, width), color); // Верх
        DrawRect(new Rect(rect.x, rect.y + rect.height - width, rect.width, width), color); // Низ
        DrawRect(new Rect(rect.x, rect.y, width, rect.height), color); // Лево
        DrawRect(new Rect(rect.x + rect.width - width, rect.y, width, rect.height), color); // Право
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