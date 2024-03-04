using UnityEngine.Windows.Speech;
using UnityEngine;
using System.Text;
using System;

public class SpellRecognition : MonoBehaviour
{
    private KeywordRecognizer recognizer;

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
            case "Explosion":
                SpellList.Explosion(transform, 5, 10);
                break;
            case "Lumos":
            case "Lumosse":
                //On prend le vecteur qui est la direction de la cam�ra du joueur *2f et on prend la nouvelle position de ce vecteur
                Vector3 posLight = gameObject.GetComponent<MonPlayerController>().playerCamera.transform.forward * 2f + gameObject.GetComponent<MonPlayerController>().playerCamera.transform.position;
                SpellList.Lumos(posLight, 1, 5);
                break;
            case "Mort":
                gameObject.GetComponent<MonPlayerController>().Damage(1000);
                break;
            case "Ragdoll":
                StartCoroutine(gameObject.GetComponent<MonPlayerController>().SetRagdollTemp(3));
                break;
            case "Boule de feu":
            case "Fireball":
                //SpellList.Fireball();
                break;
            case "Open sesame":
            case "Ouvre toi sesame":
            case "Sesame ouvre toi":
                SpellList.OpenSesame(gameObject.GetComponent<MonPlayerController>().playerCamera.transform, 10);
                break;
            case "Interact":
            case "Interaction":
                gameObject.GetComponent<MonPlayerController>().InteractSpell(10);
                break;
            case "FusRoDah":
                //SpellList.FusRoDah();
                break;
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
        if (recognizer.IsRunning)
            recognizer.Stop();
    }

    /// <summary>
    /// Quand on desactive le script, on arr�te d'�couter
    /// </summary>
    private void OnDisable()
    {
        StopListening();
    }
}
