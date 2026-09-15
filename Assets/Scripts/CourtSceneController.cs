using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CourtSceneController : MonoBehaviour
{
    [Header("UI — Диалоговое окно")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private TextMeshProUGUI judgeText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private TextMeshProUGUI yesButtonText;
    [SerializeField] private TextMeshProUGUI noButtonText;

    [Header("Декоративные панели за кнопками")]
    [SerializeField] private GameObject decorativePanel1;
    [SerializeField] private GameObject decorativePanel2;
    [SerializeField] private GameObject decorativePanel3;

    [Header("Звук и печать текста")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typingSound;
    [SerializeField] private float typingSpeed = 0.04f; // Задержка между буквами (чем меньше, тем быстрее)

    [Header("Камера")]
    [SerializeField] private Transform cameraTransform;

    [Header("Переход на сцену по номеру (Build Index)")]
    [SerializeField] private int nextSceneIndex = 0;

    private Texture2D fadeTexture;
    private float fadeAlpha = 1f;

    private struct Question
    {
        public string judgeDialogue;
        public string yesAnswer;
        public string noAnswer;
        public bool isYesCorrect;

        public Question(string dialogue, string yes, string no, bool yesIsCorrect)
        {
            judgeDialogue = dialogue;
            yesAnswer = yes;
            noAnswer = no;
            isYesCorrect = yesIsCorrect;
        }
    }

    private Question[] questions;
    private int currentQuestionIndex = 0;
    private int wrongAnswersCount = 0;
    private bool waitingForInput = false;
    private bool selectedYes = false;

    private void Awake()
    {
        fadeTexture = new Texture2D(1, 1);
        fadeTexture.SetPixel(0, 0, Color.black);
        fadeTexture.Apply();
    }

    private void OnGUI()
    {
        if (fadeAlpha > 0f)
        {
            GUI.color = new Color(0f, 0f, 0f, fadeAlpha);
            GUI.depth = -1000;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTexture);
        }
    }

    private void Start()
    {
        questions = new Question[]
        {
            new Question(
                "Подсудимый, вы въехали в США по поддельной грин-карте. Вы признаете свою вину в нелегальном пересечении границы?",
                "Я бежал от безысходности",
                "Ваша граница дыра полная",
                true // YES
            ),
            new Question(
                "В вашем досье указано, что вы отбыли 6 лет в русской тюрьме за крупную кражу. Почему суд Аляски должен верить, что вы приехали сюда не за тем же?",
                "Вас это ебать не должно",
                "Тот срок был главной ошибкой моей жизни",
                false // NO
            ),
            new Question(
                "Спустя пару недель после прилета вы вскрыли сейф. Зачем вы снова пошли на преступление?",
                "Я замерзал на улице без гроша",
                "Замки лучше ставьте, пендосы!",
                true // YES
            )
        };

        yesButton.onClick.AddListener(() => OnChoiceSelected(true));
        noButton.onClick.AddListener(() => OnChoiceSelected(false));

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (dialogueCanvasGroup != null) dialogueCanvasGroup.alpha = 0f;

        StartCoroutine(CourtSequence());
    }

    private IEnumerator CourtSequence()
    {
        // 1. Fade из черного кодом (1.5 сек)
        yield return StartCoroutine(FadeRoutine(1f, 0f, 1.5f));

        // 2. Открываем глаза / Поворот головы вверх на 40 градусов
        yield return StartCoroutine(RotateCameraUp(40f, 1.2f));

        // 3. Плавное появление диалогового окна
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (dialogueCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(dialogueCanvasGroup, 0f, 1f, 0.8f));
        }

        // 4. Цикл вопросов
        while (currentQuestionIndex < questions.Length)
        {
            Question q = questions[currentQuestionIndex];

            yesButtonText.text = q.yesAnswer;
            noButtonText.text = q.noAnswer;

            yesButton.gameObject.SetActive(true);
            noButton.gameObject.SetActive(true);

            // Печать текста судьи с проигрыванием звуков
            yield return StartCoroutine(TypeTextRoutine(q.judgeDialogue));

            waitingForInput = true;
            while (waitingForInput)
            {
                yield return null;
            }

            bool choseCorrectly = (selectedYes == q.isYesCorrect);
            if (!choseCorrectly)
            {
                wrongAnswersCount++;
            }

            currentQuestionIndex++;
        }

        // 5. Вынесение приговора — скрываем кнопки и декоративные панели
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
        SetDecorativePanelsActive(false);

        // Печать финального текста вердикта
        yield return StartCoroutine(TypeTextRoutine("Суд назначает вам 8 лет строгого режима в тюрьме Anchorage Штата Аляска."));

        yield return new WaitForSeconds(3f);

        // 6. Расчет коэффициента штрафа репутации
        float repMultiplier = 1.0f - (wrongAnswersCount * 0.10f);
        PlayerPrefs.SetFloat("ReputationMultiplier", repMultiplier);
        PlayerPrefs.Save();

        // 7. Fade в черный кодом (1.5 сек) и загрузка сцены по НОМЕРУ
        yield return StartCoroutine(FadeRoutine(0f, 1f, 1.5f));
        SceneManager.LoadScene(nextSceneIndex);
    }

    private IEnumerator TypeTextRoutine(string textToType)
    {
        judgeText.text = "";

        foreach (char letter in textToType.ToCharArray())
        {
            judgeText.text += letter;

            // Воспроизводим звук печати (пропускаем пробелы)
            if (!char.IsWhiteSpace(letter) && audioSource != null && typingSound != null)
            {
                audioSource.PlayOneShot(typingSound);
            }

            yield return new WaitForSeconds(typingSpeed);
        }
    }

    private void SetDecorativePanelsActive(bool active)
    {
        if (decorativePanel1 != null) decorativePanel1.SetActive(active);
        if (decorativePanel2 != null) decorativePanel2.SetActive(active);
        if (decorativePanel3 != null) decorativePanel3.SetActive(active);
    }

    private void OnChoiceSelected(bool isYes)
    {
        if (!waitingForInput) return;
        selectedYes = isYes;
        waitingForInput = false;
    }

    private IEnumerator FadeRoutine(float startAlpha, float targetAlpha, float duration)
    {
        float elapsed = 0f;
        fadeAlpha = startAlpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        fadeAlpha = targetAlpha;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float targetAlpha, float duration)
    {
        float elapsed = 0f;
        cg.alpha = startAlpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        cg.alpha = targetAlpha;
    }

    private IEnumerator RotateCameraUp(float angle, float duration)
    {
        if (cameraTransform == null) yield break;

        Quaternion startRotation = cameraTransform.localRotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(-angle, 0f, 0f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cameraTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / duration);
            yield return null;
        }

        cameraTransform.localRotation = targetRotation;
    }
}