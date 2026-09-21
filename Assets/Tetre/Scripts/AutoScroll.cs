using UnityEngine;
using UnityEngine.UI;

public class AutoScroll : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float scrollSpeed = 0.1f;

    private bool isScrolling = true;

    private void Update()
    {
        if (!isScrolling)
            return;

        scrollRect.verticalNormalizedPosition -= scrollSpeed * Time.deltaTime;

        if (scrollRect.verticalNormalizedPosition <= 0f)
        {
            scrollRect.verticalNormalizedPosition = 0f;
            isScrolling = false;
        }
    }
}