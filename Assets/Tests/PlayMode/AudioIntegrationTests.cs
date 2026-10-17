using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class AudioIntegrationTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly string[] Keys = { "Audio.MusicVolume", "Audio.SoundVolume", "Audio.MusicEnabled", "Audio.SoundEnabled" };
    private readonly Dictionary<string, float> savedPrefs = new Dictionary<string, float>();
    private readonly List<GameObject> objects = new List<GameObject>();
    private readonly List<Scene> scenes = new List<Scene>();
    private readonly List<AudioClip> clips = new List<AudioClip>();
    private Component audio;
    private Behaviour oldAudio;
    private object gameData, oldMusicData;
    private Scene oldScene;
    private bool oldAudioEnabled;

    private static Type GameType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance, null,
            Array.ConvertAll(args, arg => arg.GetType()), null).Invoke(target, args);
    private static object Field(object target, string name) => target.GetType().GetField(name, Hidden).GetValue(target);
    private IDictionary Cache => (IDictionary)Field(audio, "clipCache");
    private AudioSource Music => (AudioSource)Field(audio, "musicSource");
    private AudioSource Sound => (AudioSource)Field(audio, "soundSource");
    private PropertyInfo Instance => GameType("AudioManager").GetProperty("Instance");

    private Component NewComponent(string type)
    {
        var obj = new GameObject("Audio Test " + type);
        objects.Add(obj);
        return obj.AddComponent(GameType(type));
    }

    private Component Panel(string type)
    {
        var obj = Object.Instantiate(Resources.Load<GameObject>("Prefabs/UI/" + type));
        objects.Add(obj);
        return obj.GetComponent(GameType(type));
    }

    private AudioClip CacheClip(string path)
    {
        AudioClip clip = AudioClip.Create(path, 44100 * 2, 1, 44100, false);
        clips.Add(clip);
        Cache[path] = clip;
        return clip;
    }

    [SetUp]
    public void SetUp()
    {
        oldScene = SceneManager.GetActiveScene();
        foreach (string key in Keys)
        {
            if (PlayerPrefs.HasKey(key))
                savedPrefs[key] = key.EndsWith("Enabled") ? PlayerPrefs.GetInt(key) : PlayerPrefs.GetFloat(key);
            PlayerPrefs.DeleteKey(key);
        }
        gameData = GameType("GameDataMgr").GetProperty("Instance").GetValue(null);
        oldMusicData = gameData.GetType().GetField("musicData").GetValue(gameData);
        gameData.GetType().GetField("musicData").SetValue(gameData, Activator.CreateInstance(GameType("MusicData")));
        oldAudio = Instance.GetValue(null) as Behaviour;
        if (oldAudio != null)
        {
            oldAudioEnabled = oldAudio.enabled;
            oldAudio.enabled = false;
        }
        Instance.SetValue(null, null);
        audio = NewComponent("AudioManager");
        for (int i = 1; i <= 6; i++) CacheClip("Music/" + i);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject obj in objects)
            if (obj != null) Object.DestroyImmediate(obj);
        foreach (AudioClip clip in clips) Object.DestroyImmediate(clip);
        SceneManager.SetActiveScene(oldScene);
        Instance.SetValue(null, oldAudio);
        if (oldAudio != null) oldAudio.enabled = oldAudioEnabled;
        gameData.GetType().GetField("musicData").SetValue(gameData, oldMusicData);
        foreach (string key in Keys)
        {
            if (!savedPrefs.TryGetValue(key, out float value)) PlayerPrefs.DeleteKey(key);
            else if (key.EndsWith("Enabled")) PlayerPrefs.SetInt(key, (int)value);
            else PlayerPrefs.SetFloat(key, value);
        }
        PlayerPrefs.Save();
        objects.Clear(); clips.Clear(); savedPrefs.Clear();
    }

    [UnityTearDown]
    public IEnumerator UnloadScenes()
    {
        foreach (Scene scene in scenes)
            if (scene.IsValid()) yield return SceneManager.UnloadSceneAsync(scene);
        scenes.Clear();
    }

    private void ActivateScene(string name)
    {
        Scene scene = SceneManager.GetSceneByName(name);
        if (!scene.IsValid())
        {
            scene = SceneManager.CreateScene(name);
            scenes.Add(scene);
        }
        SceneManager.SetActiveScene(scene);
        Call(audio, "PlaySceneMusic");
    }

    [Test]
    public void SettingSlidersUpdateIndependentSourcesAndReopeningKeepsValues()
    {
        Component panel = Panel("SettingPanel");
        Call(panel, "ShowMe");
        Call(panel, "Init");
        Slider music = (Slider)panel.GetType().GetField("sliderMusic").GetValue(panel);
        Slider sound = (Slider)panel.GetType().GetField("sliderSound").GetValue(panel);
        Assert.That(music.value, Is.EqualTo(0.3f).Within(0.001f));
        music.value = 0.8f;
        sound.value = 0f;
        Assert.That(Music.volume, Is.EqualTo(0.8f).Within(0.001f));
        Assert.That(Sound.volume, Is.Zero);
        Call(audio, "SaveSettings");
        Object.DestroyImmediate(panel.gameObject);
        panel = Panel("SettingPanel");
        Call(panel, "ShowMe");
        Assert.That(((Slider)panel.GetType().GetField("sliderMusic").GetValue(panel)).value, Is.EqualTo(0.8f).Within(0.001f));
        Assert.That(((Slider)panel.GetType().GetField("sliderSound").GetValue(panel)).value, Is.Zero);
    }

    [Test]
    public void VolumesClampAndSavedVolumesAndMutesSurviveManagerRecreation()
    {
        Call(audio, "SetMusicVolume", 5f);
        Call(audio, "SetSoundVolume", -2f);
        Assert.That(Music.volume, Is.EqualTo(1f));
        Assert.That(Sound.volume, Is.Zero);
        Call(audio, "SetMusicVolume", 0.6f);
        Call(audio, "SetSoundVolume", 0.2f);
        Call(audio, "SetMusicEnabled", false);
        Call(audio, "SetSoundEnabled", false);
        Call(audio, "SaveSettings");
        Object.DestroyImmediate(audio.gameObject);
        gameData.GetType().GetField("musicData").SetValue(gameData, null);
        audio = NewComponent("AudioManager");
        Assert.That(Music.volume, Is.EqualTo(0.6f).Within(0.001f));
        Assert.That(Sound.volume, Is.EqualTo(0.2f).Within(0.001f));
        Assert.That(Music.mute, Is.True);
        Assert.That(Sound.mute, Is.True);
    }

    [Test]
    public void SceneAndCharacterPanelSwitchMusicAndOldPanelCannotOverrideNewScene()
    {
        ActivateScene("BeginScene");
        Assert.That(Music.clip, Is.SameAs(Cache["Music/1"]));
        Component panel = Panel("CharacterPanel");
        Call(panel, "ShowMe");
        Assert.That(Music.clip, Is.SameAs(Cache["Music/5"]));
        panel.GetType().GetMethod("HideMe").Invoke(panel, new object[] { null });
        Assert.That(Music.clip, Is.SameAs(Cache["Music/1"]));
        Call(panel, "ShowMe");
        ActivateScene("LevelAudioTest_" + Guid.NewGuid());
        Assert.That(Music.clip, Is.SameAs(Cache["Music/2"]));
        Object.DestroyImmediate(panel.gameObject);
        Assert.That(Music.clip, Is.SameAs(Cache["Music/2"]));
        Assert.That(Music.loop, Is.True);
        Assert.That(audio.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
    }

    [Test]
    public void RepeatedSceneMusicRequestDoesNotRestartTrack()
    {
        ActivateScene("BeginScene");
        Music.timeSamples = 22050;
        Call(audio, "PlaySceneMusic");
        Assert.That(Music.timeSamples, Is.GreaterThanOrEqualTo(22050));
    }

    [Test]
    public void EnemyHitAndDeathUseSeparateEffectsAndDeadEnemyDoesNotReplay()
    {
        Component enemy = NewComponent("Enemy");
        Call(enemy, "TakeDamage", 1);
        Assert.That(Sound.isPlaying, Is.True);
        Sound.Stop();
        Call(enemy, "EliminateWithoutExperience");
        Assert.That(Sound.isPlaying, Is.True);
        Sound.Stop();
        Call(enemy, "EliminateWithoutExperience");
        Call(enemy, "TakeDamage", 999);
        Assert.That(Sound.isPlaying, Is.False);
    }

    [Test]
    public void DashEffectOnlyPlaysOnSuccessfulDash()
    {
        Component controller = NewComponent("PlayerController");
        MethodInfo step = controller.GetType().GetMethod("FixedUpdate", Hidden);
        FieldInfo request = controller.GetType().GetField("dashRequested", Hidden);
        request.SetValue(controller, true);
        step.Invoke(controller, null);
        Assert.That(Sound.isPlaying, Is.True);
        Sound.Stop();
        controller.GetType().GetField("dashTime", Hidden).SetValue(controller, 0f);
        controller.GetType().GetField("dashCooldownTimer", Hidden).SetValue(controller, 1f);
        request.SetValue(controller, true);
        step.Invoke(controller, null);
        Assert.That(Sound.isPlaying, Is.False);
    }

    [Test]
    public void MissingClipWarnsOnlyOnceAndDoesNotThrowOnRepeatedHits()
    {
        const string path = "Music/__missing_audio_test__";
        LogAssert.Expect(LogType.Warning, "Audio clip not found in Resources: " + path);
        Call(audio, "PlaySound", path);
        Call(audio, "PlaySound", path);
        Call(audio, "PlaySound", path);
        Assert.That(Cache.Contains(path), Is.True);
    }
}
