using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [SerializeField] private GameObject soundFxPrefab;

    #region AudioClips
    [SerializeField] private AudioClip screamClip;
    [SerializeField] private AudioClip interactFail;
    [SerializeField] private AudioClip moneyGained;
    [SerializeField] private AudioClip ressurection;
    [SerializeField] private AudioClip explosion;
    #endregion
    private void Awake()
    {
        instance = this;
    }

    #region Musique

    public void SetMusic()
    {
        //TODO : Voir quoi mettre pr les musiques
    }

    public void ActivateMusic()
    {
        GetComponent<AudioSource>().Play();
    }

    #endregion


    #region Sound Effects
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

    #endregion
}
