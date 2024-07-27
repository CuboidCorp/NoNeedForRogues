using UnityEngine.Windows.Speech;
using UnityEngine;
using System.Text;
using System;

public class SpellRecognition : MonoBehaviour
{
    private KeywordRecognizer recognizer;

    private const float explosionRange = 5;
    private const float explosionForce = 10; //Les degats aussi

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

        switch (args.text)
        {
            case "Crepitus":
                SpellList.Explosion(transform, explosionRange, explosionForce);
                break;
            case "Lux":
                //On prend le vecteur qui est la direction de la caméra du joueur *2f et on prend la nouvelle position de ce vecteur
                Vector3 posLight = gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward * 2f + gameObject.GetComponent<MonPlayerController>().playerCamera.transform.position;
                MultiplayerGameManager.Instance.SummonLightballServerRpc(posLight, lightIntensity, lightTime);
                break;
            case "Mortuus":
                gameObject.GetComponent<MonPlayerController>().Damage(1000);
                break;
            case "Ragdoll":
                StartCoroutine(gameObject.GetComponent<MonPlayerController>().SetRagdollTemp(ragdollTime));
                break;
            case "Ignis Pila":
                //SpellList.Fireball();
                break;
            case "Sesamae occludit":
                SpellList.OpenSesame(gameObject.GetComponent<MonPlayerController>().playerCamera.transform, interactRange);
                break;
            case "Penitus":
                gameObject.GetComponent<MonPlayerController>().InteractSpell(interactRange);
                break;
            case "FusRoDah":
                //SpellList.FusRoDah();
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
                //Envoie un projectile , si il touche un fantome , le fantome est ressuscité
                break;
            case "Acceleratio":
                //Fait bouger le joueur plus vite et change sa voix pour qu'elle soit plus aigue
                break;
            case "Curae":
                //Lance un projectile qui soigne le joueur si multi ou heal direct si solo
                break;
            case "Saltus":
                //Fait sauter le joueur très haut
                break;
            case "Polyphorphismus":
                //Transforme le joueur en vache pendant 1min
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
