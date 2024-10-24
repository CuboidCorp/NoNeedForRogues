using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class CowController : NetworkBehaviour
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

    private Animator animator;
    private Rigidbody rb;

    public Coroutine turnBackCoroutine;

    #region Movement Variables

    #region Moving
    [Header("Movement Variables")]
    [SerializeField] private Transform groundCheck;
    private Vector2 moveInput;
    private float moveSpeed;
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 5f;

    [SerializeField] private float boostMaxBonusSpeed = 2f;
    private float boostBonusSpeed = 0f;

    private bool isRunning = false;

    #endregion

    #region Jump
    private bool isGrounded = false;
    [SerializeField] private float jumpPower = 7f;

    #endregion

    #endregion

    #region Camera Movement Variables
    [Header("Camera Movement Variables")]
    [SerializeField] private GameObject cameraPivot; //Le gameObject de la camera
    [SerializeField] private Camera playerCamera; //Le gameObject de la camera

    [SerializeField] private bool invertCamera = false;
    [SerializeField] private float mouseSensitivity = 60f;
    [SerializeField] private float minLookAngle = 50f;

    [SerializeField] private float fov = 60f;
    [SerializeField] private float sprintFov = 80f;
    [SerializeField] private float fovChangeSpeed = 5f;

    // Internal Variables
    private float yaw = 0.0f;
    private float pitch = 0.0f;
    #endregion


    private void Awake()
    {
        controls = new PlayerControls();
        playerActions = controls.Player;

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        playerActions.Move.performed += ctx => OnMove(ctx);
        playerActions.Move.canceled += ctx => moveInput = Vector2.zero;
        playerActions.Jump.performed += ctx => Jump();
        playerActions.Run.started += ctx => StartRun();
        playerActions.Run.canceled += ctx => StopRun();

        controls.Enable();
    }

    #region Mouvement
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
        if (moveInput != Vector2.zero)
        {
            animator.SetBool("isWalking", true);
        }
        else
        {
            animator.SetBool("isWalking", false);
            StopRun();
        }

        Vector3 moveDirection = new(moveInput.x, 0f, moveInput.y);

        rb.MovePosition(rb.position + moveSpeed * Time.fixedDeltaTime * transform.TransformDirection(moveDirection));

        if (isRunning)
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, sprintFov, Time.deltaTime * fovChangeSpeed);
        }
        else
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fov, Time.deltaTime * fovChangeSpeed);
        }

    }

    /// <summary>
    /// Fait sauter le joueur si il est au sol
    /// </summary>
    private void Jump()
    {
        if (isGrounded)
        {
            StatsManager.Instance.AddJump();
            animator.SetTrigger("Jump");
            rb.AddForce(Vector3.up * Mathf.Sqrt(2 * jumpPower), ForceMode.Impulse);
        }
    }

    /// <summary>
    /// Quand on appuye sur la touche pour courir, on augmente la vitesse de déplacement
    /// </summary>
    private void StartRun()
    {
        isRunning = true;
        animator.SetBool("isRunning", true);
        moveSpeed = runSpeed + boostBonusSpeed;
    }

    /// <summary>
    /// Fin de la course, on remet la vitesse de déplacement à la normale
    /// </summary>
    private void StopRun()
    {
        isRunning = false;
        animator.SetBool("isRunning", false);
        moveSpeed = walkSpeed + boostBonusSpeed;
    }

    /// <summary>
    /// Vérifie si le joueur est au sol
    /// </summary>
    private void CheckGround()
    {

        if (Physics.Raycast(transform.position, transform.up * -1, .2f))
        {
#if UNITY_EDITOR
            Debug.DrawRay(transform.position, transform.up * -.2f, Color.green);
#endif
            isGrounded = true;
        }
        else
        {
#if UNITY_EDITOR
            Debug.DrawRay(transform.position, transform.up * -.2f, Color.red);
#endif
            isGrounded = false;
        }
        animator.SetBool("isGrounded", isGrounded);
    }
    #endregion

    #region Camera
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

    private void FixedUpdate()
    {
        MovePlayer();
        CheckGround();
    }

    public void StartTurnBack(float time)
    {
        if (turnBackCoroutine != null)
        {
            StopCoroutine(turnBackCoroutine);
        }
        turnBackCoroutine = StartCoroutine(TurnBackIn(time));
    }

    /// <summary>
    /// Redeviens un humain au bout de time s
    /// </summary>
    /// <param name="time">Le temps avant de redevenir humain</param>
    /// <returns></returns>
    private IEnumerator TurnBackIn(float time)
    {
        yield return new WaitForSeconds(time);
        UnCow();
    }

    /// <summary>
    /// Return to human
    /// </summary>
    public void UnCow()
    {
        controls.Disable();
        if (turnBackCoroutine != null)
        {
            StopCoroutine(turnBackCoroutine);
        }
        turnBackCoroutine = null;
        root.SetActive(true);
        root.transform.position = transform.position;
        root.GetComponent<MonPlayerController>().enabled = true;
        root.GetComponent<MonPlayerController>().Uncow();
        vivox.transform.parent = root.transform;

        MultiplayerGameManager.Instance.SyncUncowServerRpc(gameObject, OwnerClientId);
    }

    /// <summary>
    /// Reçoit un speed boost et lance une coroutine pour le finir
    /// </summary>
    /// <param name="buffDuration">Durée du buff</param>
    public void ReceiveSpeedBoost(float buffDuration)
    {
        boostBonusSpeed += boostMaxBonusSpeed;
        StartCoroutine(EndSpeedBoost(buffDuration));
    }

    /// <summary>
    /// Supprime un speed boost au bout d'un certain temps
    /// </summary>
    /// <param name="time">Le temps au bout du quel le speed boost est fini</param>
    /// <returns></returns>
    private IEnumerator EndSpeedBoost(float time)
    {
        yield return new WaitForSeconds(time);
        boostBonusSpeed -= boostMaxBonusSpeed;
        if (isRunning)
        {
            moveSpeed = runSpeed + boostBonusSpeed;
        }
        else
        {
            moveSpeed = walkSpeed + boostBonusSpeed;
        }
    }

}
