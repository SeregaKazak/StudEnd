using UnityEngine;

public class PickupController : MonoBehaviour
{
    [Header("Настройка pickup")]
    public float pickupDistance = 3f;
    public float holdDistance = 1.5f;
    public float smoothSpeed = 10f;

    private Camera playerCamera;
    private GameObject heldObject;
    private Rigidbody heldObjectRb;
    private bool isLookingAtObject = false;
    private bool isHolding = false;
    private string lookAtItemName = "";
    private float originalDrag;
    private float originalAngularDrag;
    private IGameStateService gameStateService;
    private IPlayerInputService inputService;
    private IInventory inventory;
    private IItemPickupService pickupService;

    void Start()
    {
        playerCamera = Camera.main;
        CacheServices();
    }

    void OnEnable()
    {
        CacheServices();
    }

    void Update()
    {
        // Проверяем, не приостановлена ли игра
        if (IsGamePaused())
            return;
            
        CheckObject();
        HandleInput();
        MoveHeldObject();
    }

    void CheckObject()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                return;
        }

        Vector3 mousePosition = Input.mousePosition;

        // Check if the mouse position is within the screen bounds
        if (mousePosition.x < 0 || mousePosition.y < 0 || mousePosition.x > Screen.width || mousePosition.y > Screen.height)
        {
            Debug.LogWarning("Mouse position is out of screen bounds!");
            return;
        }

        // Проверяем предметы по центру экрана (для подбора в инвентарь)
        Ray centerRay = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit centerHit;
        
        if (Physics.Raycast(centerRay, out centerHit, pickupDistance))
        {
            if (centerHit.collider.CompareTag("Item"))
            {
                lookAtItemName = centerHit.collider.name;
                isLookingAtObject = true;
                return;
            }
        }
        
        // Проверяем предметы по позиции мыши (для таскания)
        Ray mouseRay = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit mouseHit;

        if (Physics.Raycast(mouseRay, out mouseHit, pickupDistance))
        {
            if (mouseHit.collider.CompareTag("Item") && !isHolding)
            {
                isLookingAtObject = true;
                lookAtItemName = mouseHit.collider.name;
                return;
            }
        }

        isLookingAtObject = false;
        lookAtItemName = "";
    }

    void HandleInput()
    {
        bool interactPressed = inputService != null ? inputService.ConsumeInteract() : Input.GetKeyDown(KeyCode.E);
        bool grabTogglePressed = inputService != null ? inputService.ConsumeGrabToggle() : Input.GetKeyDown(KeyCode.F);

        // Подбор в инвентарь (E)
        if (interactPressed && !isHolding && !string.IsNullOrEmpty(lookAtItemName))
        {
            PickUpToInventory();
        }
        
        // Таскание предметов (F)
        if (grabTogglePressed)
        {
            if (!isHolding && isLookingAtObject)
            {
                PickUpObject();
            }
            else if (isHolding)
            {
                DropObject();
            }
        }
    }

    void PickUpToInventory()
    {
        CacheServices();

        if (pickupService == null || inventory == null)
            return;

        string itemName = lookAtItemName;
        bool success = pickupService.TryPickupCenteredItem(playerCamera, pickupDistance, inventory);
        if (success)
        {
            string logName = string.IsNullOrEmpty(itemName) ? "неизвестный предмет" : itemName;
            Debug.Log("Подобран предмет в инвентарь: " + logName);
            lookAtItemName = "";
        }
    }

    void PickUpObject()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupDistance))
        {
            if (hit.collider.CompareTag("Item"))
            {
                heldObject = hit.collider.gameObject;
                heldObjectRb = heldObject.GetComponent<Rigidbody>();

                if (heldObjectRb != null)
                {
                    // Сохраняем оригинальные значения
                    originalDrag = heldObjectRb.drag;
                    originalAngularDrag = heldObjectRb.angularDrag;
                    
                    heldObjectRb.useGravity = false;
                    heldObjectRb.drag = 10;
                    heldObjectRb.angularDrag = 10;
                }

                isHolding = true;
            }
        }
    }

    void MoveHeldObject()
    {
        if (!isHolding || heldObject == null) return;

        Vector3 targetPosition = playerCamera.transform.position +
                               playerCamera.transform.forward * holdDistance;

        if (heldObjectRb != null)
        {
            heldObjectRb.velocity = (targetPosition - heldObject.transform.position) * smoothSpeed;

            // ����������� ��������
            heldObjectRb.angularVelocity = Vector3.zero;
            heldObject.transform.rotation = Quaternion.Lerp(
                heldObject.transform.rotation,
                playerCamera.transform.rotation,
                Time.deltaTime * smoothSpeed
            );
        }
        else
        {
            heldObject.transform.position = Vector3.Lerp(
                heldObject.transform.position,
                targetPosition,
                Time.deltaTime * smoothSpeed
            );
        }
    }

    void DropObject()
    {
        if (heldObjectRb != null)
        {
            heldObjectRb.useGravity = true;
            heldObjectRb.drag = originalDrag;
            heldObjectRb.angularDrag = originalAngularDrag;
        }

        heldObject = null;
        heldObjectRb = null;
        isHolding = false;
    }

    void OnGUI()
    {
        if (isLookingAtObject && !isHolding)
        {
            // Подсказка для подбора в инвентарь (E)
            if (!string.IsNullOrEmpty(lookAtItemName))
            {
                GUI.Label(new Rect(Screen.width / 2 - 120, Screen.height / 2 + 40, 240, 30),
                         "Нажмите E, чтобы подобрать");
            }
            
            // Подсказка для таскания предметов (F)
            GUI.Label(new Rect(Screen.width / 2 - 120, Screen.height / 2 + 80, 240, 30),
                     "Нажмите F чтобы тащить предмет");
        }
    }

    private void CacheServices()
    {
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

        if (inventory == null)
        {
            if (!GameServiceLocator.TryGet(out inventory))
            {
                var simpleInventory = FindObjectOfType<SimpleInventory>();
                if (simpleInventory != null)
                {
                    inventory = simpleInventory;
                    GameServiceLocator.Register<IInventory>(inventory);
                }
            }
        }

        if (pickupService == null)
        {
            if (!GameServiceLocator.TryGet(out pickupService))
            {
                pickupService = FindObjectOfType<ItemPickupService>();
                if (pickupService != null)
                {
                    GameServiceLocator.Register<IItemPickupService>(pickupService);
                }
                else
                {
                    GameObject serviceObject = new GameObject("ItemPickupService");
                    pickupService = serviceObject.AddComponent<ItemPickupService>();
                    DontDestroyOnLoad(serviceObject);
                }
            }
        }
    }

    private bool IsGamePaused()
    {
        if (gameStateService == null)
            GameServiceLocator.TryGet(out gameStateService);

        return gameStateService != null
            ? gameStateService.IsGamePaused
            : (GameStateManager.Instance != null && GameStateManager.Instance.IsGamePaused);
    }
}