using UnityEngine.Windows.Speech;
using UnityEngine;
using System.Text;
using System;

public class SpellRecognition : MonoBehaviour
{
    private KeywordRecognizer recognizer;

    private const float explosionRange = 5;
    private const float explosionForce = 10; //Les degats aussi

    private const float fireBallSpeed = 1;
    private const float fireBallExplosionRange = 5;
    private const float fireBallExplosionForce = 10;
    private const float fireBallTime = 3;

    private const float speedBoostDuration = 10;
    private const float speedBoostSpeed = 5;
    private const float speedBoostTime = 3;

    private const float fusrohdahSpeed = 3;
    private const float fusrohdahExplosionRange = 5;
    private const float fusrohdahExplosionForce = 10;
    private const float fusrohdahTime = 3;

    private const float jumpBonus = 10;

    private const float dashForce = 10;

    private const float healSpeed = 1;
    private const float healDuration = 5;
    private const float healAmount = 10;

    private const float resSpeed = 1;
    private const float resDuration = 5;

    private const float lightIntensity = 1;
    private const float lightTime = 5;

    private const float ragdollTime = 3;

    private const float interactRange = 10;

    void Start()
    {
        recognizer = new KeywordRecognizer(SpellList.spells);
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

        if (args.confidence == ConfidenceLevel.High || args.confidence == ConfidenceLevel.Medium)
            Debug.Log(args.text);
        Vector3 posProj = gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward * 3f + gameObject.GetComponent<MonPlayerController>().playerCamera.transform.position;

        StatsManager.Instance.AddSpellCast();

        switch (args.text)
        {
            case "Crepitus":
                SpellList.Explosion(transform, explosionRange, explosionForce);
                break;
            case "Lux":
                MultiplayerGameManager.Instance.SummonLightballServerRpc(posProj, lightIntensity, lightTime);
                break;
            case "Mortuus":
                gameObject.GetComponent<MonPlayerController>().Die();
                break;
            case "Ragdoll":
                StartCoroutine(gameObject.GetComponent<MonPlayerController>().SetRagdollTemp(ragdollTime));
                break;
            case "Infernum":
                MultiplayerGameManager.Instance.SummonFireBallServerRpc(posProj, gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward, fireBallSpeed, fireBallExplosionRange, fireBallExplosionForce, fireBallTime);
                break;
            case "Sesamae occludit":
                SpellList.OpenSesame(gameObject.GetComponent<MonPlayerController>().playerCamera.transform, interactRange);
                break;
            case "Penitus":
                gameObject.GetComponent<MonPlayerController>().InteractSpell(interactRange);
                break;
            case "FusRoDah":
                MultiplayerGameManager.Instance.SummonFusrohdahServerRpc(gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward * 1f + gameObject.GetComponent<MonPlayerController>().playerCamera.transform.position, gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward, fusrohdahSpeed, fusrohdahTime, fusrohdahExplosionRange, fusrohdahExplosionForce);
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
            case "François François François":
                StartCoroutine(gameObject.GetComponent<MonPlayerController>().SortFrancois());
                break;
            case "Resurrectio":
                MultiplayerGameManager.Instance.SummonResurectioServerRpc(posProj, gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward, resSpeed, resDuration);
                break;
            case "Acceleratio":
                //Fait bouger le joueur plus vite et change sa voix pour qu'elle soit plus aigue
                if (MultiplayerGameManager.Instance.soloMode)
                {
                    gameObject.GetComponent<MonPlayerController>().ReceiveSpeedBoost(speedBoostDuration);
                }
                else
                {
                    MultiplayerGameManager.Instance.SummonAccelProjServerRpc(posProj, gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward, speedBoostSpeed, speedBoostDuration, speedBoostDuration);
                }
                break;
            case "Curae":
                if (MultiplayerGameManager.Instance.soloMode)
                {
                    gameObject.GetComponent<MonPlayerController>().Heal(healAmount);
                }
                else
                {
                    MultiplayerGameManager.Instance.SummonHealProjServerRpc(posProj, gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward, healSpeed, healDuration, healAmount);
                }
                break;
            case "Saltus":
                gameObject.GetComponent<MonPlayerController>().GreaterJump(jumpBonus);
                break;
            case "Polyphorphismus":
                gameObject.GetComponent<MonPlayerController>().Polymorph();
                break;
            case "Offendas":
                Vector3 lookDir = gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward;
                gameObject.GetComponent<MonPlayerController>().Dash(lookDir, dashForce);
                break;
        }
    }

    /// <summary>
    /// Commence à écouter
    /// </summary>
    public void StartListening()
    {
        if (!recognizer.IsRunning)
            recognizer.Start();
    }

    /// <summary>
    /// Arrête d'écouter
    /// </summary>
    public void StopListening()
    {
        if (recognizer != null && recognizer.IsRunning)
            recognizer.Stop();
    }

    /// <summary>
    /// Quand on desactive le script, on arrête d'écouter
    /// </summary>
    private void OnDisable()
    {
        StopListening();
    }
}
