using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class MonPlayerController : Entity
{
    private Rigidbody rb;
    public PlayerControls controls;
    private PlayerControls.PlayerActions playerActions;

    [HideInInspector] public static MonPlayerController instanceLocale;

    [SerializeField] private int seed = 0;

    private Animator animator;

    [SerializeField] private float interactDistance = 5f;

    private GameObject vivox;

    [SerializeField]
    private Vector3 lastCheckPoint = new(0, 1, 0);//Le dernier checkpoint où le joueur a été

    public GameObject playerUI;

    #region Camera Variables

    [Header("Camera Movement Variables")]

    [HideInInspector] public Camera playerCamera;
    [SerializeField] private GameObject cameraPivot; //Le gameObject de la camera
    [SerializeField] private GameObject camTps; //Le gameObject de la camera troisieme personne
    [SerializeField] private GameObject flashlight;

    [SerializeField] private bool invertCamera = false;
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private float minLookAngle = 50f;

    [SerializeField] private float fov = 60f;
    [SerializeField] private float sprintFOV = 80f;
    [SerializeField] private float fovChangeSpeed = 5f;

    private Coroutine deactivateFlashlight;

    public float smoothTime = 0.05f;

    private float xRotation = 0f;
    private Vector2 currentMouseDelta;
    private Vector2 currentMouseDeltaVelocity;

    [SerializeField] private Color baseLightColor;
    [SerializeField] private Color nightVisionLightColor;

    #endregion

    public bool isPaused = false;

    #region Movement Variables
    [Header("Movement Variables")]
    #region Moving
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 moveInput;
    private float moveSpeed;
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 5f;

    [SerializeField] private float groundMultiplier = 10f;
    [SerializeField] private float airMultiplier = 0.4f;

    [SerializeField] private float boostMaxBonusSpeed = 2f;
    private float boostBonusSpeed = 0f;

    [SerializeField] private float groundDrag = 6f;
    [SerializeField] private float airDrag = 2f;

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
    private GameObject cowPlayerPrefab;
    #endregion

    #region Sound Effects
    [SerializeField] private AudioSource castingAudioSource;
    [SerializeField] private AudioSource movementAudioSource;
    [SerializeField] private AudioMixer mainAudioMixer;
    #endregion

    private VivoxVoiceConnexion voiceConnexion;

    [SerializeField] private int poisonDamageInterval = 1;

    /// <summary>
    /// Awake is called when the script instance is being loadedok 
    /// </summary>
    private void Awake()
    {
        controls = new PlayerControls();
        playerActions = controls.Player;
        ChargerOptions();

        playerActions.Move.performed += OnMove;
        playerActions.Move.canceled += _ => moveInput = Vector2.zero;
        playerActions.Jump.performed += _ => Jump();
        playerActions.Run.started += _ => StartRun();
        playerActions.Run.canceled += _ => StopRun();
        playerActions.LongAttack.started += _ => StartCasting();
        playerActions.LongAttack.performed += _ => StopCasting();
        playerActions.LongAttack.canceled += _ => StopCasting();

        playerActions.Interact.performed += _ => Interact();

        playerActions.Emote1.started += _ => StartEmote1();
        playerActions.Emote2.started += _ => StartEmote2();
        playerActions.Emote3.started += _ => StartEmote3();
        playerActions.Emote4.started += _ => StartEmote4();
        playerActions.Emote5.started += _ => StartEmote5();
        playerActions.Emote6.started += _ => StartEmote6();
        playerActions.Emote7.started += _ => StartEmote7();
        playerActions.Emote8.started += _ => StartEmote8();
        playerActions.Emote9.started += _ => StartEmote9();
        playerActions.Emote10.started += _ => StartEmote10();

        playerActions.Pause.performed += _ => PlayerUIManager.Instance.ShowPauseMenu();

        healthSlider = PlayerUIManager.Instance.healthSlider;
        healthText = PlayerUIManager.Instance.healthText;
        manaSlider = PlayerUIManager.Instance.manaSlider;
        manaText = PlayerUIManager.Instance.manaText;
        IntiliazeUi();

        animator = transform.GetComponentInChildren<Animator>();
        playerCamera = cameraPivot.GetComponent<Camera>();
        voiceConnexion = transform.GetComponentInChildren<VivoxVoiceConnexion>();
        vivox = voiceConnexion.gameObject;

        ghostPlayerPrefab = Resources.Load<GameObject>("Perso/GhostPlayer");
        cowPlayerPrefab = Resources.Load<GameObject>("Perso/Cow");

        //On randomize le joueur
        if (seed == 0)
        {
            seed = Random.Range(0, 100000);
        }
    }

    /// <summary>
    /// Charge les valeurs par défaut des playerPrefs
    /// </summary>
    public void ChargerOptions()
    {
        float volumeMain = PlayerPrefs.GetFloat("mainVolume", 1);
        float volumeMusique = PlayerPrefs.GetFloat("musicVolume", .3f);
        float volumeSfx = PlayerPrefs.GetFloat("sfxVolume", 1);
        float volumeVoix = PlayerPrefs.GetFloat("voiceVolume", 1);

        mainAudioMixer.SetFloat("mainVolume", Mathf.Log10(volumeMain) * 20);
        mainAudioMixer.SetFloat("musicVolume", Mathf.Log10(volumeMusique) * 20);
        mainAudioMixer.SetFloat("sfxVolume", Mathf.Log10(volumeSfx) * 20);
        mainAudioMixer.SetFloat("voiceVolume", Mathf.Log10(volumeVoix) * 20 + 10);

        invertCamera = PlayerPrefs.GetInt("inverseCam", 0) != 0;
        mouseSensitivity = PlayerPrefs.GetFloat("cameraSensi", 100);

        bool fullscreen = PlayerPrefs.GetInt("fullscreen", 1) != 0;
        Screen.fullScreen = fullscreen;

    }


    /// <summary>
    /// Quand le joueur spawn dans le jeu
    /// </summary>
    public override void OnNetworkSpawn()
    {
        gameObject.GetComponent<PlayerRandomizer>().Randomize(seed);
        DisableRagdoll(false);
        if (!IsHost)
        {
            //Si on est pas un hote on desactive le configDonjonUI
            ConfigDonjonUI.Instance.enabled = false;
        }
        if (IsOwner) //Quand on est le proprietaire on passe en mode premiere personne et on desactive toutes les parties du corps sauf les mains
        {

            GameObject mainCam = GameObject.Find("UiCamera");
            if (mainCam != null)
            {
                Destroy(mainCam);
            }

            AudioManager.instance.SetMusic(AudioManager.Musique.TAVERNE);
            AudioManager.instance.ActivateMusic();

            playerUI.SetActive(false); //On desactive notre propre UI

            instanceLocale = this;

            PlayerUIManager.Instance.SetupPlayerControls(controls);
            PlayerUIManager.Instance.AfficherInGameUI();

            ChangerRenderCorps(ShadowCastingMode.ShadowsOnly);
            transform.position = new Vector3(0, 1, 0);

        }
        else //Si on est pas le propriétaire du joueur, on desactive le script
        {
            gameObject.GetComponent<SpellRecognition>().enabled = false;
            //gameObject.GetComponent<PickUpController>().enabled = false;
            Destroy(vivox);
            Destroy(cameraPivot.GetComponent<AudioListener>());
            Destroy(cameraPivot.GetComponent<UniversalAdditionalCameraData>());
            Destroy(cameraPivot.GetComponent<Camera>());
            Destroy(camTps.GetComponent<AudioListener>());
            Destroy(camTps.GetComponent<UniversalAdditionalCameraData>());
            Destroy(camTps.GetComponent<Camera>());
            enabled = false;
        }
    }

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        rb = GetComponent<Rigidbody>();

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
    /// Lance la connexion au voice chat de Vivox
    /// </summary>
    public async void JoinVivox()
    {
        await voiceConnexion.InitVivox();
    }

    /// <summary>
    /// Quitte le voice chat vivox
    /// </summary>
    public async void LeaveVivox()
    {
        await voiceConnexion.LeaveVivox();
    }

    private void LateUpdate()
    {
        if (!isPaused)
        {
            Look();
        }
        else
        {
            currentMouseDelta = Vector2.zero;
        }
    }

    /// <summary>
    /// Update meilleur pr les checks car appelé à chaque frame
    /// </summary>
    private void FixedUpdate()
    {
        MovePlayer();
        ControlDrag();
        CheckGround();
        CheckInteract();
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

        transform.Rotate(Vector3.up * currentMouseDelta.x);

        Vector3 moveDirection = new(moveInput.x, 0f, moveInput.y);

        if (isGrounded)
        {
            rb.AddForce(groundMultiplier * moveSpeed * transform.TransformDirection(moveDirection), ForceMode.Acceleration);
        }
        else
        {
            rb.AddForce(airMultiplier * moveSpeed * transform.TransformDirection(moveDirection), ForceMode.Acceleration);
        }



        if (isRunning)
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, sprintFOV, Time.deltaTime * fovChangeSpeed);
        }
        else
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fov, Time.deltaTime * fovChangeSpeed);
        }

    }

    private void ControlDrag()
    {
        if (isGrounded)
        {
            rb.drag = groundDrag;
        }
        else
        {
            rb.drag = airDrag;
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
    /// Fait un saut plus grand multiplier par un bonus
    /// </summary>
    /// <param name="bonus">Multiplicateur du saut</param>
    public void GreaterJump(float bonus)
    {
        StatsManager.Instance.AddJump();
        animator.SetTrigger("Jump");
        rb.AddForce(Vector3.up * Mathf.Sqrt(2 * jumpPower * bonus), ForceMode.Impulse);
    }

    /// <summary>
    /// Fait un dash dans une direction donnée
    /// </summary>
    /// <param name="direction">Direction dy dash</param>
    /// <param name="force">Force du dash</param>
    public void Dash(Vector3 direction, float force)
    {
        rb.AddForce(direction * force, ForceMode.Impulse);
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
    public override void Damage(float damage)
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
        AudioManager.instance.PlayOneShotClipServerRpc(transform.position, AudioManager.SoundEffectOneShot.PLAYER_DAMAGED);
        StatsManager.Instance.AddDamageTaken(damage);
        StopEmotes();
        animator.SetTrigger("GotHurt");
        base.Damage(damage);
    }

    /// <summary>
    /// Gère les dégats sur le client propriétaire
    /// </summary>
    /// <param name="clientRpcParams">Parametre de la rpc pr permettre de target un joueur en particulier</param>
    /// <param name="damage">Les degats à infliger</param>
    [ClientRpc]
    private void HandleDamageClientRpc(float damage, ClientRpcParams clientRpcParams)
    {
        Damage(damage);
    }

    /// <summary>
    /// Heal le joueur 
    /// </summary>
    /// <param name="heal">Le nombre de points de vie rendus</param>
    public override void Heal(float heal)
    {
        if (!IsOwner)
        {
            //On gère les dégats sur chaque client pour éviter les problèmes de synchro
            ClientRpcParams clientParams = new()
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } }
            };
            HandleHealClientRpc(heal, clientParams);
            return;
        }
        StatsManager.Instance.AddHealAmount(heal);
        base.Heal(heal);
    }

    /// <summary>
    /// Gère le healing sur le client propriétaire
    /// </summary>
    /// <param name="clientRpcParams">Parametre de la rpc pr permettre de target un joueur en particulier</param>
    /// <param name="healAmount">Les degats à soigner</param>
    [ClientRpc]
    private void HandleHealClientRpc(float healAmount, ClientRpcParams clientRpcParams)
    {
        Heal(healAmount);
    }

    /// <summary>
    /// Gère la mort du joueur, soit on remplace le joueur par sa ragdoll
    /// </summary>
    public override void Die()
    {
        Debug.Log("Mort du joueur");

        currentHealth = 0;
        healthSlider.value = currentHealth;
        healthText.text = currentHealth + "/" + MaxHP;

        AudioManager.instance.PlayOneShotClipServerRpc(transform.position, AudioManager.SoundEffectOneShot.PLAYER_DEAD);
        StopAllCoroutines(); //Pr faire gaffe au poison
        SendDeathServerRpc(OwnerClientId);
        animator.SetTrigger("Died");

        gameObject.GetComponent<SpellRecognition>().enabled = false;
        gameObject.GetComponent<PickUpController>().enabled = false;

        StatsManager.Instance.AddMort();


        ChangerRenderCorps(ShadowCastingMode.On);

        MultiplayerGameManager.Instance.SyncRagdollStateServerRpc(OwnerClientId, true);
        tag = "Ragdoll";
        EnableRagdoll(false);

        controls.Disable();
        SpawnGhostPlayerServerRpc(OwnerClientId);
    }

    /// <summary>
    /// Teleporte le corps du joueur au checkpoint pour eviter des bugs
    /// </summary>
    public void TpSpawn()
    {
        if (IsOwner)
        {
            transform.position = lastCheckPoint;
        }
        else
        {
            MultiplayerGameManager.Instance.TpSpawnServerRpc(OwnerClientId);
        }
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
        ghost.GetComponent<NetworkObject>().SpawnWithOwnership(ownerId);

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
        if (MultiplayerGameManager.Instance.soloMode)
        {
            MultiplayerGameManager.Instance.SetDeadChannelTap();
        }
        if (ghostObj.GetComponent<NetworkObject>().OwnerClientId == NetworkManager.Singleton.LocalClientId)
        {
            OnGhostSpawn();
        }
        else
        {
            MultiplayerGameManager.Instance.MovePlayerTapToGhost(ghostObj.GetComponent<NetworkObject>().OwnerClientId);
            ghostObj.transform.GetChild(0).gameObject.SetActive(false);
            ghostObj.GetComponent<GhostController>().enabled = false;
        }
    }

    /// <summary>
    /// Quand le ghost est spawn et que c'est le notre, on le lie au joueur
    /// </summary>
    private void OnGhostSpawn()
    {
        GameObject ghost = GameObject.Find("GhostPlayer" + OwnerClientId);
        ghost.GetComponent<GhostController>().root = gameObject;
        ghost.GetComponent<GhostController>().vivox = vivox;
        vivox.transform.parent = ghost.transform;
        ghost.transform.GetChild(0).gameObject.SetActive(true); //Le camera pivot du ghost
        ghost.transform.GetChild(1).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        cameraPivot.SetActive(false);
        if (!MultiplayerGameManager.Instance.isInLobby)
        {
            RenderSettings.ambientLight = nightVisionLightColor;
        }
        enabled = false;
    }

    /// <summary>
    /// Envoie l'information de la mort du joueur au serveur
    /// </summary>
    /// <param name="ownerId">L'id du joueur mort</param>
    [ServerRpc(RequireOwnership = false)]
    private void SendDeathServerRpc(ulong ownerId)
    {
        MultiplayerGameManager.Instance.SyncDeath(ownerId);
    }

    /// <summary>
    /// L'inverse de la mort, on remet le joueur en vie
    /// </summary>
    public void Respawn()
    {
        transform.position = lastCheckPoint;
        if (MultiplayerGameManager.Instance.soloMode)
        {
            MultiplayerGameManager.Instance.SetNormalChannelTap();
        }
        FullHeal();
        ChangerRenderCorps(ShadowCastingMode.ShadowsOnly);
        MultiplayerGameManager.Instance.SyncRagdollStateServerRpc(OwnerClientId, false);
        tag = "Player";
        DisableRagdoll(false);
        gameObject.GetComponent<PickUpController>().enabled = true;
        gameObject.GetComponent<SpellRecognition>().enabled = true;

        if (!MultiplayerGameManager.Instance.isInLobby)
        {
            RenderSettings.ambientLight = baseLightColor;
        }
        cameraPivot.SetActive(true);
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
    public void DisableRagdoll(bool changeCam)
    {
        if (changeCam)
        {
            cameraPivot.SetActive(true);
            camTps.SetActive(false);
        }
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
    public void EnableRagdoll(bool changeCam)
    {
        StopCasting();
        StopEmotes();
        GetComponent<PickUpController>().DropObject();
        if (changeCam)
        {
            cameraPivot.SetActive(false);
            camTps.SetActive(true);
        }
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
    /// Renvoie la liste des rigidbodies de la ragdoll
    /// </summary>
    /// <returns>La liste des rigidbody</returns>
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
        MultiplayerGameManager.Instance.SyncRagdollStateServerRpc(OwnerClientId, true);
        tag = "Ragdoll";
        EnableRagdoll(true);
        yield return new WaitForSeconds(time);
        ChangerRenderCorps(ShadowCastingMode.ShadowsOnly);
        MultiplayerGameManager.Instance.SyncRagdollStateServerRpc(OwnerClientId, false);
        tag = "Player";
        DisableRagdoll(true);
        controls.Enable();
    }

    #endregion

    #region Casting

    /// <summary>
    /// Commence le casting de sort 
    /// </summary>
    private void StartCasting()
    {
        GetComponent<SpellRecognition>().StartListening();
        animator.SetTrigger("isLongAttacking");
        ChangeCastingSFXStateServerRpc(true);
    }

    /// <summary>
    /// Fin de l'animation d'attaque longue
    /// </summary>
    private void StopCasting()
    {
        GetComponent<SpellRecognition>().StopListening();
        animator.SetBool("isLongAttacking", false);
        ChangeCastingSFXStateServerRpc(false);
    }

    /// <summary>
    /// Demande au serveur de synchroniser l'etat du sound effect 
    /// </summary>
    /// <param name="isActive">Si l'audiosource doit être active ou non</param>
    [ServerRpc(RequireOwnership = false)]
    private void ChangeCastingSFXStateServerRpc(bool isActive)
    {
        ChangeCastingSFXClientRpc(isActive);
    }

    /// <summary>
    /// Active ou desactive l'audio source de casting
    /// </summary>
    /// <param name="isActive">Si l'audiosource doit être active ou non</param>
    [ClientRpc]
    private void ChangeCastingSFXClientRpc(bool isActive)
    {
        castingAudioSource.enabled = isActive;
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
    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * (invertCamera ? -1 : 1);

        Vector2 targetMouseDelta = new(mouseX, mouseY);
        currentMouseDelta = Vector2.SmoothDamp(currentMouseDelta, targetMouseDelta, ref currentMouseDeltaVelocity, smoothTime);

        xRotation -= currentMouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -minLookAngle, minLookAngle);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
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

    #endregion

    #region Interactions

    /// <summary>
    /// Vérifie si le joueur peut interagir avec un objet et active l'ui en conséquence
    /// </summary>
    private void CheckInteract()
    {
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, interactDistance))
        {
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                PlayerUIManager.Instance.ShowInteractText(interactable.GetInteractText());
            }
            else
            {
                PlayerUIManager.Instance.HideInteractText();
            }
        }
        else
        {
            PlayerUIManager.Instance.HideInteractText();
        }
    }

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
            if (hit.collider.TryGetComponent(out IInteractable interactable))
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
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                interactable.OnInteract();
            }
        }
    }

    #endregion

    #region Sorts

    /// <summary>
    /// Sort de francois qui affiche le screamer de francois et sa tete
    /// </summary>
    /// <returns></returns>
    public IEnumerator SortFrancois()
    {
        //On affiche françois sur l'écran et on joue le son
        PlayerUIManager.Instance.francois.SetActive(true);
        AudioManager.instance.PlayOneShotClipServerRpc(transform.position, AudioManager.SoundEffectOneShot.SCREAM);
        yield return new WaitForSeconds(1f);
        //On cache françois
        PlayerUIManager.Instance.francois.SetActive(false);
    }

    /// <summary>
    /// Reçoit un speed boost et lance une coroutine pour le finir
    /// </summary>
    /// <param name="buffDuration">Durée du buff</param>
    public void ReceiveSpeedBoost(float buffDuration)
    {
        boostBonusSpeed += boostMaxBonusSpeed;
        if (MultiplayerGameManager.Instance.soloMode)
        {
            MultiplayerGameManager.Instance.SetSpeedyChannelTap();
        }
        else
        {
            MultiplayerGameManager.Instance.SetSpeedyPlayerTapServerRpc(OwnerClientId);
        }
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
        if (boostBonusSpeed == 0)
        {
            if (MultiplayerGameManager.Instance.soloMode)
            {
                MultiplayerGameManager.Instance.SetNormalChannelTap();
            }
            else
            {
                MultiplayerGameManager.Instance.ResetPlayerTapServerRpc(OwnerClientId);
            }
        }
    }

    #region Polymorph

    /// <summary>
    /// Polymorph en vacche
    /// </summary>
    public void Polymorph()
    {
        SpawnCowServerRpc(OwnerClientId);
    }

    /// <summary>
    /// Demande au serveur de spawn la vache du joueur
    /// </summary>
    /// <param name="ownerId">L'id du joueur qui spawn sa vache</param>
    [ServerRpc(RequireOwnership = false)]
    private void SpawnCowServerRpc(ulong ownerId)
    {
        GameObject cow = Instantiate(cowPlayerPrefab, transform.position, transform.rotation);
        cow.GetComponent<NetworkObject>().SpawnWithOwnership(ownerId);

        HandleCowSpawnClientRpc(cow);
    }

    [ClientRpc]
    private void HandleCowSpawnClientRpc(NetworkObjectReference networkRef)
    {
        GameObject cowObj = (GameObject)networkRef;
        cowObj.name = "Cow" + cowObj.GetComponent<NetworkObject>().OwnerClientId;
        if (cowObj.GetComponent<NetworkObject>().OwnerClientId == NetworkManager.Singleton.LocalClientId)
        {
            OnCowSpawn();
        }
        else
        {
            MultiplayerGameManager.Instance.MovePlayerTapToCow(cowObj.GetComponent<NetworkObject>().OwnerClientId);
            cowObj.transform.GetChild(0).gameObject.SetActive(false); // La camera pivot de la vache
            cowObj.GetComponent<CowController>().enabled = false;
        }
    }

    /// <summary>
    /// Quand on spawn la vache, on la lie au joueur
    /// </summary>
    private void OnCowSpawn()
    {
        MultiplayerGameManager.Instance.SyncRagdollStateServerRpc(OwnerClientId, true);
        tag = "Ragdoll";
        EnableRagdoll(false);
        gameObject.GetComponent<PickUpController>().enabled = false;
        gameObject.GetComponent<SpellRecognition>().enabled = false;


        GameObject cow = GameObject.Find("Cow" + OwnerClientId);
        cow.GetComponent<CowController>().root = gameObject;
        cow.GetComponent<CowController>().vivox = vivox;
        cow.GetComponent<CowController>().StartTurnBack(5);
        vivox.transform.parent = cow.transform;
        cow.transform.GetChild(0).gameObject.SetActive(true); //Le camera pivot du ghost
        cow.transform.GetChild(1).GetComponent<SkinnedMeshRenderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly; //On desactive le corps de la vache
        cameraPivot.SetActive(false);
        enabled = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Return from cow state
    /// </summary>
    public void Uncow()
    {
        MultiplayerGameManager.Instance.SyncRagdollStateServerRpc(OwnerClientId, false);
        tag = "Player";
        DisableRagdoll(false);
        gameObject.GetComponent<PickUpController>().enabled = true;
        gameObject.GetComponent<SpellRecognition>().enabled = true;
        cameraPivot.SetActive(true);
    }

    #endregion

    /// <summary>
    /// Lance la coroutine de poison sur le joueur
    /// </summary>
    /// <param name="poisonDamage">Degats de poison pris a chaque tick de poison</param>
    /// <param name="poisonTime">Duree du poison en secondes</param>
    public void StartPoison(float poisonDamage, int poisonTime)
    {
        StartCoroutine(DoPoisonDamage(poisonDamage, poisonTime));
    }

    /// <summary>
    /// Coroutine qui tick pour infliger des degats de poisons tous les poisonDamageInterval pendant poisonTime secondes
    /// </summary>
    /// <param name="poisonDamage">Degats de poison a chaque tick</param>
    /// <param name="poisonTime">Duree du poison</param>
    private IEnumerator DoPoisonDamage(float poisonDamage, int poisonTime)
    {
        int cptTime = 0;
        while (cptTime < poisonTime)
        {
            yield return new WaitForSeconds(poisonDamageInterval);
            cptTime += poisonDamageInterval;
            Damage(poisonDamage);
        }
    }

    /// <summary>
    /// Allume la flash light et l'eteint apres un certain temps
    /// </summary>
    /// <param name="flashDuration">La duree d'éclairage</param>
    /// <param name="francoisMode">Si on a la tete de francois ou pas</param>
    public void StartFlash(int flashDuration, bool francoisMode)
    {
        if (francoisMode)
        {
            flashlight.GetComponent<Light>().cookie = Resources.Load<Texture>("Textures/francois");
        }
        else
        {
            flashlight.GetComponent<Light>().cookie = null;
        }
        flashlight.SetActive(true);
        if (deactivateFlashlight != null)
        {
            StopCoroutine(deactivateFlashlight);
        }
        deactivateFlashlight = StartCoroutine(DeactivateFlashlight(flashDuration));
    }

    private IEnumerator DeactivateFlashlight(int flashDuration)
    {
        yield return new WaitForSeconds(flashDuration);
        flashlight.SetActive(false);
    }

    /// <summary>
    /// Active la vision nocturne
    /// </summary>
    public void NightVision()
    {
        RenderSettings.ambientLight = nightVisionLightColor;
    }

    #endregion

    public void DesactiverMouvement()
    {
        controls.Disable();
    }

    public void ActiverMouvement()
    {
        controls.Enable();
    }

    /// <summary>
    /// Gère la deconnexion du joueur
    /// </summary>
    public void Deconnexion()
    {
        LeaveVivox();
        GetComponent<PickUpController>().DropObject();
        Destroy(MultiplayerGameManager.Instance.gameObject);
        Destroy(AudioManager.instance.gameObject);
        if (GenerationDonjon.instance != null)
        {
            Destroy(GenerationDonjon.instance.gameObject);
        }
    }

    /// <summary>
    /// On gère si qqn quitte le jeu
    /// </summary>
    public void OnApplicationQuit()
    {
        LeaveVivox();
        GetComponent<PickUpController>().DropObject();
        NetworkManager.Singleton.Shutdown();
    }
}
