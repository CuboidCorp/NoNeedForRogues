using Unity.Netcode;
using UnityEngine;

public class PickUpController : NetworkBehaviour
{
    [Header("Pick Up Settings")]
    [SerializeField] private Transform holdArea;
    [SerializeField] private Camera playerCam;
    private GameObject heldObj;
    private GameObject copieObj;
    private Rigidbody copieRb;
    public bool isRotating = false;
    [SerializeField] private float rotationSensitivity = 2;
    

    [Header("Physics Settings")]
    [SerializeField] private float pickupRange = 5.0f;
    [SerializeField] private float pickupForce = 100.0f; //TODO : Reduire ça pr tester

    [SerializeField] private float throwForce = 10f; //Quand on lance l'objet

    [Header("Trajectoire")]
    [SerializeField] private bool showTrajectory = false;
    [SerializeField] private int lineSegmentCount = 20;
    LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponentInChildren<LineRenderer>();
    }

    /// <summary>
    /// Si le joueur tient l'objet ou non
    /// </summary>
    /// <returns>True si il tient qqch, false sinon</returns>
    public bool IsHoldingObject()
    {
        return heldObj != null;
    }

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
        heldObj.GetComponent<WeightedObject>().SendLastOwnerServerRpc(GetComponent<NetworkObject>().OwnerClientId);
        heldObj.GetComponent<WeightedObject>().ChangeState(true);
        SubstituteRealForCopy();
    }

    #region Systeme Copie

    /// <summary>
    /// Cache le vrai objet et renvoie la copie créée
    /// </summary>
    private void SubstituteRealForCopy()
    {
        heldObj.SetActive(false);
        string cheminCopie = heldObj.GetComponent<WeightedObject>().cheminCopie;
        MultiplayerGameManager.Instance.SummonCopieObjetServerRpc(heldObj, cheminCopie, OwnerClientId);
    }

    /// <summary>
    /// Crée une copie en la chargeant depuis les resources
    /// </summary>
    /// <param name="cheminCopie">Le chemin de la copie dans les resources</param>
    public void CreeCopie(string cheminCopie)
    {
        GameObject prefabObj = Resources.Load<GameObject>(cheminCopie);
        copieObj = Instantiate(prefabObj, holdArea);
        copieObj.transform.position = holdArea.position;
        copieRb = copieObj.GetComponent<Rigidbody>();
        copieRb.drag = 10;
        copieRb.constraints = RigidbodyConstraints.FreezeRotation;
        DisableCollision();
    }

    /// <summary>
    /// Supprime la copie
    /// </summary>
    public void SupprimerCopie()
    {
        Destroy(copieObj);
    }

    /// <summary>
    /// Supprime la copie sur tt le monde et remet le nouvel objet pr de vrai
    /// </summary>
    /// <param name="force">La force à appliquer à l'objet</param>
    private void SubstituteCopyForReal(Vector3 force)
    {
        Vector3 posCopie = copieObj.transform.position;
        Vector3 rotCopie = copieObj.transform.rotation.eulerAngles;
        MultiplayerGameManager.Instance.DestroyCopieServerRpc(heldObj, OwnerClientId);
        SetObjectDataServerRpc(heldObj, posCopie, rotCopie, force);
    }

    #endregion


    [ServerRpc(RequireOwnership = false)]
    private void SetObjectDataServerRpc(NetworkObjectReference obj, Vector3 newPosition, Vector3 rotation, Vector3 force)
    {
        ((GameObject)obj).transform.position = newPosition;
        ((GameObject)obj).transform.rotation = Quaternion.Euler(rotation);
        ((GameObject)obj).GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);

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
        if (isRotating)
        {
            MonPlayerController.instanceLocale.StopRotation();
        }
        isRotating = false;
        SubstituteCopyForReal(Vector3.zero);
        StopClipping();
        heldObj.GetComponent<WeightedObject>().ChangeState(false);
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
        if (isRotating)
        {
            MonPlayerController.instanceLocale.StopRotation();
        }
        isRotating = false;
        SubstituteCopyForReal(playerCam.transform.forward * throwForce);
        heldObj.GetComponent<WeightedObject>().ChangeState(false);
        StopClipping();
        heldObj = null;
    }

    /// <summary>
    /// Desactive la collision entre le copie objet et tous les joueurs
    /// </summary>
    private void DisableCollision()
    {
        GameObject[] players = MultiplayerGameManager.Instance.GetAllPlayersGo();
        foreach (GameObject player in players)
        {
            Physics.IgnoreCollision(copieObj.GetComponent<Collider>(), player.GetComponent<Collider>(), true);
        }
    }

    private void Update()
    {
        if (copieObj != null)
        {
            MoveObject();
            if(showTrajectory)
            {
                DrawTrajectory();
            }
        }
    }

    #region Rotation

    /// <summary>
    /// Tourne l'objet tenu
    /// </summary>
    /// <param name="direction"></param>
    public void RotateObject(Vector2 direction)
    {
        if (copieObj != null)
        {
            isRotating = true;
            //Fait tourner l'objet sur lui meme
            float XaxisRotation = direction.x * rotationSensitivity;
            float YaxisRotation = direction.y * rotationSensitivity;
            //rotate the object depending on mouse X-Y Axis
            copieObj.transform.Rotate(Vector3.down, XaxisRotation);
            copieObj.transform.Rotate(Vector3.right, YaxisRotation);
        }
    }
    #endregion

    /// <summary>
    /// Bouge l'objet vers la zone de hold
    /// </summary>
    private void MoveObject()
    {
        if (Vector3.Distance(copieObj.transform.position, holdArea.position) > 0.1f)
        {
            Vector3 moveDirection = (holdArea.position - copieObj.transform.position);
            copieRb.AddForce(pickupForce * moveDirection);
        }
        else
        {
            copieRb.velocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Empeche (En théorie) l'objet de drop derrière un mur et le remet en position 
    /// </summary>
    private void StopClipping()
    {
        float clipRange = Vector3.Distance(heldObj.transform.position, transform.position); //distance from holdPos to the camera
        //have to use RaycastAll as object blocks raycast in center screen
        //RaycastAll returns array of all colliders hit within the cliprange
        Ray ray = new(transform.position, transform.TransformDirection(Vector3.forward));
        //if the array length is greater than 1, meaning it has hit more than just the object we are carrying
        if (Physics.RaycastNonAlloc(ray, new RaycastHit[0], clipRange) > 1)
        {
            //change object position to camera position 
            heldObj.transform.position = transform.position + new Vector3(0f, -0.5f, 0f); //offset slightly downward to stop object dropping above player 
            //if your player is small, change the -0.5f to a smaller number (in magnitude) ie: -0.1f
        }
    }


    #region Trajectoire

    /// <summary>
    /// Change l'etat de l'affichage de la trajectoire
    /// </summary>
    public void SwitchShowTrajectoryState()
    {
        showTrajectory = !showTrajectory;
    }

    /// <summary>
    /// Dessine la trajectoire de l'objet en main si on venait à le lancer
    /// </summary>
    private void DrawTrajectory()
    {
        Vector3[] linePoints = new Vector3[lineSegmentCount];

        lineRenderer.positionCount = lineSegmentCount;

        Vector3 startPosition = copieObj.transform.position;
        Vector3 startVelocity = throwForce * copieObj.transform.forward;

        for (int i = 0 ; i < lineSegmentCount ; i++)
        {
            float time = (float)i / (lineSegmentCount - 1);
            linePoints[i] = startPosition + time * startVelocity;
            linePoints[i].y = startPosition.y + startVelocity.y * time + Physics.gravity.y / 2f * time * time;
        }

        lineRenderer.SetPositions(linePoints);
    }

    #endregion

    private void CreatePhysicsScene()
    {
        Scene simulationSccene = SceneManager.CreateScene("Simulation", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        PhysicsScene physicsScene = simulationSccene.GetPhysicsScene();

        //On veut recup les objets qui nous interessent donc les enfants des zones de trickshots et aussi les zones affectées (windzones)
        GameObject[] trickshotsZones = GameObject.FindGameObjectsWithTag("TrickshotZone");

        foreach(GameObject trickshotZone in trickshotsZones)
        {
            foreach(Transform enfant in trickshotZone.transform)
            {
                GameObject ghostObj = Instantiate(enfant.gameObject, enfant.position, enfant.rotation);
                if(ghostObj.TryGetComponent(out Renderer render))
                {
                    render.enabled = false;
                }
                if(ghostObj.TryGetComponent(out Ventilo vent))
                {

                }
                SceneManager.MoveGameObjectToScene(ghostObj, simulationSccene);
            }
        }
    }

}
