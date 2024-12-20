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

    //LEs sounds effect oneshot --> Genre une seule fois appel�
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
        FAIL_TRICKSHOT,
        CHEST_OPENED,
        ARROW_TRAP,
        FLOOR_TRAP,
        PP_DOWN,
        BEAT_TRAP,
        SPIKE_UP
    }

    #endregion


    private void Awake() //On detruit pas l'audiomanager car il est sens� �tre partout
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        instance = this;
        DontDestroyOnLoad(this);
    }

    #region Musique

    /// <summary>
    /// Change la musique � jouer
    /// </summary>
    /// <param name="musicToPlay">La nouvelle musique � jouer</param>
    public void SetMusic(Musique musicToPlay)
    {
        AudioClip music = musiques[(int)musicToPlay];
        if (music == null)
        {
            Debug.LogWarning("La musique " + musicToPlay + " n'est pas attribu�e");
        }
        GetComponent<AudioSource>().clip = music;
    }

    /// <summary>
    /// Active la musique en cours
    /// </summary>
    public void ActivateMusic()
    {
        GetComponent<AudioSource>().Play();
    }

    /// <summary>
    /// Arrete la musique en cours
    /// </summary>
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
    /// <param name="volume">Le volume du sound effect</param>
    [ServerRpc(RequireOwnership = false)]
    public void PlayOneShotClipServerRpc(Vector3 position, SoundEffectOneShot soundEffect, float volume = 1)
    {
        PlayOneShotClipClientRpc(position, soundEffect, volume);
    }

    /// <summary>
    /// Joue un sound effect sur tous les joueurs au meme endroit
    /// </summary>
    /// <param name="position">Position du sound effect</param>
    /// <param name="soundEffect">Le sound effect en question</param>
    /// <param name="volume">Le volume du sound effect</param>
    [ClientRpc]
    private void PlayOneShotClipClientRpc(Vector3 position, SoundEffectOneShot soundEffect, float volume)
    {
        AudioSource audioSource = Instantiate(soundFxPrefab, position, Quaternion.identity).GetComponent<AudioSource>();
        AudioClip clip = oneShotClips[(int)soundEffect];
        if (clip == null)
        {
            Debug.LogWarning("Le clip " + soundEffect + " n'est pas encore attribu�");
        }
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
        Destroy(audioSource.gameObject, clip.length);

    }


    #endregion
}
