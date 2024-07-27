using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [SerializeField] private GameObject soundFxPrefab;

    [SerializeField] private AudioClip screamClip;
    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// Fait le bruit du screamer
    /// </summary>
    public void StartScreamerSound(Vector3 position)
    {
        AudioSource audioSource = Instantiate(soundFxPrefab).GetComponent<AudioSource>();

        audioSource.clip = screamClip;

        audioSource.Play();

        Destroy(audioSource.gameObject, screamClip.length);
    }
}
