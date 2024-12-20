using UnityEngine.Windows.Speech;
using UnityEngine;
using System.Text;
using System;
using Unity.Netcode;

public class SpellRecognition : MonoBehaviour
{
    private KeywordRecognizer recognizer;

    #region Spell Stats
    private const float explosionRange = 5;
    private const float explosionForce = 10; //Les degats aussi

    private const float fireBallSpeed = 1;
    private const float fireBallExplosionRange = 5;
    private const float fireBallExplosionForce = 10;
    private const float fireBallTime = 3;

    private const float speedBoostDuration = 10;
    private const float speedBoostSpeed = 5;

    private const float fusrohdahSpeed = 3;
    private const float fusrohdahExplosionRange = 5;
    private const float fusrohdahExplosionForce = 10;
    private const float fusrohdahTime = 3;

    private const float jumpBonus = 10;

    private const float dashForce = 75;

    private const float healSpeed = 1;
    private const float healDuration = 5;
    private const float healAmount = 10;

    private const float resSpeed = 1;
    private const float resDuration = 5;

    private const float lightIntensity = 2;
    private const float flashbangIntensity = 100;
    private const float lightTime = 15;

    private const int flashlightTime = 20;

    private const float ragdollTime = 3;

    private const float interactRange = 10;

    private const float zoneVentForce = 20;
    private static Vector3 zoneVentSize = new(2, 2, 5);
    private static Vector3 zoneVentPos = new(1, 1, 2.5f); //TODO  : A changer pr les vraies valeurs
    private const float zoneVentDuration = 5;

    private const float highMultiplier = 1.5f;

    #endregion


    //Sort desactiv� : Polyphorphismus

    private readonly static string[] spells = {
        "Crepitus", "Lux", "Mortuus", "Infernum", "Sesamae occludit", "Penitus", "FusRoDah", "Capere", "Emitto", "Dimittas", "Resurrectio", "Acceleratio", "Curae", "Saltus", "Offendas", "Ventus", "Lucerna", //"Polyphorphismus",
        "Fran�ois Fran�ois Fran�ois", "Flashbang", "Fran�ois divin", //Sorts troll
        "DEBUG", "TPALL", "RAGDOLL", "TRESOR", "TRAJECTOIRE", "SEED", "VISION NOCTURNE", "SPAWN" //Sorts DEBUG
    };

    private bool debugMode = false;


    void Start()
    {
        recognizer = new KeywordRecognizer(spells);
        recognizer.OnPhraseRecognized += OnPhraseRecognized;
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        //C'est bien pr debug
        StringBuilder builder = new();
        builder.AppendFormat("{0} ({1}){2}", args.text, args.confidence, Environment.NewLine);
        builder.AppendFormat("Timestamp: {0}{1}", args.phraseStartTime, Environment.NewLine);
        builder.AppendFormat(" Duration: {0} seconds{1}", args.phraseDuration.TotalSeconds, Environment.NewLine);
        Debug.Log(builder.ToString());
        Color colorSpell = args.confidence switch
        {
            ConfidenceLevel.Medium => Color.yellow,
            ConfidenceLevel.High => Color.green,
            _ => Color.red,
        };

        float multiplier = args.confidence == ConfidenceLevel.High ? highMultiplier : 1;

        PlayerUIManager.Instance.SetSpellTexte(args.text, colorSpell);

        Vector3 posProj = gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward * 3f + gameObject.GetComponent<MonPlayerController>().playerCamera.transform.position;

        StatsManager.Instance.AddSpellCast();
        //AudioManager.instance.PlayOneShotClipServerRpc(transform.position, AudioManager.SoundEffectOneShot.SPELL_CAST);

        switch (args.text)
        {
            case "Crepitus":
                Explosion(transform, explosionRange * multiplier, explosionForce * multiplier);
                break;
            case "Lux":
                MultiplayerGameManager.Instance.SummonLightballServerRpc(posProj, lightIntensity * multiplier, lightTime * multiplier);
                break;
            case "Mortuus":
                gameObject.GetComponent<MonPlayerController>().Die();
                break;
            case "Infernum":
                MultiplayerGameManager.Instance.SummonFireBallServerRpc(posProj, gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward, fireBallSpeed * multiplier, fireBallExplosionRange, fireBallExplosionForce * multiplier, fireBallTime);
                break;
            case "Sesamae occludit":
                OpenSesame(gameObject.GetComponent<MonPlayerController>().playerCamera.transform, interactRange * multiplier);
                break;
            case "Penitus":
                gameObject.GetComponent<MonPlayerController>().InteractSpell(interactRange * multiplier);
                break;
            case "FusRoDah":
                MultiplayerGameManager.Instance.SummonFusrohdahServerRpc(gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward * 1f + gameObject.GetComponent<MonPlayerController>().playerCamera.transform.position, gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward, fusrohdahSpeed * multiplier, fusrohdahTime, fusrohdahExplosionRange * multiplier, fusrohdahExplosionForce * multiplier);
                break;
            case "Capere":
                gameObject.GetComponent<PickUpController>().TryGrabObject();
                break;
            case "Emitto":
                gameObject.GetComponent<PickUpController>().ThrowObject();
                break;
            case "Dimittas":
                gameObject.GetComponent<PickUpController>().DropObject();
                break;
            case "Resurrectio":
                MultiplayerGameManager.Instance.SummonResurectioServerRpc(posProj, gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward, resSpeed, resDuration);
                break;
            case "Acceleratio":
                //Fait bouger le joueur plus vite et change sa voix pour qu'elle soit plus aigue
                if (MultiplayerGameManager.Instance.soloMode)
                {
                    gameObject.GetComponent<MonPlayerController>().ReceiveSpeedBoost(speedBoostDuration * multiplier);
                }
                else
                {
                    MultiplayerGameManager.Instance.SummonAccelProjServerRpc(posProj, gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward, speedBoostSpeed, speedBoostDuration, speedBoostDuration * multiplier);
                }
                break;
            case "Curae":
                if (MultiplayerGameManager.Instance.soloMode)
                {
                    gameObject.GetComponent<MonPlayerController>().Heal(healAmount * multiplier);
                }
                else
                {
                    MultiplayerGameManager.Instance.SummonHealProjServerRpc(posProj, gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward, healSpeed, healDuration, healAmount * multiplier);
                }
                break;
            case "Saltus":
                gameObject.GetComponent<MonPlayerController>().GreaterJump(jumpBonus * multiplier);
                break;
            //case "Polyphorphismus":
            //    gameObject.GetComponent<MonPlayerController>().Polymorph();
            //    break;
            case "Offendas":
                Vector3 lookDir = gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward;
                gameObject.GetComponent<MonPlayerController>().Dash(lookDir, dashForce * multiplier);
                break;
            case "Ventus":
                MultiplayerGameManager.Instance.SummonZoneVentServerRpc(gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward * 1f + gameObject.GetComponent<MonPlayerController>().playerCamera.transform.position, gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward, zoneVentForce * multiplier, zoneVentSize, zoneVentPos, zoneVentDuration);
                break;
            case "Lucerna":
                MonPlayerController.instanceLocale.StartFlash((int)(flashlightTime * multiplier), false);
                break;

            case "Fran�ois Fran�ois Fran�ois":
                StartCoroutine(gameObject.GetComponent<MonPlayerController>().SortFrancois());
                break;
            case "Flashbang":
                MultiplayerGameManager.Instance.SummonLightballServerRpc(posProj, flashbangIntensity * multiplier, lightTime * multiplier);
                break;
            case "Fran�ois divin":
                MonPlayerController.instanceLocale.StartFlash((int)(flashlightTime * multiplier), true);
                break;

            #region SORTS DEBUG
            // SORTS DE DEBUG A PARTIR D'ICI
            case "DEBUG":
                debugMode = true;
                PlayerUIManager.Instance.SetDebugTexte("DEBUG");
                Debug.Log("Mode debug activ�");
                break;
            case "TPALL":
                if (debugMode)
                {
                    MultiplayerGameManager.Instance.TeleportAllServerRpc(gameObject.GetComponent<NetworkObject>().OwnerClientId);
                }
                else
                {
                    Debug.LogWarning("Le joueur a essay� de TPALL sans autorisation");
                }
                break;
            case "RAGDOLL":
                if (debugMode)
                {
                    StartCoroutine(gameObject.GetComponent<MonPlayerController>().SetRagdollTemp(ragdollTime));
                }
                else
                {
                    Debug.LogWarning("Le joueur a essay� de RAGDOLL sans autorisation");
                }
                break;
            case "TRESOR":
                if (debugMode)
                {
                    MultiplayerGameManager.Instance.SummonTresorServerRpc(transform.position);
                }
                else
                {
                    Debug.LogWarning("Le joueur a essay� de TRESOR sans autorisation");
                }
                break;
            case "TRAJECTOIRE":
                if (debugMode)
                {
                    GetComponent<PickUpController>().ShowTrajectory();
                }
                else
                {
                    Debug.LogWarning("Le joueur a essay� de TRAJECTOIRE sans autorisation");
                }
                break;
            case "SEED":
                if (debugMode)
                {
                    if (GenerationDonjon.instance != null)
                    {
                        Debug.Log("Seed : " + GenerationDonjon.instance.GetSeed());
                        Debug.Log("Unity Engine Random Seed  : " + UnityEngine.Random.state);

                    }
                    else
                    {
                        Debug.Log("Pas de generation de donjon");
                    }

                }
                else
                {
                    Debug.LogWarning("Le joueur a essay� de SEED sans autorisation");
                }
                break;
            case "VISION NOCTURNE":
                if (debugMode)
                {
                    MonPlayerController.instanceLocale.NightVision();
                }
                else
                {
                    Debug.LogWarning("Le joueur a essay� de VISION NOCTURNE sans autorisation");
                }
                break;
            case "SPAWN":
                if (debugMode)
                {
                    MonPlayerController.instanceLocale.TpSpawn();
                }
                else
                {
                    Debug.LogWarning("Le joueur a essay� de SPAWN sans autorisation");
                }
                break;
                #endregion
        }
    }

    /// <summary>
    /// Commence � �couter
    /// </summary>
    public void StartListening()
    {
        if (!recognizer.IsRunning)
            recognizer.Start();
    }

    /// <summary>
    /// Arr�te d'�couter
    /// </summary>
    public void StopListening()
    {
        if (recognizer != null && recognizer.IsRunning)
            recognizer.Stop();
    }

    /// <summary>
    /// Quand on desactive le script, on arr�te d'�couter
    /// </summary>
    private void OnDisable()
    {
        StopListening();
    }

    #region Sorts

    /// <summary>
    /// Cr�e une explosion � l'endroit souhait�
    /// </summary>
    public static void Explosion(Transform target, float radius, float degats)
    {
        AudioManager.instance.PlayOneShotClipServerRpc(target.position, AudioManager.SoundEffectOneShot.EXPLOSION);
        MultiplayerGameManager.Instance.SummonExplosionServerRpc(target.position, radius, 1);

#pragma warning disable UNT0028 // Use non-allocating physics APIs -> C'est un warning pr l'optimisation, mais on s'en fout
        Collider[] hitColliders = Physics.OverlapSphere(target.position, radius);
#pragma warning restore UNT0028 // Use non-allocating physics APIs

        foreach (Collider objetTouche in hitColliders)
        {
            if (objetTouche.CompareTag("Untagged"))
            {
                continue;
            }
            if (objetTouche.CompareTag("Cow"))
            {
                objetTouche.GetComponent<CowController>().UnCow();
                continue;
            }
            //On inflige des d�gats en fonction de la distance
            float distance = Vector3.Distance(target.position, objetTouche.transform.position);
            float degatsInfliges = degats * (1 - distance / radius);
            float forceExplosion = degatsInfliges * 1000;


            if (objetTouche.CompareTag("Player"))
            {
                objetTouche.GetComponent<MonPlayerController>().Damage(degatsInfliges);

                Rigidbody[] ragdollElems = objetTouche.GetComponent<MonPlayerController>().GetRagdollRigidbodies();

                foreach (Rigidbody ragdoll in ragdollElems)
                {
                    ragdoll.AddExplosionForce(forceExplosion, target.position, radius);
                }

            }
            else if (objetTouche.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.AddExplosionForce(forceExplosion, target.position, radius);
            }
        }

    }

    /// <summary>
    /// On cast un ray d'une certaine distance depuis la position de la cam�ra du joueur, si le premier truc qu'on touche possede un script Openable, on l'ouvre
    /// </summary>
    /// <param name="source">Le transform de la cam du joueur</param>
    /// <param name="distanceInteract">La distance possible d'interaction du sort</param>
    public static void OpenSesame(Transform source, float distanceInteract)
    {
#if UNITY_EDITOR
        Debug.DrawRay(source.position, source.forward * distanceInteract, Color.yellow, 1f);
#endif

        if (Physics.Raycast(source.position, source.forward, out RaycastHit hit, distanceInteract))
        {
            if (hit.transform.TryGetComponent(out Openable openable)) //On utilise hit.transform pr chopper le parent qui a un rigidbody
            {
                openable.Open();
            }
        }
    }



    #endregion
}
