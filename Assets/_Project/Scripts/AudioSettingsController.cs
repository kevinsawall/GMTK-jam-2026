using UnityEngine;
using UnityEngine.UI;

public sealed class AudioSettingsController : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    private void OnEnable()
    {
        Bind();
    }

    private void Start()
    {
        Bind();
    }

    private void Bind()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
        musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        sfxVolumeSlider.onValueChanged.RemoveListener(SetSfxVolume);

        masterVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
        musicVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
        sfxVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);

        masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
    }

    private void SetMasterVolume(float value)
    {
        AudioManager.Instance.SetMasterVolume(value);
    }

    private void SetMusicVolume(float value)
    {
        AudioManager.Instance.SetMusicVolume(value);
    }

    private void SetSfxVolume(float value)
    {
        AudioManager.Instance.SetSfxVolume(value);
    }
}
