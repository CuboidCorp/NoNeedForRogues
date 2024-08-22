using UnityEngine;

public class ToxicGaz : Network
{
    public float expansionSpeed = .5f;
    public float damage = 1f;

    private Vector3 maxSize = new(100, 100, 100);
    private List<MonPlayerController> listPlayersInside;


    private void Awake()
    {
        listPlayersInside = [];
        StartCoroutine(Expansion());
    }

    private IEnumerator Expansion()
    {
        while(true)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, maxSize, expansionSpeed * Time.deltaTime);
            yield return null;
        }
    }

    //TODO : Rajouter coroutine pr damage ts les players inside


    private void OnTriggerEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            listPlayersInside.Add(other.GetComponent<MonPlayerController>());
        }
    }


    private void OnTriggerExit(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            listPlayersInside.Remove(other.GetComponent<MonPlayerController>());
        }
    }
}
