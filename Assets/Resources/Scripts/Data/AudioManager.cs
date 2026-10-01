using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource soundSource;
    [SerializeField] private AudioClip backgroundMusic;

    private MusicData musicData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        if (soundSource == null || soundSource == musicSource)
            soundSource = gameObject.AddComponent<AudioSource>();

        musicData = GameDataMgr.Instance.musicData;
        if (musicData == null)
        {
            musicData = new MusicData();
            GameDataMgr.Instance.musicData = musicData;
        }

        ApplySettings();

        AudioClip clipToPlay = backgroundMusic != null
            ? backgroundMusic
            : musicSource.clip;

        if (clipToPlay != null)
            PlayMusic(clipToPlay);
    }

    public void SetMusicEnabled(bool isEnabled)
    {
        musicData.musicOpen = isEnabled;
        musicSource.mute = !isEnabled;
    }

    public void SetMusicVolume(float volume)
    {
        musicData.musicValue = Mathf.Clamp01(volume);
        musicSource.volume = musicData.musicValue;
    }

    public void SetSoundEnabled(bool isEnabled)
    {
        musicData.soundOpen = isEnabled;
        soundSource.mute = !isEnabled;
    }

    public void SetSoundVolume(float volume)
    {
        musicData.soundValue = Mathf.Clamp01(volume);
        soundSource.volume = musicData.soundValue;
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null)
            return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlaySound(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
            return;

        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip == null)
        {
            Debug.LogWarning("Audio clip not found in Resources: " + resourcePath);
            return;
        }

        PlaySound(clip);
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
            soundSource.PlayOneShot(clip);
    }

    private void ApplySettings()
    {
        musicSource.mute = !musicData.musicOpen;
        musicSource.volume = Mathf.Clamp01(musicData.musicValue);

        soundSource.mute = !musicData.soundOpen;
        soundSource.volume = Mathf.Clamp01(musicData.soundValue);
    }
}