using System;
using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public Sound[] Sounds;

    public static AudioManager s_singleton;

    private void Awake()
    {
        if(s_singleton != null)
        {
            Destroy(gameObject);
            return;
        }

        s_singleton = this;

        SetUpSound();
        PlayOnAwake();
    }

    private void SetUpSound()
    {
        foreach (Sound sound in Sounds)
        {
            sound.source = gameObject.AddComponent<AudioSource>();
            if (sound.output != null)
                sound.source.outputAudioMixerGroup = sound.output;
            sound.source.clip = sound.clip;
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            sound.source.playOnAwake = sound.playOnAwake;
            sound.source.bypassEffects = sound.bypassEffects;
            sound.source.bypassListenerEffects = sound.bypassListenerEffects;
            sound.source.loop = sound.loop;
        }
    }

    private void PlayOnAwake()
    {
        foreach (var sound in Sounds)
        {
            if (sound != null && sound.playOnAwake)
            {
                sound.source.Play();
            }
        }
    }

    public void Play(string name)
    {
        Sound sound = GetSound(name);

        sound.source.Play();
    }

    public void CheckThenPlay(string name)
    {
        Sound sound = GetSound(name);

        if(!sound.source.isPlaying)
            sound.source.Play();
    }

    public string CheckThenPlayRandomSound(string[] names)
    {
        var randomIndex = UnityEngine.Random.Range(0, names.Length);

        CheckThenPlay(names[randomIndex]);
        return names[randomIndex];
    }

    public string PlayRandomSound(string[] names)
    {
        var randomIndex = UnityEngine.Random.Range(0, names.Length);

        Play(names[randomIndex]);
        return names[randomIndex];
    }

    public void PlayRandomSound(List<string> names)
    {
        var randomIndex = UnityEngine.Random.Range(0, names.Count);

        Play(names[randomIndex]);
    }

    public void Pause(string name)
    {
        Sound sound = GetSound(name);

        sound.source.Pause();
    }

    public void Stop(string name)
    {
        Sound sound = GetSound(name);

        sound.source.Stop();
    }

    public void SetVolume(string name, float volume)
    {
        Sound sound = GetSound(name);

        sound.source.volume = volume;
    }

    public void SetPitch(string name, float pitch)
    {
        Sound sound = GetSound(name);

        sound.source.pitch = pitch;
    }

    public void PlayRandomPitch(string name, float min, float max)
    {
        var randomPitch = UnityEngine.Random.Range(min, max);

        SetPitch(name, randomPitch);
        Play(name);
    }

    private Sound GetSound(string name)
    {
        Sound sound = Array.Find(Sounds, s => s.name == name);

        if (sound == null)
            throw new Exception("Couldn't find a sound with the name: " + name);

        return sound;
    }
}
