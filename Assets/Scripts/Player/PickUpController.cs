using Unity.Netcode;
using UnityEngine;

public class PickUpController : NetworkBehaviour
{
    [Header("Pick Up Settings")]
    [HideInInspector] public Transform holdArea;
    [SerializeField] private Camera playerCam;
    private GameObject heldObj;
    private Rigidbody heldObjRb;
    private bool isRotating;
    [SerializeField]private float rotationSensitivity;

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
        if (heldObj != null)
        {
            return false;
        }
        if (Physics.Raycast(playerCam.transform.position, playerCam.transform.forward, out RaycastHit hit, pickupRange))
        {
            if (hit.collider.CompareTag("PickUp"))
            {
                if (hit.collider.gameObject.GetComponent<WeightedObject>().isHeld.Value == true)
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
        heldObj.GetComponent<WeightedObject>().ChangeState(true);
        heldObjRb = heldObj.GetComponent<Rigidbody>();
        SetHeldObjRbParamsServerRpc(heldObj, true);
        heldObjRb.isKinematic = false;
        MultiplayerGameManager.Instance.ChangeParentServerRpc(holdArea.gameObject, heldObj);
    }

    /// <summary>
    /// Drop l'objet tenu si il y en a un
    /// </summary>
    public void DropObject()
    {
        if (heldObj == null)
        {
            return;
        }
        //On le drop
        heldObj.GetComponent<WeightedObject>().ChangeState(false);
        SetHeldObjRbParamsServerRpc(heldObj, false);

        MultiplayerGameManager.Instance.RemoveParentServerRpc(heldObj);
        StopClipping();
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
        heldObj.GetComponent<WeightedObject>().ChangeState(false);
        SetHeldObjRbParamsServerRpc(heldObj, false);
        heldObjRb.AddForce(playerCam.transform.forward * throwForce, ForceMode.Impulse);

        MultiplayerGameManager.Instance.RemoveParentServerRpc(heldObj);
        StopClipping();
        heldObjRb = null;
        heldObj = null;
    }

    /// <summary>
    /// Envoie une serverRpc pour changer les paramètres du rigidbody de l'objet tenu
    /// </summary>
    /// <param name="networkObjectReference">La réference à l'objet tenu</param>
    /// <param name="estTenu">Si l'objet est tenu ou non</param>
    [ServerRpc(RequireOwnership = false)]
    private void SetHeldObjRbParamsServerRpc(NetworkObjectReference networkObjectReference, bool estTenu)
    {
        Rigidbody rb = ((GameObject)networkObjectReference).GetComponent<Rigidbody>();
        if (estTenu)
        {
            DisableCollision();
            rb.useGravity = false;
            rb.drag = 10;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        else
        {
            EnableCollision();
            rb.useGravity = true;
            rb.drag = 1;
            rb.constraints = RigidbodyConstraints.None;
        }
    }

    /// <summary>
    /// Réactive la collision entre l'objet et tous les joueurs
    /// </summary>
    private void EnableCollision(NetworkObjectReference networkObjectReference)
    {
        GameObject[] players = MultiplayerGameManager.Instance.GetAllPlayersGo();
        foreach(GameObject player in players)
        {
            Physics.IgnoreCollision(((GameObject)networkObjectReference).GetComponent<Collider>(), player.GetComponent<Collider>(), true);
        }
    }

    /// <summary>
    /// Desactive la collision entre l'objet et tous les joueurs
    /// </summary>
    private void DisableCollision()
    {
        GameObject[] players = MultiplayerGameManager.Instance.GetAllPlayersGo();
        foreach (GameObject player in players)
        {
            Physics.IgnoreCollision(((GameObject)networkObjectReference).GetComponent<Collider>(), player.GetComponent<Collider>(), false);
        }
    }


    private void Update()
    {
        if (heldObj != null)
        {
            MoveObject();
            if(isRotating)
            {
                RotateObject();
            }
        }

    }

    #region Rotation
    public void StartRotating()
    {
        //Desactivation de la rotation de la cam du joueur

    }

    public void StopRotating()
    {
        //Reactivation de la rotation de la cam
    }

    private void RotateObject()
    {
        //Fait tourner l'objet sur lui meme
        float XaxisRotation = Input.GetAxis("Mouse X") * rotationSensitivity;
        float YaxisRotation = Input.GetAxis("Mouse Y") * rotationSensitivity;
        //rotate the object depending on mouse X-Y Axis
        heldObj.transform.Rotate(Vector3.down, XaxisRotation);
        heldObj.transform.Rotate(Vector3.right, YaxisRotation);

    }

    #endregion

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

    void StopClipping() //function only called when dropping/throwing
    {
        var clipRange = Vector3.Distance(heldObj.transform.position, transform.position); //distance from holdPos to the camera
        //have to use RaycastAll as object blocks raycast in center screen
        //RaycastAll returns array of all colliders hit within the cliprange
        RaycastHit[] hits;
        hits = Physics.RaycastAll(transform.position, transform.TransformDirection(Vector3.forward), clipRange);
        //if the array length is greater than 1, meaning it has hit more than just the object we are carrying
        if (hits.Length > 1)
        {
            //change object position to camera position 
            heldObj.transform.position = transform.position + new Vector3(0f, -0.5f, 0f); //offset slightly downward to stop object dropping above player 
            //if your player is small, change the -0.5f to a smaller number (in magnitude) ie: -0.1f
        }
    }
}
