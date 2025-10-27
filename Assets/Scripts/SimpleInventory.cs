using System.Collections.Generic;
using UnityEngine;

public class SimpleInventory : MonoBehaviour
{
    [System.Serializable]
    public class ItemData
    {
        public string itemName;
        public GameObject prefab; // Ссылка на оригинальный объект
    }

    public Camera playerCamera;
    public float pickupRange = 3f;

    private List<ItemData> inventory = new List<ItemData>();
    private bool isInventoryOpen = false;
    private bool canMove = true;
    private int selectedItemIndex = -1;
    private string lookAtItemName = "";

    void Update()
    {
        // Проверяем, на что наведен прицел
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
            if (hit.collider.CompareTag("Item"))
            {
                lookAtItemName = hit.collider.name;

                // Подбор предмета
                if (Input.GetKeyDown(KeyCode.E) && canMove)
                {
                    ItemData data = new ItemData();
                    data.itemName = hit.collider.name;

                    // Сохраняем оригинальный префаб или объект
                    data.prefab = hit.collider.gameObject;

                    // Создаём копию, чтобы при выпадении потом можно было клонировать
                    GameObject storedPrefab = Instantiate(data.prefab);
                    storedPrefab.SetActive(false);
                    data.prefab = storedPrefab;

                    inventory.Add(data);
                    Destroy(hit.collider.gameObject);
                    lookAtItemName = "";
                    Debug.Log("Подобран предмет: " + data.itemName);
                }
            }
            else
            {
                lookAtItemName = "";
            }
        }
        else
        {
            lookAtItemName = "";
        }

        // Открытие/закрытие инвентаря
        if (Input.GetKeyDown(KeyCode.I))
        {
            isInventoryOpen = !isInventoryOpen;
            canMove = !isInventoryOpen;
            Cursor.lockState = isInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isInventoryOpen;
        }
    }

    void OnGUI()
    {
        // Подсказка при наведении
        if (!isInventoryOpen && !string.IsNullOrEmpty(lookAtItemName))
        {
            GUI.Label(new Rect(Screen.width / 2 - 80, Screen.height / 2 + 40, 200, 30), "Нажмите E, чтобы подобрать");
        }

        // Инвентарь
        if (isInventoryOpen)
        {
            GUI.Box(new Rect(10, 10, 250, 300), "Инвентарь");

            for (int i = 0; i < inventory.Count; i++)
            {
                if (GUI.Button(new Rect(20, 40 + i * 30, 220, 25), inventory[i].itemName))
                {
                    selectedItemIndex = i;
                    Debug.Log("Выбран предмет: " + inventory[i].itemName);
                }
            }

            if (selectedItemIndex >= 0 && selectedItemIndex < inventory.Count)
            {
                if (GUI.Button(new Rect(20, 280, 220, 25), "Использовать " + inventory[selectedItemIndex].itemName))
                {
                    DropItem(inventory[selectedItemIndex]);
                    inventory.RemoveAt(selectedItemIndex);
                    selectedItemIndex = -1;
                }
            }
        }
    }

    void DropItem(ItemData item)
    {
        if (item.prefab != null)
        {
            Vector3 dropPos = playerCamera.transform.position + playerCamera.transform.forward * 2f;
            GameObject clone = Instantiate(item.prefab, dropPos, Quaternion.identity);
            clone.name = item.itemName;
            clone.tag = "Item";
            clone.SetActive(true);
            Debug.Log("Предмет выпал: " + item.itemName);
        }
    }
}