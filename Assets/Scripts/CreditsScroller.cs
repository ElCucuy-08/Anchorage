using UnityEngine;

public class CreditsScroller : MonoBehaviour
{
    [Header("Настройки движения")]
    [Tooltip("Скорость движения титров вверх")]
    public float scrollSpeed = 50f;

    [Tooltip("Y-координата, при достижении которой титры остановятся или переключат сцену")]
    public float endPositionY = 1500f;

    [Header("Взаимодействие")]
    [Tooltip("Можно ли ускорить титры удержанием клавиши (например, пробела или ЛКМ)")]
    public bool allowSpeedUp = true;
    [Tooltip("Во сколько раз увеличится скорость при ускорении")]
    public float speedUpMultiplier = 3f;

    private RectTransform rectTransform;
    private float startPositionY;

    void Start()
    {
        // Получаем компонент RectTransform UI-элемента
        rectTransform = GetComponent<RectTransform>();

        // Запоминаем начальную позицию (опционально)
        startPositionY = rectTransform.anchoredPosition.y;
    }

    void Update()
    {
        // Текущая скорость (обычная или ускоренная)
        float currentSpeed = scrollSpeed;
        if (allowSpeedUp && (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0)))
        {
            currentSpeed *= speedUpMultiplier;
        }

        // Двигаем объект вверх по оси Y
        rectTransform.anchoredPosition += new Vector2(0, currentSpeed * Time.deltaTime);

        // Проверяем, достигли ли мы конца
        if (rectTransform.anchoredPosition.y >= endPositionY)
        {
            OnCreditsEnd();
        }
    }

    void OnCreditsEnd()
    {
        // Отключаем скрипт, чтобы движение прекратилось
        enabled = false;

        Debug.Log("Титры закончились!");

        // Здесь можно вставить код для возврата в главное меню, например:
        // UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
