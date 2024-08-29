using UnityEngine;

/// <summary>
/// Classe qui répresente un ventilo qui doit spawn sa zone vent
/// </summary>
public class Ventilo : MonoBehaviour
{
    private GameObject zoneVent;

    private static int nbVentilo = 0;

    [SerializeField] private float forceWindZone = 20;
    [SerializeField] private Vector3 tailleColliderWindZone;
    [SerializeField] private Vector3 posColliderWindZone;


    private void Awake()
    {
        name = "Ventilo" + nbVentilo;
        nbVentilo++;
        if (MultiplayerGameManager.Instance.IsServer)
        {
            MultiplayerGameManager.Instance.SummonVentiloWindZoneServerRpc(name, transform.position, transform.rotation.eulerAngles, forceWindZone, tailleColliderWindZone, posColliderWindZone));
        }
    }

    public void SetWindZone(GameObject windZone)
    {
        zoneVent = windZone;
    }

    private void OnDestroy()
    {
        if (zoneVent != null)
        {
            if (MultiplayerGameManager.Instance.IsServer)
            {
                MultiplayerGameManager.Instance.DespawnObjServerRpc(zoneVent);
            }

        }
    }
}
