using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    public Transform handPoint;
    public float rotationSpeed = 200f;

    private int selectedSlot = 0;
    private GameObject currentSpawnedHandObject = null;
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
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SelectSlot(5);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SelectSlot(6);
        if (Input.GetKeyDown(KeyCode.Alpha8)) SelectSlot(7);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            int next = selectedSlot > 0 ? selectedSlot - 1 : 7;
            SelectSlot(next);
        }
        else if (scroll < 0f)
        {
            int next = selectedSlot < 7 ? selectedSlot + 1 : 0;
            SelectSlot(next);
        }
    }

    void SelectSlot(int index)
    {
        selectedSlot = index;

        for (int i = 0; i < slotHighlights.Length; i++)
        {
            if (slotHighlights[i] != null)
                slotHighlights[i].SetActive(i == selectedSlot);
        }

        SpawnHandItem(items[selectedSlot]);
    }

    void HandleInteraction()
    {
        if (Input.GetKeyDown(KeyCode.E) && itemsInRange.Count > 0)
        {
            itemsInRange.RemoveAll(item => item == null || !item.gameObject.activeInHierarchy);

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

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (items[selectedSlot] != null)
            {
                DropItem(items[selectedSlot]);
                items[selectedSlot] = null;
                UpdateAllSlots();
                SpawnHandItem(null);
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
                if (i == selectedSlot) SpawnHandItem(newItem);
                return true;
            }
        }
        Debug.Log("Инвентарь полон!");
        return false;
    }

    void DropItem(Item droppedItem)
    {
        // ЗАМЕНИТЕ 'prefab' на то имя, которое используется у вас в классе Item
        if (droppedItem == null || droppedItem.prefab == null)
        {
            Debug.LogWarning("У предмета нет 3D-префаба для выброса!");
            return;
        }

        Vector3 spawnPos = dropPoint != null ? dropPoint.position : transform.position + transform.forward * 1.5f;

        // ЗАМЕНИТЕ 'prefab' здесь тоже
        GameObject spawnedObject = Instantiate(droppedItem.prefab, spawnPos, Quaternion.identity);

        Rigidbody rb = spawnedObject.GetComponent<Rigidbody>();
        if (rb == null) rb = spawnedObject.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        Collider col = spawnedObject.GetComponent<Collider>();
        if (col == null) col = spawnedObject.AddComponent<BoxCollider>();
        col.isTrigger = true;

        PickupItem pickupComponent = spawnedObject.GetComponent<PickupItem>();
        if (pickupComponent == null) pickupComponent = spawnedObject.AddComponent<PickupItem>();
        pickupComponent.itemData = droppedItem;

        spawnedObject.tag = "Pickup";
    }

    void HandleItemRotation()
    {
        if (Input.GetMouseButton(0) && currentSpawnedHandObject != null)
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

            currentSpawnedHandObject.transform.Rotate(Vector3.up, -mouseX, Space.World);
            currentSpawnedHandObject.transform.Rotate(Vector3.right, mouseY, Space.World);
        }
    }

    void SpawnHandItem(Item itemToSpawn)
    {
        if (currentSpawnedHandObject != null)
        {
            Destroy(currentSpawnedHandObject);
            currentSpawnedHandObject = null;
        }

        // ЗАМЕНИТЕ 'prefab' здесь тоже
        if (itemToSpawn == null || itemToSpawn.prefab == null || handPoint == null) return;

        // ЗАМЕНИТЕ 'prefab' здесь тоже
        currentSpawnedHandObject = Instantiate(itemToSpawn.prefab, handPoint);
        currentSpawnedHandObject.transform.localPosition = Vector3.zero;
        currentSpawnedHandObject.transform.localRotation = Quaternion.identity;

        Collider col = currentSpawnedHandObject.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = currentSpawnedHandObject.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        PickupItem pi = currentSpawnedHandObject.GetComponent<PickupItem>();
        if (pi != null) pi.enabled = false;
    }

    void UpdateAllSlots()
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].icon != null && slotIcons[i] != null)
            {
                slotIcons[i].sprite = items[i].icon;
                slotIcons[i].enabled = true;
            }
            else if (slotIcons[i] != null)
            {
                slotIcons[i].sprite = null;
                slotIcons[i].enabled = false;
            }
        }
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