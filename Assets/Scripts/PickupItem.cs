using UnityEngine;

// Компонент для предметов в мире, которые можно подобрать
public class PickupItem : MonoBehaviour
{
    public Item itemData;

    [Tooltip("Сколько здоровья даёт предмет. 0 — зажигалки лечат автоматически.")]
    public float healAmount = 0f;
}
