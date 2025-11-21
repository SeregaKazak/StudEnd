using UnityEngine;

/// <summary>
/// Реализация сервиса подбора предметов. Использует Strategy-подход для инкапсуляции логики.
/// </summary>
public class ItemPickupService : MonoBehaviour, IItemPickupService
{
    private void Awake()
    {
        GameServiceLocator.Register<IItemPickupService>(this);
    }

    private void OnDestroy()
    {
        GameServiceLocator.Unregister<IItemPickupService>();
    }

    public bool TryPickupCenteredItem(Camera camera, float pickupDistance, IInventory inventory)
    {
        if (camera == null || inventory == null)
            return false;

        Ray ray = new Ray(camera.transform.position, camera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance))
        {
            if (hit.collider.CompareTag("Item"))
            {
                InventoryItemData data = CreateInventoryData(hit.collider.gameObject);
                if (inventory.TryAdd(data))
                {
                    Destroy(hit.collider.gameObject);
                    return true;
                }
            }
        }

        return false;
    }

    private InventoryItemData CreateInventoryData(GameObject source)
    {
        var copy = Instantiate(source);
        copy.SetActive(false);
        return new InventoryItemData
        {
            itemName = source.name,
            prefab = copy
        };
    }
}


