using UnityEngine;
using System.Collections;

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

    void Start()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        // Проверяем, не приостановлена ли игра
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsGamePaused)
            return;
            
        CheckObject();
        HandleInput();
        MoveHeldObject();
    }

    void CheckObject()
    {
        Vector3 mousePosition = Input.mousePosition;

        // Check if the mouse position is within the screen bounds
        Vector3 mousePos = Input.mousePosition;

        if (mousePos.x < 0 || mousePos.x > Screen.width ||
            mousePos.y < 0 || mousePos.y > Screen.height)
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
        // Подбор в инвентарь (E)
        if (Input.GetKeyDown(KeyCode.E) && !isHolding && !string.IsNullOrEmpty(lookAtItemName))
        {
            PickUpToInventory();
        }
        
        // Таскание предметов (F)
        if (Input.GetKeyDown(KeyCode.F))
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
        // Находим предмет по центру экрана
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupDistance))
        {
            if (hit.collider.CompareTag("Item"))
            {
                // Передаем предмет в инвентарь через SimpleInventory
                SimpleInventory inventoryScript = FindObjectOfType<SimpleInventory>();
                if (inventoryScript != null)
                {
                    // Создаем данные предмета
                    SimpleInventory.ItemData data = new SimpleInventory.ItemData();
                    data.itemName = hit.collider.name;
                    
                    // Сохраняем оригинальный префаб
                    data.prefab = hit.collider.gameObject;
                    
                    // Создаём копию для хранения в инвентаре
                    GameObject storedPrefab = Instantiate(data.prefab);
                    storedPrefab.SetActive(false);
                    data.prefab = storedPrefab;
                    
                    // Добавляем в инвентарь
                    inventoryScript.inventory.Add(data);
                    
                    // Удаляем оригинальный объект
                    Destroy(hit.collider.gameObject);
                    
                    Debug.Log("Подобран предмет в инвентарь: " + data.itemName);
                }
                
                lookAtItemName = "";
            }
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
                    heldObjectRb.useGravity = false;
                    heldObjectRb.linearDamping = 10;
                    heldObjectRb.angularDamping = 10;
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
            heldObjectRb.linearVelocity = (targetPosition - heldObject.transform.position) * smoothSpeed;

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
            heldObjectRb.linearDamping = 1;
            heldObjectRb.angularDamping = 0.5f;
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
}