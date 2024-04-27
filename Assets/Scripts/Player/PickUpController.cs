using UnityEngine;

public class PickUpController : MonoBehaviour
{
    [Header("Pick Up Settings")]
    public Transform holdArea;
    [SerializeField] private Camera playerCam;
    private GameObject heldObj;
    private Rigidbody heldObjRb;

    [Header("Physics Settings")]
    [SerializeField] private float pickupRange = 5.0f;
    [SerializeField] private float pickupForce = 100.0f;

    [SerializeField] private float throwForce = 10f; //Quand on lance l'objet

    /// <summary>
    /// Essaye d'attraper un objet à portée
    /// </summary>
    /// <returns>L'objet à attraper</returns>
    public bool TryGrabObject()
    {
        if(heldObj != null)
        {
            return false;
        }
        if (Physics.Raycast(playerCam.transform.position, playerCam.transform.forward, out RaycastHit hit, pickupRange))
        {
            if (hit.collider.CompareTag("PickUp"))
            {
                if(hit.collider.gameObject.GetComponent<WeightedObject>().isHeld.Value == true)
                {
                    return false;
                }
                PickupObject(hit.collider.gameObject);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// On pick up l'objet
    /// </summary>
    /// <param name="objetRamasse">Le gameObject qu'on pick up</param>
    private void PickupObject(GameObject objetRamasse)
    {
        heldObj = objetRamasse;
        heldObj.GetComponent<WeightedObject>().isHeld.Value = true;
        heldObjRb = heldObj.GetComponent<Rigidbody>();
        heldObjRb.useGravity = false;
        heldObjRb.drag = 10;
        heldObjRb.constraints = RigidbodyConstraints.FreezeRotation;

        heldObj.transform.SetParent(holdArea);
    }

    /// <summary>
    /// Drop l'objet tenu si il y en a un
    /// </summary>
    public void DropObject()
    {
        if(heldObj == null)
        {
            return;
        }
        //On le drop
        heldObj.GetComponent<WeightedObject>().isHeld.Value = false;
        heldObjRb.useGravity = true;
        heldObjRb.drag = 1;
        heldObjRb.constraints = RigidbodyConstraints.None;

        heldObj.transform.parent = null;

        heldObjRb = null;
        heldObj = null;
    }

    /// <summary>
    /// Lance l'objet tenu si il y en a un
    /// </summary>
    public void ThrowObject()
    {
        if (heldObj == null)
        {
            return;
        }
        heldObj.GetComponent<WeightedObject>().isHeld.Value = false;
        heldObjRb.useGravity = true;
        heldObjRb.drag = 1;
        heldObjRb.constraints = RigidbodyConstraints.None;
        heldObjRb.AddForce(playerCam.transform.forward * throwForce, ForceMode.Impulse);
        
        heldObj.transform.parent = null;

        heldObjRb = null;
        heldObj = null;
    }

    private void Update()
    {
        if(heldObj != null)
        {
            MoveObject();
        }
    }

    /// <summary>
    /// Bouge l'objet vers la zone de hold
    /// </summary>
    private void MoveObject()
    {
        if (Vector3.Distance(heldObj.transform.position, holdArea.position) > 0.1f)
        {
            Vector3 moveDirection = (holdArea.position - heldObj.transform.position);
            heldObjRb.AddForce(pickupForce * moveDirection);
        }
        else
        {
            heldObjRb.velocity = Vector3.zero;
        }
    }


}
