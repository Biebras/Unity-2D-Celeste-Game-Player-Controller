using UnityEngine.Audio;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;
    public SoundType soundType;
    public AudioClip clip;
    public AudioMixerGroup output;
    [Range(0, 1)]
    public float volume = 1;
    [Range(.1f, 3)]
    public float pitch = 1;
    public bool playOnAwake = false;
    public bool bypassEffects;
    public bool bypassListenerEffects;
    public bool loop = false;

    [HideInInspector] public AudioSource source;
}

public enum SoundType
{
    NotAssigned,
    Music,
    SFX
}

