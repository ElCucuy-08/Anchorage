using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Единственный скрипт, который висит на игроке
public class Inventory : MonoBehaviour
{
    [Header("UI Элементы слотов")]
    public Image[] slotIcons = new Image[8];
    public GameObject[] slotHighlights = new GameObject[8];

    [Header("Инвентарь (Текущие предметы)")]
    public Item[] items = new Item[8];

    [Header("Настройки выбрасывания")]
    public Transform dropPoint;

    private int selectedSlot = 0;

    // Список всех предметов, которые сейчас находятся в зоне досягаемости игрока
    private List<PickupItem> itemsInRange = new List<PickupItem>();

    void Start()
    {
        UpdateAllSlots();
        SelectSlot(0);
    }

    void Update()
    {
        HandleSlotSelection();
        HandleInteraction();
    }

    void HandleSlotSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SelectSlot(5);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SelectSlot(6);
        if (Input.GetKeyDown(KeyCode.Alpha8)) SelectSlot(7);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) SelectSlot(selectedSlot > 0 ? selectedSlot - 1 : 7);
        else if (scroll < 0f) SelectSlot(selectedSlot < 7 ? selectedSlot + 1 : 0);
    }

    void SelectSlot(int index)
    {
        selectedSlot = index;

        if (slotHighlights == null) return;

        for (int i = 0; i < slotHighlights.Length; i++)
        {
            if (slotHighlights[i] != null)
                slotHighlights[i].SetActive(i == selectedSlot);
        }
    }

    void UpdateAllSlots()
    {
        if (slotIcons == null) return;

        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (slotIcons[i] == null) continue;

            bool hasItem = (items != null) && (i < items.Length) && (items[i] != null);

            if (hasItem)
            {
                slotIcons[i].sprite = items[i].icon;
                slotIcons[i].color = Color.white;
            }
            else
            {
                slotIcons[i].sprite = null;
                slotIcons[i].color = new Color(1, 1, 1, 0);
            }
        }
    }

    void HandleInteraction()
    {
        // Использование предмета (ЛКМ)
        if (Input.GetMouseButtonDown(0))
        {
            if (items != null && selectedSlot < items.Length && items[selectedSlot] != null)
                Debug.Log($"Использован предмет: {items[selectedSlot].itemName}");
        }

        // Подбор предмета (E)
        if (Input.GetKeyDown(KeyCode.E) && itemsInRange.Count > 0)
        {
            // Защита от «мертвых» ссылок на уничтоженные объекты
            itemsInRange.RemoveAll(item => item == null);

            if (itemsInRange.Count > 0)
            {
                PickupItem targetItem = itemsInRange[0];

                if (targetItem != null && targetItem.itemData != null)
                {
                    if (AddItem(targetItem.itemData))
                    {
                        Debug.Log($"Подобран предмет: {targetItem.itemData.itemName}");

                        itemsInRange.RemoveAt(0);
                        Destroy(targetItem.gameObject);
                    }
                }
            }
        }

        // Выбрасывание предмета (Q)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (items != null && selectedSlot();
            if (col != null) col.isTrigger = false;

            // Включаем физику для реалистичного падения предмета при выбрасывании
            Rigidbody rb = spawnedObject.GetComponent<Rigidbody>();
            if (rb == null) rb = spawnedObject.AddComponent<Rigidbody>();

            rb.isKinematic = false;
            rb.useGravity = true;

            PickupItem pickupComponent = spawnedObject.GetComponent<PickupItem>();
            if (pickupComponent == null)
                pickupComponent = spawnedObject.AddComponent<PickupItem>();

            pickupComponent.itemData = droppedItem;
        }
        else
        {
            Debug.LogWarning("У этого предмета не назначен Prefab!");
        }

        items[selectedSlot] = null;
        UpdateAllSlots();
    }
}
    }

    bool AddItem(Item newItem)
{
    for (int i = 0; i < items.Length; i++)
    {
        if (items[i] == null)
        {
            items[i] = newItem;
            UpdateAllSlots();
            return true;
        }
    }

    Debug.Log("Инвентарь полон!");
    return false;
}

private void OnTriggerEnter(Collider other)
{
    if (!other.CompareTag("Pickup")) return;

    PickupItem pickupComponent = other.GetComponent<PickupItem>();
    if (pickupComponent == null || pickupComponent.itemData == null) return;

    if (!itemsInRange.Contains(pickupComponent))
    {
        itemsInRange.Add(pickupComponent);
    }
}

private void OnTriggerExit(Collider other)
{
    if (!other.CompareTag("Pickup")) return;

    PickupItem pickupComponent = other.GetComponent<PickupItem>();
    if (pickupComponent != null && itemsInRange.Contains(pickupComponent))
    {
        itemsInRange.Remove(pickupComponent);
    }
}
}
