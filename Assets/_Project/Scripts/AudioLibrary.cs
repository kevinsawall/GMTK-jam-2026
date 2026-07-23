using System;
using System.Collections.Generic;
using UnityEngine;

public enum MusicId
{
    None,
    MainMenu,
    Gameplay,
    EndScreen
}

public enum SfxId
{
    None,
    ButtonClick,
    ButtonHover,
    Pause,
    Resume
}

[Serializable]
public sealed class MusicEntry
{
    public MusicId id;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = true;
}

[Serializable]
public sealed class SfxEntry
{
    public SfxId id;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
}

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "Basic UI Template/Audio Library")]
public sealed class AudioLibrary : ScriptableObject
{
    [SerializeField] private List<MusicEntry> music = new List<MusicEntry>();
    [SerializeField] private List<SfxEntry> sfx = new List<SfxEntry>();

    public bool TryGetMusic(MusicId id, out MusicEntry entry)
    {
        entry = music.Find(item => item.id == id && item.clip != null);
        return entry != null;
    }

    public bool TryGetSfx(SfxId id, out SfxEntry entry)
    {
        entry = sfx.Find(item => item.id == id && item.clip != null);
        return entry != null;
    }
}
