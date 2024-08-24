using Unity.Netcode;
using UnityEngine;

public class AudioManager : NetworkBehaviour
{
    public static AudioManager instance;

    [Header("Music")]
    [SerializeField] private AudioClip[] musiques;

    public enum Musique
    {
        TAVERNE,
        MAIN,
        DONJON,
        END,
    }

    #region Sound Effects

    [Header("Sound Effects")]
    [SerializeField] private GameObject soundFxPrefab;

    //LEs sounds effect oneshot --> Genre une seule fois appelé
    [SerializeField] private AudioClip[] oneShotClips;

    public enum SoundEffectOneShot
    {
        SCREAM,
        FAIL_INTERACT,
        MONEY_GAINED,
        RESURRECTION,
        EXPLOSION,
        NUHUH,
        PLAYER_DAMAGED,
        PLAYER_DEAD,
        SPELL_CAST,
        CHEST_OPENED,
        ARROW_TRAP,
        FLOOR_TRAP,
        PP_DOWN,
        PP_UP,
        BEAT_TRAP,
        SPIKE_UP
    }

    /// <summary>
    /// Les sound effect qui font du bruit jusqu'à ce qu'on les arrete
    /// </summary>
    [SerializeField] private AudioClip[] loopingClips; //TODO : Ptet mieux de les gerer dans leur trucs respectifs

    public enum SoundEffectLoop
    {
        CASTING,
        MOVING
    }

    #endregion


    private void Awake() //On detruit pas l'audiomanager car il est sensé être partout
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        instance = this;
        DontDestroyOnLoad(this);
    }

    #region Musique

    public void SetMusic(Musique musicToPlay)
    {
        AudioClip music = musiques[(int)musicToPlay];
        if (music == null)
        {
            Debug.LogWarning("La musique " + musicToPlay + " n'est pas attribuée");
        }
        GetComponent<AudioSource>().clip = music;
    }

    public void ActivateMusic()
    {
        GetComponent<AudioSource>().Play();
    }

    public void StopMusic()
    {
        GetComponent<AudioSource>().Stop();
    }

    #endregion

    #region Sound Effects
    /// <summary>
    /// Appele le serveur pour faire spawn un sound effect temporaire sur tous les clients
    /// </summary>
    /// <param name="position">Position du sound effect</param>
    /// <param name="soundEffect">Le sound effect a jouer</param>
    [ServerRpc(RequireOwnership = false)]
    public void PlayOneShotClipServerRpc(Vector3 position, SoundEffectOneShot soundEffect)
    {
        PlayOneShotClipClientRpc(position, soundEffect);
    }

    /// <summary>
    /// Joue un sound effect sur tous les joueurs au meme endroit
    /// </summary>
    /// <param name="position">Position du sound effect</param>
    /// <param name="soundEffect">Le sound effect en question</param>
    [ClientRpc]
    private void PlayOneShotClipClientRpc(Vector3 position, SoundEffectOneShot soundEffect)
    {
        AudioSource audioSource = Instantiate(soundFxPrefab, position, Quaternion.identity).GetComponent<AudioSource>();
        AudioClip clip = oneShotClips[(int)soundEffect];
        if (clip == null)
        {
            Debug.LogWarning("Le clip " + soundEffect + " n'est pas encore attribué");
        }
        audioSource.clip = clip;
        audioSource.Play();
        Destroy(audioSource.gameObject, clip.length);

    }


    #endregion
}
