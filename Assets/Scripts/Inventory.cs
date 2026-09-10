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

    private GameObject objectToPickup = null;
    private Item itemDataToPickup = null;

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
        if (Input.GetKeyDown(KeyCode.E) && objectToPickup != null && itemDataToPickup != null)
        {
            if (AddItem(itemDataToPickup))
            {
                Debug.Log($"Подобран предмет: {itemDataToPickup.itemName}");
                Destroy(objectToPickup);

                objectToPickup = null;
                itemDataToPickup = null;
            }
        }

        // Выбрасывание предмета (Q)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (items != null && selectedSlot < items.Length && items[selectedSlot] != null)
            {
                Item droppedItem = items[selectedSlot];
                Debug.Log($"Выброшен предмет: {droppedItem.itemName}");

                if (droppedItem.prefab != null)
                {
                    Vector3 spawnPos = dropPoint != null
                        ? dropPoint.position
                        : transform.position + transform.forward * 1.5f + Vector3.up;

                    GameObject spawnedObject = Instantiate(droppedItem.prefab, spawnPos, Quaternion.identity);

                    // Гарантируем, что у выброшенного объекта есть всё нужное для повторного подбора
                    spawnedObject.tag = "Pickup";

                    Collider col = spawnedObject.GetComponent<Collider>();
                    if (col != null) col.isTrigger = true;

                    if (spawnedObject.GetComponent<Rigidbody>() == null)
                    {
                        Rigidbody rb = spawnedObject.AddComponent<Rigidbody>();
                        rb.isKinematic = true;
                        rb.useGravity = false;
                    }

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

        objectToPickup = other.gameObject;
        itemDataToPickup = pickupComponent.itemData;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Pickup") && other.gameObject == objectToPickup)
        {
            objectToPickup = null;
            itemDataToPickup = null;
        }
    }
}