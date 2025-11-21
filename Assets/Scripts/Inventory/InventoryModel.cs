using System;
using System.Collections.Generic;

/// <summary>
/// Модель данных инвентаря. Не зависит от Unity API и может тестироваться отдельно.
/// </summary>
public class InventoryModel : IInventory
{
    private readonly List<InventoryItemData> items = new List<InventoryItemData>();

    public event Action<InventoryItemData> ItemAdded;
    public event Action<InventoryItemData> ItemRemoved;

    public IReadOnlyList<InventoryItemData> Items => items;

    public bool TryAdd(InventoryItemData item)
    {
        if (item == null)
            return false;

        items.Add(item);
        ItemAdded?.Invoke(item);
        return true;
    }

    public bool TryRemoveAt(int index, out InventoryItemData removedItem)
    {
        if (index >= 0 && index < items.Count)
        {
            removedItem = items[index];
            items.RemoveAt(index);
            ItemRemoved?.Invoke(removedItem);
            return true;
        }

        removedItem = null;
        return false;
    }
}


