using UnityEngine;

/// <summary>
/// Classe qui permet de montrer les erreurs au joueur
/// </summary>
public class ErrorHandler : MonoBehaviour
{
    public string message;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        gameObject.tag = "ErrorHandler";
    }
}
