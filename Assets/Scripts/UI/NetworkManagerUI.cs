using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private GameObject networkUI;
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private GameObject uiCamera;
    [SerializeField] private GameObject gameUI;

    private void Awake()
    {
        hostBtn.onClick.AddListener(OnHostBtnClick);
        clientBtn.onClick.AddListener(OnClientBtnClick);
    }

    private void OnHostBtnClick()
    {
        uiCamera.SetActive(false);
        networkUI.SetActive(false);
        gameUI.SetActive(true);
        NetworkManager.Singleton.StartHost();
        
    }

    private void OnClientBtnClick()
    {
        uiCamera.SetActive(false);
        networkUI.SetActive(false);
        gameUI.SetActive(true);
        NetworkManager.Singleton.StartClient();
        
    }
}
