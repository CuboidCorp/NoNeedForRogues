using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CowController : MonoBehaviour
{

    private PlayerControls controls;
    private PlayerControls.PlayerActions playerActions;

    // <summary>
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

    #region Movement Variables

    #region Moving
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
    [SerializeField] private float jumpPower = 1f;

    #endregion

    #endregion

    #region Camera Movement Variables
    [HideInInspector] public Camera playerCamera; //TODO : Voir pk c'est public
    [HideInInspector] public GameObject copyCam; //Le parent de la grabzone
    [SerializeField] private GameObject cameraPivot; //Le gameObject de la camera

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


    private void Awake()
    {
        controls = new PlayerControls();
        playerActions = controls.Player;

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        playerActions.Move.performed += ctx => OnMove(ctx);
        playerActions.Move.canceled += ctx => moveInput = Vector2.zero;
        playerActions.Jump.performed += ctx => Jump();
        playerActions.Look.performed += ctx => Look(ctx.ReadValue<Vector2>());
        playerActions.Run.started += ctx => StartRun();
        playerActions.Run.canceled += ctx => StopRun();
    }

    /// <summary>
    /// Recoit l'input du joueur pour se déplacer
    /// </summary>
    /// <param name="ctx">Le contexte contenant les valeurs</param>
    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    /// <summary>
    /// Fait sauter le joueur si il est au sol
    /// </summary>
    private void Jump()
    {
        if (isGrounded)
        {
            animator.SetTrigger("Jump");
            rb.AddForce(Vector3.up * Mathf.Sqrt(2 * jumpPower), ForceMode.Impulse);
        }
    }

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


}
