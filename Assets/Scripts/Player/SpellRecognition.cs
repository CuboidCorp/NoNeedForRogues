using UnityEngine.Windows.Speech;
using UnityEngine;
using System.Text;
using System;

public class SpellRecognition : MonoBehaviour
{
    [SerializeField] private string[] m_Keywords;

    private KeywordRecognizer m_Recognizer;

    void Start()
    {
        m_Recognizer = new KeywordRecognizer(m_Keywords);
        m_Recognizer.OnPhraseRecognized += OnPhraseRecognized;
        m_Recognizer.Start();
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        //C'est bien pr debug
        StringBuilder builder = new();
        builder.AppendFormat("{0} ({1}){2}", args.text, args.confidence, Environment.NewLine);
        builder.AppendFormat("\tTimestamp: {0}{1}", args.phraseStartTime, Environment.NewLine);
        builder.AppendFormat("\tDuration: {0} seconds{1}", args.phraseDuration.TotalSeconds, Environment.NewLine);
        Debug.Log(builder.ToString());

        if (args.confidence == ConfidenceLevel.High || args.confidence == ConfidenceLevel.Medium)
            Debug.Log(args.text);

        switch (args.text)
        {
            case "Explosion":
                SpellList.Explosion(transform, 5, 10);
                break;
        }
    }
}
