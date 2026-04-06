using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace WaddleAndGrapple.Engine.Managers;

public class AudioManager
{
    public static AudioManager Instance { get; private set; } = new AudioManager();
    
    public string CurrentSongName { get; private set; }

    public static float SongVolume 
    {
        get => MediaPlayer.Volume;
        set => MediaPlayer.Volume = Math.Clamp(value, 0f, 1f);
    }

    public static float SFXVolume
    {
        get => SoundEffect.MasterVolume;
        set => SoundEffect.MasterVolume = Math.Clamp(value, 0f, 1f);
    }
    
    private readonly Dictionary<string, SoundEffect> _soundsEffects = [];
    private readonly Dictionary<string, Song> _songs = [];
    
    private AudioManager()
    {
        SongVolume = 0.7f; // Default volume
        SFXVolume = 0.7f;  // Default volume
    }

    public void LoadSound(string name, SoundEffect sound)
    {
        _soundsEffects[name] = sound;
    }
    
    public void PlaySound(string name, float volume = 1f, float pitch = 0f, float pan = 0f)
    {
        if (!_soundsEffects.TryGetValue(name, out var sound))
            sound = ResourceManager.Instance.GetSound(name);

        if (sound == null) 
        {
            Console.WriteLine($"[AudioManager] Sound '{name}' not found.");
            return;
        }

        sound?.Play(SFXVolume * volume, pitch, pan);
    }

    public void LoadSong(string name, Song song)
    {
        _songs[name] = song;
    }

    public void PlaySong(string name, bool isRepeating = true)
    {
        if (!_songs.TryGetValue(name, out var song))
            song = ResourceManager.Instance.GetSong(name);

        if (song == null) return;
        MediaPlayer.Stop();
        MediaPlayer.IsRepeating = isRepeating;
        MediaPlayer.Play(song);

        CurrentSongName = name;
    }

    public void StopSong()
    {
        MediaPlayer.Stop();
        CurrentSongName = null;
    }
}