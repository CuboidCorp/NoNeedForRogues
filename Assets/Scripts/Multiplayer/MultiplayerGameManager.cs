using UnityEngine;

public class MultiplayerGameManager : MonoBehaviour
{
    public bool soloMode = false;

    public static MultiplayerGameManager Instance;

    private void Awake()
    {
        Instance = this;
    }
}
