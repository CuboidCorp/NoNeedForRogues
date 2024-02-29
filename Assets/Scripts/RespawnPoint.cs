using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("PlayerGhost"))
        {
            other.gameObject.GetComponent<GhostController>().Respawn();
        }
    }
}
