using System;
using System.Collections.Generic;

/// <summary>
/// Интерфейс инвентаря, отделяющий данные от представления.
/// </summary>
public interface IInventory
{
    event Action<InventoryItemData> ItemAdded;
    event Action<InventoryItemData> ItemRemoved;

    IReadOnlyList<InventoryItemData> Items { get; }

    bool TryAdd(InventoryItemData item);
    bool TryRemoveAt(int index, out InventoryItemData removedItem);
}


