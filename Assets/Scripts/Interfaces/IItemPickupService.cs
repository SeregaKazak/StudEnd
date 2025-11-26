using UnityEngine;

/// <summary>
/// Сервис, отвечающий за преобразование предметов сцены в элементы инвентаря.
/// </summary>
public interface IItemPickupService
{
    bool TryPickupCenteredItem(Camera camera, float pickupDistance, IInventory inventory);
}


