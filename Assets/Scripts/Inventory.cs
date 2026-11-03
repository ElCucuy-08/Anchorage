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

    [Header("Настройки удержания и вращения")]
    public Transform handPoint; // Точка (Пустой GameObject) на игроке/камере, где будет визуально отображаться предмет в руке
    public float rotationSpeed = 5f; // Скорость вращения предмета мышью

    private int selectedSlot = 0;
    private GameObject currentSpawnedHandObject = null; // Текущий 3D объект предмета в руке

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
        HandleItemRotation();
    }

    void HandleSlotSelection()
    {
        int previousSlot = selectedSlot;

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
        else if (scroll();
        if (col != null) col.enabled = false;

        Rigidbody rb = currentSpawnedHandObject.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // Отключаем на нем скрипт подбора, чтобы случайно не сработал
        PickupItem pi = currentSpawnedHandObject.GetComponent<PickupItem>();
        if (pi != null) pi.enabled = false;
    }
}
    }

    void HandleInteraction()
{
    // Изменено: Убрано использование на ЛКМ (так как теперь ЛКМ отвечает за вращение)
    // Если вам нужно действие на ЛКМ, его логику можно перенести, например, на ПКМ (Input.GetMouseButtonDown(1))

    // Подбор предмета (E)
    if (Input.GetKeyDown(KeyCode.E) && itemsInRange.Count > 0)
    {
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

    // Новая логика: Вращение предмета при зажатии ЛКМ
    void HandleItemRotation()
{
    // Проверяем, зажата ли ЛКМ и создан ли сейчас объект в руке
    if (Input.GetMouseButton(0) && currentSpawnedHandObject != null)
    {
        // Считываем смещение мыши по осям X и Y
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

        // Вращаем объект относительно осей мира или камеры игрока
        // mouseX крутит объект влево-вправо (вокруг вертикальной оси Y)
        // mouseY крутит объект вверх-вниз (вокруг горизонтальной оси X)
        currentSpawnedHandObject.transform.Rotate(Vector3.up, -mouseX, Space.World);
        currentSpawnedHandObject.transform.Rotate(Vector3.right, mouseY, Space.World);
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
