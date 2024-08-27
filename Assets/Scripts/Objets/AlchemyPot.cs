using UnityEngine;

/// <summary>
/// Classe qui répresente le puits qui fait spawn son alchemy zone qui permet de convertir l'or
/// </summary>
public class AlchemyPot : MonoBehaviour
{
    private GameObject alchemyZone;

    private static int nbAlchemyPot = 0;

    private void Awake()
    {
        name = "AlchemyPot" + nbAlchemyPot;
        nbAlchemyPot++;
        if (MultiplayerGameManager.Instance.IsServer)
        {
            MultiplayerGameManager.Instance.SummonAlchemyZoneServerRpc(name, transform.position + new Vector3(0, 1.2f, 0f));
        }
    }

    public void SetAlchemyZone(GameObject alcZone)
    {
        alchemyZone = alcZone;
    }

    private void OnDestroy()
    {
        if (alchemyZone != null)
        {
            if (MultiplayerGameManager.Instance.IsServer)
            {
                MultiplayerGameManager.Instance.DespawnObjServerRpc(alchemyZone);
            }

        }
    }
}
