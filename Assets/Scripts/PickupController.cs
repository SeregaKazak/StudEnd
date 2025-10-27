using UnityEngine;
using System.Collections;

public class PickupController : MonoBehaviour
{
    [Header("Настройки подбора")]
    public float pickupDistance = 3f;
    public float holdDistance = 1.5f;
    public float smoothSpeed = 10f;

    private Camera playerCamera;
    private GameObject heldObject;
    private Rigidbody heldObjectRb;
    private bool isLookingAtObject = false;
    private bool isHolding = false;

    void Start()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        CheckObject();
        HandleInput();
        MoveHeldObject();
    }

    void CheckObject()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupDistance))
        {
            if (hit.collider.CompareTag("Item") && !isHolding)
            {
                isLookingAtObject = true;
                return;
            }
        }

        isLookingAtObject = false;
    }

    void HandleInput()
    {
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

            // Блокировка вращения
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
            heldObjectRb.drag = 1;
            heldObjectRb.angularDrag = 0.5f;
        }

        heldObject = null;
        heldObjectRb = null;
        isHolding = false;
    }

    void OnGUI()
    {
        if (isLookingAtObject && !isHolding)
        {
            GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 50, 200, 30),
                     "Нажмите 'F' чтобы поднять");
        }
    }
}