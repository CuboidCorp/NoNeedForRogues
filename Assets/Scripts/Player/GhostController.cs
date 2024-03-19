using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controller type freecam pr quand on est mort
/// </summary>
[DisallowMultipleComponent]
public class GhostController : NetworkBehaviour
{
    private PlayerControls controls;
    private PlayerControls.PlayerActions playerActions;

    /// <summary>
    /// La référence au joueur
    /// </summary>
    [HideInInspector]
    public GameObject root;
    /// <summary>
    /// L'objet vivox qui permet la connexion au chat vocal
    /// </summary>
    [HideInInspector]
    public GameObject vivox;

    #region Camera Movement Variables

    public Camera playerCamera;

    [SerializeField] private bool invertCamera = false;
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private float minLookAngle = 50f;

    [SerializeField] private float fov = 60f;
    [SerializeField] private float boostFov = 80f;
    [SerializeField] private float fovChangeSpeed = 5f;

    // Internal Variables
    private float yaw = 0.0f;
    private float pitch = 0.0f;

    #endregion

    #region Movement Variables

    #region Moving
    private Vector2 moveInput;
    private float moveInputVertical;

    private float moveSpeed;
    [SerializeField] private float normalSpeed = 3f;
    [SerializeField] private float boostSpeed = 5f;

    //private float maxSpeed = 10f;    //private float maxSpeed = 10f;

    private bool isWalking = false;
    private bool isBoosting = false;

    #endregion

    #endregion 

    /// <summary>
    /// Awake is called when the script instance is being loaded
    /// </summary>
    private void Awake()
    {
        controls = new PlayerControls();
        playerActions = controls.Player;

        playerActions.Move.performed += ctx => OnMove(ctx);
        playerActions.Move.canceled += ctx => moveInput = Vector2.zero;
        playerActions.Look.performed += ctx => Look(ctx.ReadValue<Vector2>());
        playerActions.Run.started += ctx => StartBoost();
        playerActions.Run.canceled += ctx => StopBoost();
        playerActions.Jump.started += ctx => moveInputVertical = 1;
        playerActions.Jump.canceled += ctx => moveInputVertical = 0;
        playerActions.Crouch.started += ctx => moveInputVertical = -1;
        playerActions.Crouch.canceled += ctx => moveInputVertical = 0;
    }

    /// <summary>
    /// Quand on active le script, on active les inputs
    /// </summary>
    private void OnEnable()
    {
        controls.Enable();
    }

    /// <summary>
    /// Quand on désactive le script, on désactive les inputs
    /// </summary>
    private void OnDisable()
    {
        controls.Disable();
    }

    /// <summary>
    /// Update meilleur pr les checks car appelé à chaque frame
    /// </summary>
    private void FixedUpdate()
    {
        MovePlayer();
    }

    #region Movement


    /// <summary>
    /// Recoit l'input du joueur pour se déplacer
    /// </summary>
    /// <param name="ctx">Le contexte contenant les valeurs</param>
    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    /// <summary>
    /// Gere le mouvement du joueur
    /// </summary>
    private void MovePlayer()
    {
        isWalking = moveInput != Vector2.zero;
        if (!isWalking)
        {
            StopBoost();
        }

        Vector3 moveDirection = new(moveInput.x, moveInputVertical, moveInput.y);

        transform.Translate(moveSpeed * Time.fixedDeltaTime * moveDirection);

        if (isBoosting)
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, boostFov, Time.deltaTime * fovChangeSpeed);
        }
        else
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fov, Time.deltaTime * fovChangeSpeed);
        }

    }

    /// <summary>
    /// Quand on appuye sur la touche pour courir, on augmente la vitesse de déplacement
    /// </summary>
    private void StartBoost()
    {
        isBoosting = true;
        moveSpeed = boostSpeed;
    }

    /// <summary>
    /// Fin de la course, on remet la vitesse de déplacement à la normale
    /// </summary>
    private void StopBoost()
    {
        isBoosting = false;
        moveSpeed = normalSpeed;
    }

    #endregion

    /// <summary>
    /// Quand l'objet est spawn, on désactive le script de mouvement du joueur
    /// </summary>
    public override void OnNetworkSpawn()
    {
        Debug.Log("Spawn mon owner est :" + OwnerClientId);
        if(IsOwner)
        {
            //On active la caméra du joueur
            transform.GetChild(0).gameObject.SetActive(true);
            transform.GetChild(1).GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
        else
        {
            transform.GetChild(0).gameObject.SetActive(false);
            enabled = false;
        }
    }


    #region Gestion de la caméra

    /// <summary>
    /// Gère la rotation de la caméra du joueur
    /// </summary>
    /// <param name="direction">La direction ou on regarde</param>
    private void Look(Vector2 direction)
    {
        if (invertCamera)
        {
            direction.y *= -1;
        }
        yaw += direction.x * mouseSensitivity * Time.deltaTime;
        pitch -= direction.y * mouseSensitivity * Time.deltaTime;

        pitch = Mathf.Clamp(pitch, -minLookAngle, minLookAngle);

        playerCamera.transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);

        transform.eulerAngles = new Vector3(0.0f, yaw, 0.0f);
    }

    #endregion

    /// <summary>
    /// Respawn du joueur
    /// </summary>
    public void Respawn()
    {
        root.GetComponent<MonPlayerController>().enabled = true;
        root.GetComponent<MonPlayerController>().Respawn();
        vivox.transform.parent = root.transform;

        Destroy(gameObject);
    }
}
