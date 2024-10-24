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

    public float smoothTime = 0.05f;

    private float xRotation = 0f;
    private Vector2 currentMouseDelta;
    private Vector2 currentMouseDeltaVelocity;

    #endregion

    #region Movement Variables

    #region Moving
    private Vector2 moveInput;
    private float moveInputVertical;

    private float moveSpeed;
    [SerializeField] private float normalSpeed = 3f;
    [SerializeField] private float boostSpeed = 5f;

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
        playerActions.Run.started += ctx => StartBoost();
        playerActions.Run.canceled += ctx => StopBoost();
        playerActions.Jump.started += ctx => moveInputVertical = 1;
        playerActions.Jump.canceled += ctx => moveInputVertical = 0;
        playerActions.Crouch.started += ctx => moveInputVertical = -1;
        playerActions.Crouch.canceled += ctx => moveInputVertical = 0;

        invertCamera = PlayerPrefs.GetInt("inverseCam", 0) != 0;
        mouseSensitivity = PlayerPrefs.GetFloat("cameraSensi", 100);

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

    private void LateUpdate()
    {
        Look();
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

        transform.Rotate(Vector3.up * currentMouseDelta.x);

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

    #region Gestion de la caméra

    /// <summary>
    /// Gère la rotation de la caméra du joueur
    /// </summary>
    /// <param name="direction">La direction ou on regarde</param>
    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * (invertCamera ? -1 : 1);

        Vector2 targetMouseDelta = new(mouseX, mouseY);
        currentMouseDelta = Vector2.SmoothDamp(currentMouseDelta, targetMouseDelta, ref currentMouseDeltaVelocity, smoothTime);

        // Rotation verticale (regarder vers le haut et vers le bas)
        xRotation -= currentMouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -minLookAngle, minLookAngle); // Limite la rotation verticale entre -90 et 90 degrés

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
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

        MultiplayerGameManager.Instance.SyncRespawnServerRpc(gameObject, OwnerClientId);
    }
}
