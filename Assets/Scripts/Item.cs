using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName;
    public Sprite icon;

    [Header("Объект для спавна при выбрасывании")]
    public GameObject prefab;
}