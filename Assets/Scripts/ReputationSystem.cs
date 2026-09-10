using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReputationSystem : MonoBehaviour
{
    [Header("Текущая репутация")]
    [SerializeField] private float currentReputation = 0f;

    [Header("Тест из Инспектора")]
    [SerializeField] private bool addFiveReputation;

    [Header("UI Компоненты")]
    [SerializeField] private GameObject reputationUIPanel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform fillRectTransform; // Серый квадратик шкалы
    [SerializeField] private TextMeshProUGUI infoText;

    [Header("Настройки анимации и показа")]
    [SerializeField] private float fillLerpSpeed = 6f;
    [SerializeField] private float fadeSpeed = 4f;
    [SerializeField] private float displayDuration = 2.5f;

    private readonly float[] thresholds = { 0f, 20f, 50f, 100f, 175f };
    private readonly string[] rankNames = { "Никто", "Кем являешься?", "Мужик!", "Уважаемый!", "Авторитет" };

    private float targetFillAmount = 0f;
    private float originalMaxWidth = 0f;
    private float currentDisplayWidth = 0f;
    private int currentRankIndex = 0;

    private float targetAlpha = 0f;
    private float hideTimer = 0f;

    private void Start()
    {
        currentRankIndex = GetRankIndex(currentReputation);

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (reputationUIPanel != null) reputationUIPanel.SetActive(false);

        if (fillRectTransform != null)
        {
            originalMaxWidth = fillRectTransform.sizeDelta.x;
        }

        UpdateUIValues(true, true);
        currentDisplayWidth = originalMaxWidth * targetFillAmount;
        ApplyWidth(currentDisplayWidth);
    }

    private void Update()
    {
        if (addFiveReputation)
        {
            addFiveReputation = false;
            AddReputation(5f);
        }

        if (fillRectTransform != null)
        {
            float targetWidth = originalMaxWidth * targetFillAmount;
            currentDisplayWidth = Mathf.Lerp(currentDisplayWidth, targetWidth, Time.deltaTime * fillLerpSpeed);
            ApplyWidth(currentDisplayWidth);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

            if (canvasGroup.alpha <= 0.01f && targetAlpha == 0f && reputationUIPanel.activeSelf)
            {
                reputationUIPanel.SetActive(false);
            }
        }

        if (targetAlpha > 0f)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
            {
                targetAlpha = 0f;
            }
        }
    }

    public void AddReputation(float amount)
    {
        currentReputation += amount;

        if (reputationUIPanel != null && !reputationUIPanel.activeSelf)
        {
            reputationUIPanel.SetActive(true);
        }
        targetAlpha = 1f;
        hideTimer = displayDuration;

        int newRankIndex = GetRankIndex(currentReputation);

        if (newRankIndex > currentRankIndex)
        {
            currentRankIndex = newRankIndex;
            if (infoText != null)
            {
                infoText.text = $"Новая масть — {rankNames[currentRankIndex]}!";
            }

            // Обновляем процент заполнения для нового ранга, но НЕ перезаписываем текст
            UpdateUIValues(false, false);
        }
        else
        {
            UpdateUIValues(false, true);
        }
    }

    private int GetRankIndex(float rep)
    {
        for (int i = thresholds.Length - 1; i >= 0; i--)
        {
            if (rep >= thresholds[i])
                return i;
        }
        return 0;
    }

    private void UpdateUIValues(bool immediate, bool updateText)
    {
        if (currentRankIndex < thresholds.Length - 1)
        {
            float currentMin = thresholds[currentRankIndex];
            float nextMin = thresholds[currentRankIndex + 1];

            float progress = (currentReputation - currentMin) / (nextMin - currentMin);
            targetFillAmount = Mathf.Clamp01(progress);

            float pointsNeeded = nextMin - currentReputation;

            if (updateText && infoText != null)
            {
                infoText.text = $"До новой масти {Mathf.CeilToInt(pointsNeeded)} репутации.";
            }
        }
        else
        {
            targetFillAmount = 1f;
            if (updateText && infoText != null)
            {
                infoText.text = $"Масть: {rankNames[currentRankIndex]} (Максимум)";
            }
        }

        if (immediate)
        {
            currentDisplayWidth = originalMaxWidth * targetFillAmount;
            ApplyWidth(currentDisplayWidth);
        }
    }

    private void ApplyWidth(float width)
    {
        if (fillRectTransform == null) return;

        Vector2 size = fillRectTransform.sizeDelta;
        size.x = width;
        fillRectTransform.sizeDelta = size;
    }
}