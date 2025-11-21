using System;
using System.Collections.Generic;
using UnityEngine;

public class SimpleInventory : MonoBehaviour, IInventory
{
    [SerializeField] private List<InventoryItemData> initialItems = new List<InventoryItemData>();
    private bool isInventoryOpen = false;
    private int selectedItemIndex = -1;

    [Header("3D Item Preview")]
    public Camera previewCamera;
    public Transform previewParent;
    public float rotationSpeed = 2f;
    private GameObject currentPreviewItem;

    private readonly InventoryModel model = new InventoryModel();
    private IPlayerInputService inputService;
    private IGameStateService gameStateService;

    public event Action<InventoryItemData> ItemAdded
    {
        add => model.ItemAdded += value;
        remove => model.ItemAdded -= value;
    }

    public event Action<InventoryItemData> ItemRemoved
    {
        add => model.ItemRemoved += value;
        remove => model.ItemRemoved -= value;
    }

    public IReadOnlyList<InventoryItemData> Items => model.Items;

    private void Awake()
    {
        GameServiceLocator.Register<IInventory>(this);
    }

    private void OnDestroy()
    {
        GameServiceLocator.Unregister<IInventory>();
    }

    void Start()
    {
        CacheServices();

        // Автоматически создаем объекты для превью если они не назначены
        if (previewParent == null)
        {
            GameObject previewParentObj = new GameObject("PreviewParent");
            previewParent = previewParentObj.transform;
            // Размещаем далеко от игровой сцены, чтобы не пересекаться
            previewParent.position = new Vector3(1000, 1000, 1000);
        }
        
        if (previewCamera == null)
        {
            GameObject previewCameraObj = new GameObject("PreviewCamera");
            previewCamera = previewCameraObj.AddComponent<Camera>();
            previewCamera.transform.SetParent(previewParent);
            // Камера смотрит прямо на предмет
            previewCamera.transform.localPosition = new Vector3(0, 0.5f, -2f);
            previewCamera.transform.localRotation = Quaternion.Euler(10, 0, 0);
            previewCamera.fieldOfView = 30; // Более узкий угол для приближения
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.15f, 0.15f, 0.15f); // Темно-серый фон
            previewCamera.enabled = false; // Отключаем по умолчанию
            previewCamera.depth = 10; // Рендерим поверх всего
        }

        // Загружаем первоначальные предметы
        foreach (var item in initialItems)
        {
            if (item != null)
            {
                model.TryAdd(item);
            }
        }
    }

    void OnEnable()
    {
        CacheServices();
    }

    void Update()
    {
        // Открытие/закрытие инвентаря
        bool toggleInventory = inputService != null ? inputService.ConsumeToggleInventory() : Input.GetKeyDown(KeyCode.I);
        if (toggleInventory)
        {
            ToggleInventory();
        }
        
        // Вращение предмета в превью
        if (isInventoryOpen && currentPreviewItem != null)
        {
            RotatePreviewItem();
        }
    }
    
    void RotatePreviewItem()
    {
        if (Input.GetMouseButton(0)) // Левая кнопка мыши
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            
            // Вращение по вертикали (вокруг оси X)
            currentPreviewItem.transform.Rotate(Vector3.right, mouseY * rotationSpeed, Space.World);
            // Вращение по горизонтали (вокруг оси Y)
            currentPreviewItem.transform.Rotate(Vector3.up, -mouseX * rotationSpeed, Space.World);
        }
    }
    
    void CreatePreviewItem(InventoryItemData item)
    {
        // Удаляем предыдущий превью
        if (currentPreviewItem != null)
        {
            Destroy(currentPreviewItem);
        }
        
        if (item.prefab != null)
        {
            Debug.Log("Создаем превью для предмета: " + item.itemName);
            
            // Создаем копию предмета для превью
            currentPreviewItem = Instantiate(item.prefab, previewParent);
            currentPreviewItem.SetActive(true);
            
            // Настраиваем позицию - предмет на уровне камеры
            currentPreviewItem.transform.localPosition = new Vector3(0, 0, 0);
            currentPreviewItem.transform.localRotation = Quaternion.Euler(-15, 45, 0); // Небольшой наклон для лучшего обзора
            
            // Сохраняем оригинальный масштаб предмета
            // НЕ изменяем масштаб - оставляем как есть
            
            // Убираем коллайдеры и физику для превью
            Collider[] colliders = currentPreviewItem.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }
            
            Rigidbody[] rigidbodies = currentPreviewItem.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = true;
            }
            
            // Включаем камеру превью
            if (previewCamera != null)
            {
                previewCamera.enabled = true;
            }
            
            Debug.Log("Превью создано. Позиция: " + currentPreviewItem.transform.position);
        }
        else
        {
            Debug.LogWarning("Prefab предмета " + item.itemName + " равен null!");
        }
    }
    
    void OnGUI()
    {
        // Инвентарь
        if (isInventoryOpen)
        {
            // Адаптивные размеры под экран
            float inventoryWidth = Screen.width * 0.25f; // 25% ширины экрана
            float inventoryHeight = Screen.height * 0.7f; // 70% высоты экрана
            float margin = Screen.width * 0.01f; // 1% отступ
            
            GUI.Box(new Rect(margin, margin, inventoryWidth, inventoryHeight), "Инвентарь");

            float buttonHeight = Screen.height * 0.04f; // 4% высоты экрана
            float buttonSpacing = Screen.height * 0.005f; // 0.5% отступ между кнопками

            for (int i = 0; i < Items.Count; i++)
            {
                Rect buttonRect = new Rect(margin * 2, margin * 3 + i * (buttonHeight + buttonSpacing), 
                                          inventoryWidth - margin * 3, buttonHeight);
                
                // Подсвечиваем выбранный предмет
                if (selectedItemIndex == i)
                {
                    GUI.backgroundColor = Color.yellow;
                }
                
                if (GUI.Button(buttonRect, Items[i].itemName))
                {
                    selectedItemIndex = i;
                    Debug.Log("Выбран предмет: " + Items[i].itemName);
                    
                    // Создаем превью при выборе
                    CreatePreviewItem(Items[i]);
                }
                
                GUI.backgroundColor = Color.white;
            }
            
            // Область для 3D превью - показываем только если предмет выбран
            if (selectedItemIndex >= 0 && selectedItemIndex < Items.Count && currentPreviewItem != null)
            {
                // Адаптивная область превью
                float previewWidth = Screen.width * 0.4f; // 40% ширины экрана
                float previewHeight = Screen.height * 0.7f; // 70% высоты экрана
                float previewX = inventoryWidth + margin * 2;
                
                Rect previewArea = new Rect(previewX, margin, previewWidth, previewHeight);
                GUI.Box(previewArea, "Превью предмета");
                
                // Настраиваем камеру превью для рендеринга в область превью
                if (previewCamera != null)
                {
                    previewCamera.pixelRect = new Rect(previewArea.x, Screen.height - previewArea.y - previewArea.height, 
                                                     previewArea.width, previewArea.height);
                }
                
                // Инструкция для вращения
                GUI.Label(new Rect(previewX, previewHeight + margin * 2, previewWidth, buttonHeight), 
                         "Зажмите ЛКМ для вращения предмета");
            }

            if (selectedItemIndex >= 0 && selectedItemIndex < Items.Count)
            {
                float useButtonY = inventoryHeight - buttonHeight - margin;
                if (GUI.Button(new Rect(margin * 2, useButtonY, inventoryWidth - margin * 3, buttonHeight), 
                              "Использовать " + Items[selectedItemIndex].itemName))
                {
                    UseItem(selectedItemIndex);
                }
            }
        }
    }

    void DropItem(InventoryItemData item)
    {
        if (item.prefab != null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 dropPos = mainCamera.transform.position + mainCamera.transform.forward * 2f;
                GameObject clone = Instantiate(item.prefab, dropPos, Quaternion.identity);
                clone.name = item.itemName;
                clone.tag = "Item";
                clone.SetActive(true);
                Debug.Log("Предмет выпал: " + item.itemName);
            }
        }
    }

    public bool TryAdd(InventoryItemData item) => model.TryAdd(item);

    public bool TryRemoveAt(int index, out InventoryItemData removedItem) => model.TryRemoveAt(index, out removedItem);

    private void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        if (!isInventoryOpen)
        {
            ClearPreview();
        }

        if (gameStateService == null)
        {
            GameServiceLocator.TryGet(out gameStateService);
        }

        if (gameStateService != null)
        {
            if (isInventoryOpen)
                gameStateService.PauseGame();
            else
                gameStateService.ResumeGame();
        }
        else
        {
            Cursor.lockState = isInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isInventoryOpen;
        }
    }

    private void ClearPreview()
    {
        if (currentPreviewItem != null)
        {
            Destroy(currentPreviewItem);
            currentPreviewItem = null;
        }

        selectedItemIndex = -1;

        if (previewCamera != null)
        {
            previewCamera.enabled = false;
        }
    }

    private void UseItem(int index)
    {
        if (model.TryRemoveAt(index, out InventoryItemData item))
        {
            DropItem(item);
            selectedItemIndex = -1;
            ClearPreview();
        }
    }

    private void CacheServices()
    {
        if (inputService == null)
        {
            if (!GameServiceLocator.TryGet(out inputService))
            {
                inputService = FindObjectOfType<PlayerInputService>();
                if (inputService != null)
                {
                    GameServiceLocator.Register<IPlayerInputService>(inputService);
                }
            }
        }

        if (gameStateService == null)
        {
            if (!GameServiceLocator.TryGet(out gameStateService))
            {
                gameStateService = FindObjectOfType<GameStateManager>();
                if (gameStateService != null)
                {
                    GameServiceLocator.Register<IGameStateService>(gameStateService);
                }
            }
        }
    }
}