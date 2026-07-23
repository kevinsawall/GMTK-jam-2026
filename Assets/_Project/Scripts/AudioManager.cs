using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioManager : MonoBehaviour
{
    private const string MasterVolumeParameter = "MasterVolume";
    private const string MusicVolumeParameter = "MusicVolume";
    private const string SfxVolumeParameter = "SfxVolume";

    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioLibrary audioLibrary;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = CreateSource("MusicSource", musicMixerGroup);
        sfxSource = CreateSource("SfxSource", sfxMixerGroup);
        SetMasterVolume(masterVolume);
        SetMusicVolume(musicVolume);
        SetSfxVolume(sfxVolume);
    }

    public void PlayMusic(MusicId id)
    {
        if (!audioLibrary.TryGetMusic(id, out MusicEntry entry))
        {
            Debug.LogWarning($"No music clip is configured for {id}.", this);
            return;
        }

        if (musicSource.clip == entry.clip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = entry.clip;
        musicSource.volume = entry.volume;
        musicSource.loop = entry.loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
        musicSource.clip = null;
    }

    public void PlaySfx(SfxId id)
    {
        if (!audioLibrary.TryGetSfx(id, out SfxEntry entry))
        {
            Debug.LogWarning($"No SFX clip is configured for {id}.", this);
            return;
        }

        sfxSource.PlayOneShot(entry.clip, entry.volume);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        SetMixerVolume(MusicVolumeParameter, musicVolume);
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        SetMixerVolume(MasterVolumeParameter, masterVolume);
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        SetMixerVolume(SfxVolumeParameter, sfxVolume);
    }

    private AudioSource CreateSource(string sourceName, AudioMixerGroup mixerGroup)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.outputAudioMixerGroup = mixerGroup;
        return source;
    }

    private void SetMixerVolume(string parameterName, float value)
    {
        if (audioMixer == null)
        {
            return;
        }

        float decibels = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
        audioMixer.SetFloat(parameterName, decibels);
    }
}
