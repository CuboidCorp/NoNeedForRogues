using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class MonPlayerController : Entity
{

    private Rigidbody rb;
    private PlayerControls controls;
    private PlayerControls.PlayerActions playerActions;
    private PlayerControls.UIActions uiActions;

    [SerializeField] private int seed = 0;

    private Animator animator;

    [SerializeField] private float interactDistance = 5f;

    [SerializeField] private GameObject vivox;

    private Vector3 lastCheckPoint = new(0, 1, 0);//Le dernier checkpoint où le joueur a été

    #region Camera Movement Variables

    public Camera playerCamera;
    [SerializeField] private GameObject cameraPivot;

    [SerializeField] private bool invertCamera = false;
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private float minLookAngle = 50f;

    [SerializeField] private float fov = 60f;
    [SerializeField] private float sprintFOV = 80f;
    [SerializeField] private float fovChangeSpeed = 5f;

    // Internal Variables
    private float yaw = 0.0f;
    private float pitch = 0.0f;

    #endregion

    #region Movement Variables

    #region Moving
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 moveInput;
    private float moveSpeed;
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 5f;

    //private float maxSpeed = 10f;    //private float maxSpeed = 10f;

    private bool isWalking = false;
    private bool isRunning = false;

    #endregion

    #region Jump
    private bool isGrounded = false;
    [SerializeField] private float jumpPower = 1f;

    #endregion

    #endregion

    #region Prefabs
    private GameObject ghostPlayerPrefab;

    #endregion 

    private VivoxVoiceConnexion voiceConnexion;

    /// <summary>
    /// Awake is called when the script instance is being loaded
    /// </summary>
    private void Awake()
    {
        controls = new PlayerControls();
        playerActions = controls.Player;
        uiActions = controls.UI;

        playerActions.Move.performed += ctx => OnMove(ctx);
        playerActions.Move.canceled += ctx => moveInput = Vector2.zero;
        playerActions.Jump.performed += ctx => Jump();
        playerActions.Look.performed += ctx => Look(ctx.ReadValue<Vector2>());
        playerActions.Run.started += ctx => StartRun();
        playerActions.Run.canceled += ctx => StopRun();
        playerActions.BasicAttack.started += ctx => StartBasicAttack();
        playerActions.BasicAttack.performed += ctx => StopBasicAttack();
        playerActions.BasicAttack.canceled += ctx => StopBasicAttack();
        playerActions.LongAttack.started += ctx => StartLongAttack();
        playerActions.LongAttack.performed += ctx => StopLongAttack();
        playerActions.LongAttack.canceled += ctx => StopLongAttack();

        playerActions.Interact.performed += ctx => Interact();

        playerActions.Emote1.started += ctx => StartEmote1();
        playerActions.Emote2.started += ctx => StartEmote2();
        playerActions.Emote3.started += ctx => StartEmote3();
        playerActions.Emote4.started += ctx => StartEmote4();
        playerActions.Emote5.started += ctx => StartEmote5();
        playerActions.Emote6.started += ctx => StartEmote6();
        playerActions.Emote7.started += ctx => StartEmote7();
        playerActions.Emote8.started += ctx => StartEmote8();
        playerActions.Emote9.started += ctx => StartEmote9();
        playerActions.Emote10.started += ctx => StartEmote10();

        animator = transform.GetComponentInChildren<Animator>();

        voiceConnexion = vivox.GetComponent<VivoxVoiceConnexion>();

        //On recupere le prefab de la ragdoll
        ghostPlayerPrefab = Resources.Load<GameObject>("Perso/GhostPlayer");


        //On randomize le joueur

        if (seed == 0)
        {
            seed = Random.Range(0, 100000);
        }
    }

    /// <summary>
    /// Quand le joueur spawn dans le jeu
    /// </summary>
    public async override void OnNetworkSpawn()
    {
        gameObject.GetComponent<PlayerRandomizer>().Randomize(seed);
        DisableRagdoll();
        if (IsOwner) //Quand on est le proprietaire on passe en mode premiere personne et on desactive toutes les parties du corps sauf les mains
        {
            if (MultiplayerGameManager.Instance.soloMode)
            {
                gameObject.tag = "Player";
            }
            ChangerRenderCorps(ShadowCastingMode.ShadowsOnly);
            transform.position = new Vector3(0, 1, 0);

            await voiceConnexion.InitVivox();

        }
        else //Si on est pas le propriétaire du joueur, on desactive le script
        {
            gameObject.GetComponent<SpellRecognition>().enabled = false;
            Destroy(vivox);
            cameraPivot.SetActive(false);
            enabled = false;
        }
    }

    /// <summary>
    /// Passe les parties du corps en mode première personne en les mettant sur une autre layer
    /// </summary>
    private void ChangerRenderCorps(ShadowCastingMode shadow)
    {
        //On desactive les child 0 a 3 pr le premier child
        //Ce qui correspond à épaulières, genouières, ceinture, cape
        for (int i = 0; i < 4; i++)
        {
            //On recupere les skinned mesh renderer dans leurs enfants et on met leur option de rendu sur shadow only
            foreach (SkinnedMeshRenderer smr in transform.GetChild(0).GetChild(i).GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.shadowCastingMode = shadow;
            }
        }

        //On desactive les child 0 a 7 pr le deuxieme child
        //Ce qui correspond à la tete, le torse, les cheveux, les jambes, les pieds, les moustaches, les yeux, les sourcils
        for (int i = 0; i < 8; i++)
        {
            //On recupere les skinned mesh renderer dans leurs enfants et on met leur option de rendu sur shadow only
            foreach (SkinnedMeshRenderer smr in transform.GetChild(1).GetChild(i).GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.shadowCastingMode = shadow;
            }
        }

    }

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;

        moveSpeed = walkSpeed;
        playerCamera.fieldOfView = fov;
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
        CheckGround();
    }

    #region Movement


    /// <summary>
    /// Recoit l'input du joueur pour se déplacer
    /// </summary>
    /// <param name="ctx">Le contexte contenant les valeurs</param>
    private void OnMove(InputAction.CallbackContext ctx)
    {
        StopEmotes();
        moveInput = ctx.ReadValue<Vector2>();
        animator.SetFloat("SpeedX", moveInput.x);
        animator.SetFloat("SpeedZ", moveInput.y);
    }

    /// <summary>
    /// Gere le mouvement du joueur
    /// </summary>
    private void MovePlayer()
    {
        isWalking = moveInput != Vector2.zero;
        if (isWalking)
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
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, sprintFOV, Time.deltaTime * fovChangeSpeed);
        }
        else
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fov, Time.deltaTime * fovChangeSpeed);
        }

    }

    /// <summary>
    /// Quand on appuye sur la touche pour courir, on augmente la vitesse de déplacement
    /// </summary>
    private void StartRun()
    {
        isRunning = true;
        animator.SetBool("isRunning", true);
        moveSpeed = runSpeed;
    }

    /// <summary>
    /// Fin de la course, on remet la vitesse de déplacement à la normale
    /// </summary>
    private void StopRun()
    {
        isRunning = false;
        animator.SetBool("isRunning", false);
        moveSpeed = walkSpeed;
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

    #endregion

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

    #region Degats et Mort

    /// <summary>
    /// Inflige des dégats au joueur
    /// </summary>
    /// <param name="damage">Le nombre de degats infligés</param>
    /// <returns>True si le joueur est mort, false sinon</returns>
    public void Damage(float damage)
    {
        if (!IsOwner)
        {
            //On gère les dégats sur chaque client pour éviter les problèmes de synchro
            ClientRpcParams clientParams = new()
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } }
            };
            HandleDamageClientRpc(damage, clientParams);
            return;
        }
        StopEmotes();
        animator.SetTrigger("GotHurt");
        StopEmotes();
        vie -= damage;
        if (vie <= 0)
        {
            Die();
            return;
        }
        return;
    }

    /// <summary>
    /// Gère les dégats sur le client propriétaire
    /// </summary>
    /// <param name="clientRpcParams">Parametre de la rpc pr permettre de target un joueur en particulier</param>
    /// <param name="damage">Les degats à infliger</param>
    [ClientRpc]
    private void HandleDamageClientRpc(float damage, ClientRpcParams clientRpcParams)
    {
        Debug.Log("HandleDamageClientRpc");
        Damage(damage);
    }

    /// <summary>
    /// Gère la mort du joueur, soit on remplace le joueur par sa ragdoll
    /// </summary>
    private void Die()
    {
        SendDeathServerRpc(OwnerClientId);
        animator.SetTrigger("Died");
        gameObject.GetComponent<SpellRecognition>().enabled = false;
        StopEmotes();
        
        gameObject.tag = "Ragdoll";
        ChangerRenderCorps(ShadowCastingMode.On);

        SyncRagdollStateServerRpc(OwnerClientId, true);
        EnableRagdoll();

        controls.Disable();
        SpawnGhostPlayerServerRpc(OwnerClientId);
    }

    /// <summary>
    /// Demande au serveur de spawn le ghost du joueur
    /// </summary>
    /// <param name="ownerId">L'id du joueur qui spawn son ghost</param>
    [ServerRpc(RequireOwnership = false)]
    private void SpawnGhostPlayerServerRpc(ulong ownerId)
    {
        GameObject ghost = Instantiate(ghostPlayerPrefab, transform.position, transform.rotation);
        ghost.name = "GhostPlayer" + OwnerClientId;
        ghost.GetComponent<NetworkObject>().Spawn();
        ghost.GetComponent<NetworkObject>().ChangeOwnership(ownerId);

        HandleGhostSpawnClientRpc(ghost);
    }

    /// <summary>
    /// Permet de gérer le spawn du ghost sur le client
    /// </summary>
    /// <param name="networkRef">La reference à l'objet</param>
    [ClientRpc]
    private void HandleGhostSpawnClientRpc(NetworkObjectReference networkRef)
    {
        GameObject ghostObj = (GameObject)networkRef;
        ghostObj.name = "GhostPlayer" + ghostObj.GetComponent<NetworkObject>().OwnerClientId;
        Debug.Log("Ghost Owner"+ghostObj.GetComponent<NetworkObject>().OwnerClientId+" Mon Owner"+OwnerClientId);
        if(ghostObj.GetComponent<NetworkObject>().OwnerClientId == OwnerClientId)
        {
            OnGhostSpawn();
        }
    }

    /// <summary>
    /// Quand le ghost est spawn, on le lie au joueur
    /// </summary>
    private void OnGhostSpawn()
    {
        GameObject ghost = GameObject.Find("GhostPlayer"+OwnerClientId);
        ghost.GetComponent<GhostController>().root = gameObject;
        ghost.GetComponent<GhostController>().vivox = vivox;
        vivox.transform.parent = ghost.transform;

        
        cameraPivot.SetActive(false);
        enabled = false;
    }

    /// <summary>
    /// Gère la mort quand on est pas le propriétaire du joueur
    /// </summary>
    public void HandleDeath()
    {
        animator.SetTrigger("Died");
        StopEmotes();
        gameObject.tag = "Ragdoll";
    }

    /// <summary>
    /// Envoie l'information de la mort du joueur au serveur
    /// </summary>
    /// <param name="ownerId">L'id du joueur mort</param>
    [ServerRpc]
    private void SendDeathServerRpc(ulong ownerId)
    {
        MultiplayerGameManager.Instance.SyncDeath(ownerId);
    }

    /// <summary>
    /// L'inverse de la mort, on remet le joueur en vie
    /// </summary>
    public void Respawn() //TODO : Sync le respawn a tt les clients
    {
        transform.position = lastCheckPoint;
        gameObject.tag = "Player";
        ChangerRenderCorps(ShadowCastingMode.ShadowsOnly);
        SyncRagdollStateServerRpc(OwnerClientId, false);
        DisableRagdoll();
        gameObject.GetComponent<SpellRecognition>().enabled = true;
        cameraPivot.SetActive(true);
    }

    /// <summary>
    /// Handle le respawn d'un joueur dont on est pas propriétaire
    /// </summary>
    public void HandleRespawn()
    {
        gameObject.tag = "Player";
    }

    /// <summary>
    /// Change le point de respawn du joueur
    /// </summary>
    /// <param name="position">Le nouveau point de checkpoint</param>
    public void SetRespawnPoint(Vector3 position)
    {
        lastCheckPoint = position;
    }
    #endregion

    #region Ragdoll

    /// <summary>
    /// Desactive la ragdoll du joueur
    /// </summary>
    public void DisableRagdoll()
    {
        animator.enabled = true;
        gameObject.GetComponent<CapsuleCollider>().enabled = true;
        gameObject.GetComponent<Rigidbody>().isKinematic = false;
        //On passe tous les rigidbodies des gosses en kinematic et on desactive les colliders
        foreach (Rigidbody rb in GetRagdollRigidbodies())
        {
            rb.isKinematic = true;
        }
        foreach (Collider col in transform.GetChild(2).GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

    }

    /// <summary>
    /// Active la ragdoll du joueur
    /// </summary>
    public void EnableRagdoll()
    {
        animator.enabled = false;
        gameObject.GetComponent<CapsuleCollider>().enabled = false;
        gameObject.GetComponent<Rigidbody>().isKinematic = true;
        //On passe tous les rigidbodies des gosses en kinematic et on desactive les colliders
        foreach (Rigidbody rb in GetRagdollRigidbodies())
        {
            rb.isKinematic = false;
        }
        foreach (Collider col in transform.GetChild(2).GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
        }
    }

    /// <summary>
    /// Permet de sync l'etat de la ragdoll d'un joueur aux autres
    /// </summary>
    /// <param name="playerId">Le joueur qui change l'etat de sa ragdoll</param>
    /// <param name="ragdollActive">Si la ragdoll deviient active ou inactive</param>
    [ServerRpc]
    private void SyncRagdollStateServerRpc(ulong playerId, bool ragdollActive)
    {
        MultiplayerGameManager.Instance.SyncRagdoll(playerId, ragdollActive);
    }

    /// <summary>
    /// Renvoie la liste des rigidbodies de la ragdoll
    /// </summary>
    /// <returns>La </returns>
    public Rigidbody[] GetRagdollRigidbodies()
    {
        return transform.GetChild(2).GetComponentsInChildren<Rigidbody>();
    }

    /// <summary>
    /// Met le joueur en ragdoll pour un temps donné
    /// </summary>
    /// <param name="time">Le temps pendant lequel le joueur est en ragdoll</param>
    /// <returns>Quand le joueur n'est plus en ragdoll</returns>
    public IEnumerator SetRagdollTemp(float time)
    {
        controls.Disable();
        ChangerRenderCorps(ShadowCastingMode.On);
        SyncRagdollStateServerRpc(OwnerClientId, true);
        EnableRagdoll();
        yield return new WaitForSeconds(time);
        ChangerRenderCorps(ShadowCastingMode.ShadowsOnly);
        SyncRagdollStateServerRpc(OwnerClientId, false);
        DisableRagdoll();
        controls.Enable();
    }

    #endregion

    #region Attaques

    /// <summary>
    /// Commence l'animation d'attaque de base
    /// </summary>
    private void StartBasicAttack()
    {
        animator.SetBool("isAttacking", true);
    }

    /// <summary>
    /// Fin de l'animation d'attaque de base
    /// </summary>
    private void StopBasicAttack()
    {
        animator.SetBool("isAttacking", false);
    }

    /// <summary>
    /// Commence l'animation d'attaque longue
    /// </summary>
    private void StartLongAttack()
    {
        Debug.Log("LongAttack");
        GetComponent<SpellRecognition>().StartListening();
        animator.SetTrigger("isLongAttacking");
    }

    /// <summary>
    /// Fin de l'animation d'attaque longue
    /// </summary>
    private void StopLongAttack()
    {
        Debug.Log("StopLongAttack");
        GetComponent<SpellRecognition>().StopListening();
        animator.SetBool("isLongAttacking", false);
    }

    #endregion

    #region Emotes

    /// <summary>
    /// Commence l'animation de l'emote 1
    /// </summary>
    private void StartEmote1()
    {
        animator.SetTrigger("Emote1");
    }

    /// <summary>
    /// Commence l'animation de l'emote 2
    /// </summary>
    private void StartEmote2()
    {
        animator.SetTrigger("Emote2");
    }

    /// <summary>
    /// Commence l'animation de l'emote 3
    /// </summary>
    private void StartEmote3()
    {
        animator.SetTrigger("Emote3");
    }

    /// <summary>
    /// Commence l'animation de l'emote 4
    /// </summary>
    private void StartEmote4()
    {
        animator.SetTrigger("Emote4");
    }

    /// <summary>
    /// Commence l'animation de l'emote 5
    /// </summary>
    private void StartEmote5()
    {
        animator.SetTrigger("Emote5");
    }

    /// <summary>
    /// Commence l'animation de l'emote 6
    /// </summary>
    private void StartEmote6()
    {
        animator.SetTrigger("Emote6");
    }

    /// <summary>
    /// Commence l'animation de l'emote 7
    /// </summary>
    private void StartEmote7()
    {
        animator.SetTrigger("Emote7");
    }

    /// <summary>
    /// Commence l'animation de l'emote 8
    /// </summary>
    private void StartEmote8()
    {
        animator.SetTrigger("Emote8");
    }

    /// <summary>
    /// Commence l'animation de l'emote 9
    /// </summary>
    private void StartEmote9()
    {
        animator.SetTrigger("Emote9");
    }

    /// <summary>
    /// Commence l'animation de l'emote 10
    /// </summary>
    private void StartEmote10()
    {
        animator.SetTrigger("Emote10");
    }

    /// <summary>
    /// Arrête toutes les emotes
    /// </summary>
    private void StopEmotes()
    {
        //On force à remettre à zéro tt avec le trigger reset
        animator.SetTrigger("ResetEmotes");
    }

    #endregion

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
    /// Permet d'interagir avec les objets qui sont interactables
    /// </summary>
    private void Interact()
    {

        //On fait un draw ray pr voir si on touche un objet interactable
#if UNITY_EDITOR
        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactDistance, Color.yellow, 1f);
#endif

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, interactDistance))
        {
            if (hit.collider.TryGetComponent(out Interactable interactable))
            {
                interactable.OnInteract();
            }
        }
    }

    /// <summary>
    /// Interagir avec un objet à une distance plus grande
    /// </summary>
    /// <param name="interactDist">La distance d'interaction</param>
    public void InteractSpell(float interactDist)
    {
        //On fait un draw ray pr voir si on touche un objet interactable
#if UNITY_EDITOR
        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactDist, Color.yellow, 1f);
#endif

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, interactDist))
        {
            if (hit.collider.TryGetComponent(out Interactable interactable))
            {
                interactable.OnInteract();
            }
        }
    }

}
