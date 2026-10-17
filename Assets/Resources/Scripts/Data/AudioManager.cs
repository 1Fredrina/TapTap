using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public const string MenuMusicPath = "Music/1";
    public const string GameMusicPath = "Music/2";
    public const string EnemyDeathSoundPath = "Music/3";
    public const string DashSoundPath = "Music/4";
    public const string CharacterMusicPath = "Music/5";
    public const string HitSoundPath = "Music/6";

    private const string MusicVolumeKey = "Audio.MusicVolume";
    private const string SoundVolumeKey = "Audio.SoundVolume";
    private const string MusicEnabledKey = "Audio.MusicEnabled";
    private const string SoundEnabledKey = "Audio.SoundEnabled";

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource soundSource;
    [SerializeField] private AudioClip backgroundMusic;

    private MusicData musicData;
    private readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
    private Object characterMusicOwner;

    public float MusicVolume => musicData.musicValue;
    public float SoundVolume => musicData.soundValue;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance().PlaySceneMusic();
    }

    public static AudioManager EnsureInstance()
    {
        if (Instance == null)
        {
            AudioManager existing = FindFirstObjectByType<AudioManager>();
            if (existing != null)
                return existing;
            new GameObject(nameof(AudioManager)).AddComponent<AudioManager>();
        }
        return Instance;
    }

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

        musicSource.playOnAwake = false;
        soundSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        soundSource.spatialBlend = 0f;

        musicData = GameDataMgr.Instance.musicData;
        if (musicData == null)
        {
            musicData = new MusicData();
            GameDataMgr.Instance.musicData = musicData;
        }

        musicData.musicValue = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, musicData.musicValue));
        musicData.soundValue = Mathf.Clamp01(PlayerPrefs.GetFloat(SoundVolumeKey, musicData.soundValue));
        musicData.musicOpen = PlayerPrefs.GetInt(MusicEnabledKey, musicData.musicOpen ? 1 : 0) != 0;
        musicData.soundOpen = PlayerPrefs.GetInt(SoundEnabledKey, musicData.soundOpen ? 1 : 0) != 0;
        ApplySettings();
    }

    private void OnEnable()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene == SceneManager.GetActiveScene())
            PlaySceneMusic();
    }

    private void OnActiveSceneChanged(Scene previous, Scene current)
    {
        // 场景切换结束角色选择的临时音乐，旧面板淡出时不会覆盖新场景音乐。
        characterMusicOwner = null;
        PlaySceneMusic();
    }

    public void PlaySceneMusic()
    {
        if (characterMusicOwner != null)
        {
            PlayResourceMusic(CharacterMusicPath);
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "BeginScene")
            PlayResourceMusic(MenuMusicPath);
        else if (sceneName.StartsWith("Level", System.StringComparison.Ordinal) || FindFirstObjectByType<GameManager>() != null)
            PlayResourceMusic(GameMusicPath);
        else if (backgroundMusic != null)
            PlayMusic(backgroundMusic);
        else
            StopMusic();
    }

    public void BeginCharacterSelection(Object owner)
    {
        if (owner == null) return;
        characterMusicOwner = owner;
        PlayResourceMusic(CharacterMusicPath);
    }

    public void EndCharacterSelection(Object owner)
    {
        if (!ReferenceEquals(characterMusicOwner, owner)) return;
        characterMusicOwner = null;
        PlaySceneMusic();
    }

    private void PlayResourceMusic(string path)
    {
        AudioClip clip = LoadClip(path);
        if (clip != null)
            PlayMusic(clip);
        else
            StopMusic();
    }

    public void SetMusicEnabled(bool isEnabled)
    {
        musicData.musicOpen = isEnabled;
        musicSource.mute = !isEnabled;
        PlayerPrefs.SetInt(MusicEnabledKey, isEnabled ? 1 : 0);
    }

    public void SetMusicVolume(float volume)
    {
        musicData.musicValue = Mathf.Clamp01(volume);
        musicSource.volume = musicData.musicValue;
        PlayerPrefs.SetFloat(MusicVolumeKey, musicData.musicValue);
    }

    public void SetSoundEnabled(bool isEnabled)
    {
        musicData.soundOpen = isEnabled;
        soundSource.mute = !isEnabled;
        PlayerPrefs.SetInt(SoundEnabledKey, isEnabled ? 1 : 0);
    }

    public void SetSoundVolume(float volume)
    {
        musicData.soundValue = Mathf.Clamp01(volume);
        soundSource.volume = musicData.soundValue;
        PlayerPrefs.SetFloat(SoundVolumeKey, musicData.soundValue);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null)
            return;

        if (musicSource.clip == clip && musicSource.loop == loop && musicSource.isPlaying)
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

        PlaySound(LoadClip(resourcePath));
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
            soundSource.PlayOneShot(clip);
    }

    private AudioClip LoadClip(string resourcePath)
    {
        if (clipCache.TryGetValue(resourcePath, out AudioClip cached))
            return cached;

        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        // 包括缺失结果也缓存，避免敌人密集受击时反复加载或刷屏。
        clipCache[resourcePath] = clip;
        if (clip == null)
            Debug.LogWarning("Audio clip not found in Resources: " + resourcePath);
        return clip;
    }

    public void SaveSettings()
    {
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && Instance == this) SaveSettings();
    }

    private void OnApplicationQuit()
    {
        if (Instance == this) SaveSettings();
    }

    private void ApplySettings()
    {
        musicSource.mute = !musicData.musicOpen;
        musicSource.volume = Mathf.Clamp01(musicData.musicValue);

        soundSource.mute = !musicData.soundOpen;
        soundSource.volume = Mathf.Clamp01(musicData.soundValue);
    }
}
