using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : BasePanel //设置面板，提供音量调节、游戏说明
{
    public Button btnClose;
    public Button btnExit;
    public Slider sliderMusic;
    public Slider sliderSound;
    public override void Init()
    {
        AudioManager.EnsureInstance();
        SyncVolumes();
        if (sliderMusic != null)
            sliderMusic.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sliderSound != null)
            sliderSound.onValueChanged.AddListener(OnSoundVolumeChanged);

        btnClose.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<SettingPanel>();
        });
        btnExit.onClick.AddListener(() =>
        {
            AudioManager.Instance?.SaveSettings();
            Application.Quit();
        });
    }

    public override void ShowMe()
    {
        base.ShowMe();
        AudioManager.EnsureInstance();
        SyncVolumes();
    }

    private void SyncVolumes()
    {
        if (sliderMusic != null)
            sliderMusic.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
        if (sliderSound != null)
            sliderSound.SetValueWithoutNotify(AudioManager.Instance.SoundVolume);
    }

    private void OnMusicVolumeChanged(float volume) => AudioManager.Instance?.SetMusicVolume(volume);
    private void OnSoundVolumeChanged(float volume) => AudioManager.Instance?.SetSoundVolume(volume);

    public override void HideMe(UnityEngine.Events.UnityAction callback)
    {
        AudioManager.Instance?.SaveSettings();
        base.HideMe(callback);
    }

    private void OnDisable()
    {
        AudioManager.Instance?.SaveSettings();
    }

    private void OnDestroy()
    {
        if (sliderMusic != null)
            sliderMusic.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        if (sliderSound != null)
            sliderSound.onValueChanged.RemoveListener(OnSoundVolumeChanged);
    }
}
