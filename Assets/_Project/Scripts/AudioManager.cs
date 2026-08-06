using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioManager : MonoBehaviour
{
    private const string MasterVolumeParameter = "MasterVolume";
    private const string MusicVolumeParameter = "MusicVolume";
    private const string SfxVolumeParameter = "SfxVolume";
    private const string MasterVolumePreferenceKey = "Audio.MasterVolume";
    private const string MusicVolumePreferenceKey = "Audio.MusicVolume";
    private const string SfxVolumePreferenceKey = "Audio.SfxVolume";
    private const float DefaultMasterVolume = 0.8f;
    private const float DefaultMusicVolume = 0.7f;
    private const float DefaultSfxVolume = 0.4f;

    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioLibrary audioLibrary;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField, Range(0f, 1f)] private float masterVolume = DefaultMasterVolume;
    [SerializeField, Range(0f, 1f)] private float musicVolume = DefaultMusicVolume;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = DefaultSfxVolume;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioSource loopingSfxSource;
    private float musicClipVolume = 1f;
    private float loopingSfxClipVolume = 1f;
    private float musicDuckMultiplier = 1f;
    private Coroutine musicDuckRoutine;
    private Coroutine loopingSfxResumeRoutine;

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
        loopingSfxSource = CreateSource("LoopingSfxSource", sfxMixerGroup);
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumePreferenceKey, DefaultMasterVolume));
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumePreferenceKey, DefaultMusicVolume));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumePreferenceKey, DefaultSfxVolume));
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
        musicClipVolume = entry.volume;
        ApplyMusicVolume();
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
        PlaySfx(id, 1f);
    }

    public void PlaySfx(SfxId id, float volumeMultiplier)
    {
        if (!audioLibrary.TryGetSfx(id, out SfxEntry entry))
        {
            Debug.LogWarning($"No SFX clip is configured for {id}.", this);
            return;
        }

        float volume = entry.volume * sfxVolume * Mathf.Max(0f, volumeMultiplier);
        sfxSource.PlayOneShot(entry.clip, volume);
    }

    /// <summary>Plays one of two variations with a small volume change to avoid repetition.</summary>
    public void PlayRandomSfx(
        SfxId firstVariation,
        SfxId secondVariation,
        float minimumVolumeMultiplier = 0.88f,
        float maximumVolumeMultiplier = 1.12f)
    {
        SfxId selectedVariation = Random.value < 0.5f ? firstVariation : secondVariation;
        float volumeMultiplier = Random.Range(
            Mathf.Min(minimumVolumeMultiplier, maximumVolumeMultiplier),
            Mathf.Max(minimumVolumeMultiplier, maximumVolumeMultiplier));
        PlaySfx(selectedVariation, volumeMultiplier);
    }

    public void PlayLoopingSfx(SfxId id)
    {
        if (!audioLibrary.TryGetSfx(id, out SfxEntry entry))
        {
            Debug.LogWarning($"No SFX clip is configured for {id}.", this);
            return;
        }

        if (loopingSfxSource.clip == entry.clip && loopingSfxSource.isPlaying) return;

        loopingSfxSource.clip = entry.clip;
        loopingSfxClipVolume = entry.volume;
        loopingSfxSource.loop = true;
        ApplyLoopingSfxVolume();
        loopingSfxSource.Play();
    }

    public void PlaySfxWithMusicDuck(SfxId id, float musicDuckMultiplier)
    {
        if (!audioLibrary.TryGetSfx(id, out SfxEntry entry))
        {
            Debug.LogWarning($"No SFX clip is configured for {id}.", this);
            return;
        }

        PlaySfx(id);

        if (musicDuckRoutine != null) StopCoroutine(musicDuckRoutine);
        musicDuckRoutine = StartCoroutine(DuckMusicForClip(entry.clip.length, musicDuckMultiplier));
    }

    public void PauseLoopingSfxForOneShot(SfxId loopingSfxId, SfxId oneShotSfxId)
    {
        if (!audioLibrary.TryGetSfx(oneShotSfxId, out SfxEntry entry))
        {
            Debug.LogWarning($"No SFX clip is configured for {oneShotSfxId}.", this);
            return;
        }

        StopLoopingSfx(loopingSfxId);
        PlaySfx(oneShotSfxId);

        if (loopingSfxResumeRoutine != null) StopCoroutine(loopingSfxResumeRoutine);
        loopingSfxResumeRoutine = StartCoroutine(ResumeLoopingSfxAfterClip(loopingSfxId, entry.clip.length));
    }

    public void StopLoopingSfx(SfxId id)
    {
        if (!audioLibrary.TryGetSfx(id, out SfxEntry entry) || loopingSfxSource.clip != entry.clip) return;

        loopingSfxSource.Stop();
        loopingSfxSource.clip = null;
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        SetMixerVolume(MusicVolumeParameter, musicVolume);
        ApplyMusicVolume();
        PlayerPrefs.SetFloat(MusicVolumePreferenceKey, musicVolume);
        PlayerPrefs.Save();
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        // A global master volume must include music and any future AudioSources, not
        // only sources routed through a particular mixer group.
        AudioListener.volume = masterVolume;
        ApplyMusicVolume();
        ApplyLoopingSfxVolume();
        PlayerPrefs.SetFloat(MasterVolumePreferenceKey, masterVolume);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        SetMixerVolume(SfxVolumeParameter, sfxVolume);
        ApplyLoopingSfxVolume();
        PlayerPrefs.SetFloat(SfxVolumePreferenceKey, sfxVolume);
        PlayerPrefs.Save();
    }

    private AudioSource CreateSource(string sourceName, AudioMixerGroup mixerGroup)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = mixerGroup;
        return source;
    }

    private void ApplyMusicVolume()
    {
        if (musicSource == null) return;

        musicSource.volume = musicClipVolume * musicVolume * musicDuckMultiplier;
    }

    private void ApplyLoopingSfxVolume()
    {
        if (loopingSfxSource == null) return;

        loopingSfxSource.volume = loopingSfxClipVolume * sfxVolume;
    }

    private IEnumerator DuckMusicForClip(float duration, float duckMultiplier)
    {
        musicDuckMultiplier = Mathf.Clamp01(duckMultiplier);
        ApplyMusicVolume();

        yield return new WaitForSecondsRealtime(duration);

        musicDuckMultiplier = 1f;
        ApplyMusicVolume();
        musicDuckRoutine = null;
    }

    private IEnumerator ResumeLoopingSfxAfterClip(SfxId id, float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        PlayLoopingSfx(id);
        loopingSfxResumeRoutine = null;
    }

    private bool SetMixerVolume(string parameterName, float value)
    {
        if (audioMixer == null)
        {
            return false;
        }

        float decibels = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
        return audioMixer.SetFloat(parameterName, decibels);
    }
}
