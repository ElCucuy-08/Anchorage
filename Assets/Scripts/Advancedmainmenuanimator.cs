using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AdvancedMainMenuAnimator : MonoBehaviour
{
    [Header("Анимация загрузки")]
    [SerializeField] private Image loadingScreenImage;
    [SerializeField] private Text loadingScreenText;
    [SerializeField] private CanvasGroup loadingScreenCanvasGroup;

    [Header("Параметры времени")]
    [SerializeField] private float showLoadingScreenTime = 2f;
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float cameraMoveDuration = 2f;
    [SerializeField] private float menuFadeInDuration = 1f;

    [Header("Камера")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Vector3 startCameraPosition = new Vector3(0, 5, -10);
    [SerializeField] private Vector3 targetCameraPosition = new Vector3(0, 2, 5);

    [Header("UI Элементы")]
    [SerializeField] private CanvasGroup menuCanvasGroup;
    [SerializeField] private CanvasGroup titleCanvasGroup;

    [Header("Звук (опционально)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip menuMusic;

    [Header("Освещение")]
    [SerializeField] private Color startAmbientColor = Color.black;
    [SerializeField] private Color endAmbientColor = Color.white;

    private void Start()
    {
        SetupInitialState();
        StartCoroutine(PlayMenuSequence());
    }

    private void SetupInitialState()
    {
        // Устанавливаем начальное состояние
        if (loadingScreenCanvasGroup != null)
            loadingScreenCanvasGroup.alpha = 1f;

        if (menuCanvasGroup != null)
            menuCanvasGroup.alpha = 0f;

        if (titleCanvasGroup != null)
            titleCanvasGroup.alpha = 0f;

        if (mainCamera != null)
            mainCamera.transform.position = startCameraPosition;

        RenderSettings.ambientLight = startAmbientColor;

        // Стартуем музыку
        if (audioSource != null && menuMusic != null)
        {
            audioSource.clip = menuMusic;
            audioSource.loop = true;
            audioSource.volume = 0f; // Начинаем с нуля
        }
    }

    private IEnumerator PlayMenuSequence()
    {
        // Этап 1: Показываем черный экран с текстом (2 секунды)
        yield return StartCoroutine(ShowLoadingScreen(showLoadingScreenTime));

        // Этап 2: Осветление экрана с фейдом музыки (1.5 секунды)
        yield return StartCoroutine(FadeInAmbience(fadeInDuration));

        // Этап 3: Камера движется к целевой позиции (2 секунды)
        yield return StartCoroutine(MoveCameraSmooth(cameraMoveDuration));

        // Этап 4: Появляются UI элементы меню (1 секунда)
        yield return StartCoroutine(FadeInMenuUI(menuFadeInDuration));

        // Этап 5: Меню полностью готово, камера начинает вращаться
        Debug.Log("✅ Меню полностью загружено!");
    }

    private IEnumerator ShowLoadingScreen(float duration)
    {
        // Отображаем текст загрузки
        if (loadingScreenText != null)
        {
            loadingScreenText.text = "BYTESCHOOL";
            loadingScreenText.color = new Color(1, 1, 1, 1);
        }

        // Просто ждем
        yield return new WaitForSeconds(duration);
    }

    private IEnumerator FadeInAmbience(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // Фейд освещения
            RenderSettings.ambientLight = Color.Lerp(startAmbientColor, endAmbientColor, progress);

            // Фейд экрана загрузки
            if (loadingScreenCanvasGroup != null)
            {
                loadingScreenCanvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);
            }

            // Фейд музыки
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.volume = Mathf.Lerp(0f, 0.7f, progress);
            }
            else if (audioSource != null && menuMusic != null)
            {
                audioSource.Play();
                audioSource.volume = Mathf.Lerp(0f, 0.7f, progress);
            }

            yield return null;
        }

        if (loadingScreenCanvasGroup != null)
            loadingScreenCanvasGroup.alpha = 0f;

        RenderSettings.ambientLight = endAmbientColor;
    }

    private IEnumerator MoveCameraSmooth(float duration)
    {
        float elapsedTime = 0f;
        Vector3 startPos = mainCamera.transform.position;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // Используем SmoothStep для более естественного движения
            float easeProgress = Mathf.SmoothStep(0f, 1f, progress);
            mainCamera.transform.position = Vector3.Lerp(startPos, targetCameraPosition, easeProgress);

            yield return null;
        }

        mainCamera.transform.position = targetCameraPosition;
    }

    private IEnumerator FadeInMenuUI(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            if (menuCanvasGroup != null)
                menuCanvasGroup.alpha = progress;

            if (titleCanvasGroup != null)
                titleCanvasGroup.alpha = progress;

            yield return null;
        }

        if (menuCanvasGroup != null)
            menuCanvasGroup.alpha = 1f;

        if (titleCanvasGroup != null)
            titleCanvasGroup.alpha = 1f;
    }

    // Метод для воспроизведения звука кнопки
    public void PlayButtonClickSound(AudioClip clickSound)
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound, 1f);
        }
    }
}