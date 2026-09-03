using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Экран загрузки")]
    [SerializeField] private Canvas loadingScreenCanvas;
    [SerializeField] private Image loadingScreenBackground;
    [SerializeField] private float loadingScreenDuration = 5f;
    [SerializeField] private float fadeInDuration = 1.5f;

    [Header("Канвас меню")]
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private TextMeshProUGUI gameTitle;
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    [Header("Камера")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Vector3 targetCameraPosition = new Vector3(0, 2, 5);
    [SerializeField] private float cameraMoveDuration = 2f;

    [Header("Сцены")]
    [SerializeField] private int sceneIndex = 1;

    [Header("Освещение")]
    [SerializeField] private Light ambientLight;
    [SerializeField] private Color targetAmbientColor = Color.white;

    private TextMeshProUGUI loadingScreenText;
    private Image rotatingSquare;

    private void Start()
    {
        SetupUI();
        StartCoroutine(MenuLoadingSequence());
    }

    private void SetupUI()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitButtonClicked);

        if (gameTitle != null)
            gameTitle.text = "ANCOURAGE";

        if (menuCanvas != null)
            SetCanvasAlpha(menuCanvas, 0f);

        CreateLoadingScreenElements();
    }

    private void CreateLoadingScreenElements()
    {
        if (loadingScreenCanvas == null)
        {
            Debug.LogError("Loading Screen Canvas не назначен!");
            return;
        }

        if (loadingScreenBackground == null)
        {
            loadingScreenBackground = loadingScreenCanvas.GetComponent<Image>();
        }
        if (loadingScreenBackground == null)
        {
            loadingScreenBackground = loadingScreenCanvas.gameObject.AddComponent<Image>();
        }
        loadingScreenBackground.color = Color.black;

        GameObject textObject = new GameObject("LoadingText");
        textObject.transform.SetParent(loadingScreenCanvas.transform, false);
        loadingScreenText = textObject.AddComponent<TextMeshProUGUI>();

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchoredPosition = new Vector2(0, 100);
        textRect.sizeDelta = new Vector2(1000, 200);

        loadingScreenText.text = "BYTESCHOOL";
        loadingScreenText.fontSize = 60;
        loadingScreenText.alignment = TextAlignmentOptions.Center;
        loadingScreenText.color = new Color(1, 1, 1, 0);

        GameObject squareObject = new GameObject("RotatingSquare");
        squareObject.transform.SetParent(loadingScreenCanvas.transform, false);
        rotatingSquare = squareObject.AddComponent<Image>();

        RectTransform squareRect = squareObject.GetComponent<RectTransform>();
        squareRect.anchoredPosition = new Vector2(0, -100);
        squareRect.sizeDelta = new Vector2(30, 30);

        rotatingSquare.color = Color.white;

        Texture2D squareTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        Color[] colors = new Color[32 * 32];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.white;
        squareTexture.SetPixels(colors);
        squareTexture.Apply();

        Sprite squareSprite = Sprite.Create(squareTexture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        rotatingSquare.sprite = squareSprite;
    }

    private IEnumerator MenuLoadingSequence()
    {
        yield return StartCoroutine(ShowLoadingScreen(loadingScreenDuration));
        yield return StartCoroutine(FadeOutLoadingScreen(fadeInDuration));
        yield return StartCoroutine(MoveCameraToTarget(cameraMoveDuration));

        if (cameraTarget != null)
        {
            StartCoroutine(OrbitCameraRoutine());
        }

        yield return StartCoroutine(FadeInMenuUI(1f));
    }

    private IEnumerator ShowLoadingScreen(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            if (loadingScreenText != null)
            {
                float textAlpha = Mathf.Clamp01(progress / 0.3f);
                Color textColor = loadingScreenText.color;
                textColor.a = textAlpha;
                loadingScreenText.color = textColor;
            }

            if (rotatingSquare != null)
            {
                rotatingSquare.transform.localEulerAngles = new Vector3(0, 0, -progress * 360f * 2);
            }

            yield return null;
        }
    }

    private IEnumerator FadeOutLoadingScreen(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            if (loadingScreenText != null)
            {
                Color textColor = loadingScreenText.color;
                textColor.a = Mathf.Lerp(1f, 0f, progress);
                loadingScreenText.color = textColor;
            }

            if (rotatingSquare != null)
            {
                Color squareColor = rotatingSquare.color;
                squareColor.a = Mathf.Lerp(1f, 0f, progress);
                rotatingSquare.color = squareColor;
            }

            if (loadingScreenBackground != null)
            {
                Color bgColor = loadingScreenBackground.color;
                bgColor.a = Mathf.Lerp(1f, 0f, progress);
                loadingScreenBackground.color = bgColor;
            }

            if (ambientLight != null)
            {
                RenderSettings.ambientLight = Color.Lerp(Color.black, targetAmbientColor, progress);
            }

            yield return null;
        }

        if (loadingScreenCanvas != null)
            loadingScreenCanvas.enabled = false;
    }

    private IEnumerator MoveCameraToTarget(float duration)
    {
        if (mainCamera == null)
            yield break;

        Vector3 startPosition = mainCamera.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float easeProgress = Mathf.SmoothStep(0f, 1f, progress);

            mainCamera.transform.position = Vector3.Lerp(startPosition, targetCameraPosition, easeProgress);

            if (cameraTarget != null)
                mainCamera.transform.LookAt(cameraTarget, Vector3.up);

            yield return null;
        }

        mainCamera.transform.position = targetCameraPosition;
        if (cameraTarget != null)
            mainCamera.transform.LookAt(cameraTarget, Vector3.up);
    }

    private IEnumerator OrbitCameraRoutine()
    {
        float orbitSpeed = 15f;
        float currentAngle = 0f;

        Vector3 initialOffset = targetCameraPosition - cameraTarget.position;
        float distance = new Vector3(initialOffset.x, 0, initialOffset.z).magnitude;
        float height = initialOffset.y;

        while (true)
        {
            if (cameraTarget != null)
            {
                currentAngle += orbitSpeed * Time.deltaTime;
                float rad = currentAngle * Mathf.Deg2Rad;

                float x = cameraTarget.position.x + Mathf.Sin(rad) * distance;
                float z = cameraTarget.position.z + Mathf.Cos(rad) * distance;
                float y = cameraTarget.position.y + height;

                mainCamera.transform.position = new Vector3(x, y, z);
                mainCamera.transform.LookAt(cameraTarget, Vector3.up);
            }
            yield return null;
        }
    }

    private IEnumerator FadeInMenuUI(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            if (menuCanvas != null)
                SetCanvasAlpha(menuCanvas, Mathf.Lerp(0f, 1f, progress));

            yield return null;
        }

        if (menuCanvas != null)
            SetCanvasAlpha(menuCanvas, 1f);
    }

    private void SetCanvasAlpha(Canvas canvas, float alpha)
    {
        if (canvas == null)
            return;

        CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = alpha;
    }

    private void OnPlayButtonClicked()
    {
        Debug.Log("Загружаем сцену с индексом: " + sceneIndex);
        SceneManager.LoadScene(sceneIndex);
    }

    private void OnQuitButtonClicked()
    {
        Debug.Log("Выход из игры");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}