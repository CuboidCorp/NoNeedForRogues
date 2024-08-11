using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Music")]
    [SerializeField] private AudioClip tavernMusicClip;



    #region Sound Effects

    [Header("Sound Effects")]
    [SerializeField] private GameObject soundFxPrefab;

    [SerializeField] private AudioClip screamClip;
    [SerializeField] private AudioClip interactFail;
    [SerializeField] private AudioClip moneyGained;
    [SerializeField] private AudioClip ressurection;
    [SerializeField] private AudioClip explosion;
    [SerializeField] private AudioClip nuhUh;
    #endregion
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        instance = this;
        DontDestroyOnLoad(this);
    }

    #region Musique

    public void SetMusicTaverne()
    {
        GetComponent<AudioSource>().clip = tavernMusicClip;
    }

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

    public void StartUnableToInteract(Vector3 position)
    {
        AudioSource audioSource = Instantiate(soundFxPrefab).GetComponent<AudioSource>();

        audioSource.clip = interactFail;

        audioSource.Play();

        Destroy(audioSource.gameObject, interactFail.length);
    }

    public void CowardPlayer(Vector3 position)
    {
        AudioSource audioSource = Instantiate(soundFxPrefab).GetComponent<AudioSource>();

        audioSource.clip = nuhUh;

        audioSource.Play();

        Destroy(audioSource.gameObject, nuhUh.length);
    }

    public void StartMoneyGained(Vector3 position) //TODO : Pas de se pr l'argent encore
    {
        AudioSource audioSource = Instantiate(soundFxPrefab).GetComponent<AudioSource>();

        audioSource.clip = moneyGained;

        audioSource.Play();

        Destroy(audioSource.gameObject, moneyGained.length);
    }

    public void StartRessurection(Vector3 position)
    {
        AudioSource audioSource = Instantiate(soundFxPrefab).GetComponent<AudioSource>();

        audioSource.clip = ressurection;

        audioSource.Play();

        Destroy(audioSource.gameObject, ressurection.length);
    }

    public void StartExplosion(Vector3 position)
    {
        AudioSource audioSource = Instantiate(soundFxPrefab).GetComponent<AudioSource>();

        audioSource.clip = explosion;

        audioSource.Play();

        Destroy(audioSource.gameObject, explosion.length);
    }



    #endregion
}
