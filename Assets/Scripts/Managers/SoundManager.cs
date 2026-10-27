using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    #region Instance Setup
    public static SoundManager Instance { get; private set; }
    #endregion

    #region Modifiable Variables
    [Header("Sound Effects")]
    public AudioClip PlayerMove;
    public AudioClip BoxRustle;
    public AudioClip ButtonClick;
    public AudioClip Whoosh;
    public AudioClip PanelOpenAndClose;

    [Header("Background Sound")]
    public AudioClip Menu;
    public AudioClip Game;
    public AudioClip Ending;

    [Header("Audio Manipulations")]
    // for player move sfx
    [Range(0f, 1f)]
    public float PlayerMoveVolume = 0.6f;
    [Range(0f, 3f)]
    public float PlayerMovePitchLowerBound = 0.8f;
    [Range(0f, 3f)]
    public float PlayerMovePitchUpperBound = 1.2f;
    // for box rustle sfx
    [Range(0f, 1f)]
    public float BoxRustleVolume = 0.6f;
    [Range(0f, 3f)]
    public float BoxRustlePitchLowerBound = 0.8f;
    [Range(0f, 3f)]
    public float BoxRustlePitchUpperBound = 1.2f;
    // for button click sfx
    [Range(0f, 1f)]
    public float ButtonClickVolume = 0.6f;
    [Range(0f, 3f)]
    public float ButtonClickPitchLowerBound = 0.8f;
    [Range(0f, 3f)]
    public float ButtonClickPitchUpperBound = 1.2f;
    // for whoosh sfx
    [Range(0f, 1f)]
    public float WhooshVolume = 0.6f;
    [Range(0f, 3f)]
    public float WhooshPitchLowerBound = 0.8f;
    [Range(0f, 3f)]
    public float WhooshPitchUpperBound = 1.2f;
    // for panel open and close sfx
    [Range(0f, 1f)]
    public float PanelOpenAndCloseVolume = 0.6f;
    [Range(0f, 3f)]
    public float PanelOpenAndClosePitchLowerBound = 0.8f;
    [Range(0f, 3f)]
    public float PanelOpenAndClosePitchUpperBound = 1.2f;

    // for music
    [Range(0f, 1f)]
    public float MenuVolume = 0.6f;
    [Range(0f, 1f)]
    public float GameVolume = 0.6f;
    [Range(0f, 1f)]
    public float EndingVolume = 0.6f;

    [Header("Sound GameObject")]
    public GameObject SFXObj;
    #endregion

    #region Misc
    public bool MusicIsPlaying
    {
        get
        {
            return
                menuMusicInstance != null ||
                gameMusicInstance != null ||
                endingMusicInstance != null;
        }
        private set
        {
            ;
        }
    }

    // store the music sources in instance references so we can pause them or do whatever with them
    private GameObject menuMusicInstance, gameMusicInstance, endingMusicInstance;

    // the whoosh sound effect is a bit special as in it needs to be played exactly once, but it is called in an update loop
    // so we have to kind of create a bool specially for it
    public bool WhooshPlayed
    {
        get { return whooshPlayed; }
        private set {; }
    }
    private bool whooshPlayed;
    #endregion

    private void Awake()
    {
        Init();
    }

    // same singleton behavior as game manager
    private void Init()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        Setup();
    }

    private void Setup()
    {
        // reset some variables when a new scene loads
        SceneManager.sceneLoaded += delegate 
                                            {
                                                whooshPlayed = false;
                                            };
    }

    public void PlaySFX(AudioClip clip, float volume, float pitch, bool destroySelf)
    {
        AudioSource audioSrc = Instantiate(SFXObj)?.GetComponent<AudioSource>();

        if (audioSrc == null || clip == null)
        {
            Debug.Log("Can't access audiosource or valid audio clip. Can't play sound effect");
            return;
        }

        audioSrc.clip = clip;
        audioSrc.volume = volume;
        audioSrc.pitch = pitch;
        audioSrc.Play();

        if (destroySelf)
            Destroy(audioSrc.gameObject, clip.length);
    }

    public GameObject PlayMusic(AudioClip clip, float volume, float pitch, bool destroySelf)
    {
        AudioSource audioSrc = Instantiate(SFXObj).GetComponent<AudioSource>();

        if (audioSrc == null || clip == null)
        {
            Debug.Log("Can't access audiosource or valid audio clip. Can't play music");
            return null;
        }

        audioSrc.clip = clip;
        audioSrc.volume = volume;
        audioSrc.pitch = pitch;
        audioSrc.loop = true;
        audioSrc.Play();

        if (destroySelf)
            Destroy(audioSrc.gameObject, clip.length);

        return audioSrc.gameObject;
    }

    // SFX-based methods //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    public void PlayPlayerMoveSFX()
    {
        float pitch = Random.Range(PlayerMovePitchLowerBound, PlayerMovePitchUpperBound);
        PlaySFX(PlayerMove, PlayerMoveVolume, pitch, true);
    }

    public void PlayBoxRustleSFX()
    {
        float pitch = Random.Range(BoxRustlePitchLowerBound, BoxRustlePitchUpperBound);
        PlaySFX(BoxRustle, BoxRustleVolume, pitch, true);
    }

    public void PlayButtonClickSFX()
    {
        float pitch = Random.Range(ButtonClickPitchLowerBound, ButtonClickPitchUpperBound);
        PlaySFX(ButtonClick, ButtonClickVolume, pitch, true);
    }

    public void PlayWhooshSFX()
    {
        float pitch = Random.Range(WhooshPitchLowerBound, WhooshPitchUpperBound);
        PlaySFX(Whoosh, ButtonClickVolume, pitch, true);

        whooshPlayed = true;
    }

    public void PlayPanelOpenAndCloseSFX()
    {
        float pitch = Random.Range(PanelOpenAndClosePitchLowerBound, PanelOpenAndClosePitchUpperBound);
        PlaySFX(PanelOpenAndClose, PanelOpenAndCloseVolume, pitch, true);
    }


    // Music-based methods //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void PlayMenuMusic()
    {
        menuMusicInstance = PlayMusic(Menu, MenuVolume, 1f, false);
    }

    public void PlayGameMusic()
    {
        gameMusicInstance = PlayMusic(Game, GameVolume, 1f, false);
    }

    // I'm only planning for the player to only be able to pause and unpause the game and ending music
    public void PauseGameMusic()
    {
        if (!MusicIsPlaying || gameMusicInstance == null)
        {
            Debug.Log("Cannot pause music. Instance doesn't exist or no music is playing");
            return;
        }

        AudioSource audioSrc = gameMusicInstance.GetComponent<AudioSource>();
        audioSrc.Pause();
    }

    public void UnpauseGameMusic()
    {
        if (!MusicIsPlaying || gameMusicInstance == null)
        {
            Debug.Log("Cannot unpause music. Instance doesn't exist or no music is playing");
            return;
        }

        AudioSource audioSrc = gameMusicInstance.GetComponent<AudioSource>();
        audioSrc.UnPause();
    }

    public void PlayEndingMusic()
    {
        endingMusicInstance = PlayMusic(Ending, EndingVolume, 1f, false);
    }

    public void PauseEndingMusic()
    {
        if (!MusicIsPlaying || endingMusicInstance == null)
        {
            Debug.Log("Cannot unpause music. Instance doesn't exist or no music is playing");
            return;
        }

        AudioSource audioSrc = endingMusicInstance.GetComponent<AudioSource>();
        audioSrc.Pause();
    }

    public void UnpauseEndingMusic()
    {
        if (!MusicIsPlaying || endingMusicInstance == null)
        {
            Debug.Log("Cannot unpause music. Instance doesn't exist or no music is playing");
            return;
        }

        AudioSource audioSrc = endingMusicInstance.GetComponent<AudioSource>();
        audioSrc.UnPause();
    }
}
