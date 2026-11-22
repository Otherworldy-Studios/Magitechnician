using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Plays sound effects, music, and UI sounds by name through Feel's MMSoundManager.
/// Each track has its own list of named sounds, filled in the Inspector. Call PlaySFX, PlayMusic, or PlayUI
/// with a sound's name through SoundController.Instance. Only one music track plays at a time: PlayMusic crossfades
/// from the current track to the new one, and StopMusic fades the current track out.
/// </summary>
public class SoundController : MGR<SoundController>
{
    /// <summary>
    /// A named sound: the name other scripts pass to the Play methods, and the Feel sound data to play.
    /// </summary>
    [Serializable]
    public class Sound
    {
        [Tooltip("The name scripts use to play this sound, for example \"UI Click\".")]
        [SerializeField] private string _name;
        [Tooltip("The clip and its playback settings.")]
        [SerializeField] private MMF_MMSoundManagerSoundData _soundData;

        public string Name => _name;
        public MMF_MMSoundManagerSoundData SoundData => _soundData;
    }

    // One list per track. Each starts empty and is filled in the Inspector.
    [SerializeField] private Sound[] _sfxSounds;
    [SerializeField] private Sound[] _musicTracks;
    [SerializeField] private Sound[] _uiSounds;

    [Header("Music")]
    [Tooltip("Seconds PlayMusic takes to crossfade between tracks, and StopMusic takes to fade out. 0 cuts at once.")]
    [SerializeField, Min(0f)] private float _musicFadeDuration = 1f;

    private Sound _currentMusic;
    private readonly Dictionary<AudioSource, Coroutine> _fades = new(); // At most one fade per source.

    /// <summary>Puts every sound on the track of the list it's in, so the lists decide the track.</summary>
    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }

        foreach (Sound sfxSound in _sfxSounds)
        {
            sfxSound.SoundData.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Sfx;
        }

        foreach (Sound musicTrack in _musicTracks)
        {
            musicTrack.SoundData.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Music;
            musicTrack.SoundData.DoNotPlayIfClipAlreadyPlaying = true; // A track that's already playing doesn't restart.
        }

        foreach (Sound uiSound in _uiSounds)
        {
            uiSound.SoundData.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.UI;
        }
    }

    [Button, DisableInEditorMode]
    public void DEBUGPlaySFX(string soundName)
    {
        PlaySFX(soundName, transform.position);
    }

    [Button, DisableInEditorMode]
    public void DEBUGPlayMusic(string soundName)
    {
        PlayMusic(soundName, transform.position);
    }

    [Button, DisableInEditorMode]
    public void DEBUGStopMusic()
    {
        StopMusic();
    }

    [Button, DisableInEditorMode]
    public void DEBUGPlayUI(string soundName)
    {
        PlayUI(soundName, transform.position);
    }

    /// <summary>Plays a sound effect by name at a position in the scene.</summary>
    /// <param name="soundName">The sound's Name in the SFX list.</param>
    /// <param name="atPosition">Where in the scene the sound plays.</param>
    public void PlaySFX(string soundName, Vector3 atPosition)
    {
        Sound s = Array.Find(_sfxSounds, sfxSound => sfxSound.Name == soundName);
        PlaySound(s, soundName, atPosition);
    }

    /// <summary>
    /// Crossfades from the current music track to this one over Music Fade Duration. Asking for the track that's
    /// already playing changes nothing.
    /// </summary>
    public void PlayMusic(string soundName, Vector3 atPosition)
    {
        Sound s = Array.Find(_musicTracks, musicTrack => musicTrack.Name == soundName);
        if (s == null)
        {
            Debug.LogWarning("Sound not found: " + soundName);
            return;
        }

        if (s == _currentMusic && FindPlayingSources(s).Count > 0)
        {
            return;
        }

        FadeOutMusic();

        _currentMusic = s;
        s.SoundData.Play(atPosition);

        // MMSoundManager starts the track at its own volume. Start it silent and fade up to that volume.
        foreach (AudioSource source in FindPlayingSources(s))
        {
            float targetVolume = source.volume;
            StartFade(source, 0f, targetVolume, false);
        }
    }

    /// <summary>Fades the current music track out over Music Fade Duration, then frees its audio source.</summary>
    public void StopMusic()
    {
        FadeOutMusic();
        _currentMusic = null;
    }

    public void PlayUI(string soundName, Vector3 atPosition)
    {
        Sound s = Array.Find(_uiSounds, uiSound => uiSound.Name == soundName);
        PlaySound(s, soundName, atPosition);
    }

    /// <summary>Plays a sound that was found by name, or logs a warning if no sound has that name.</summary>
    /// <param name="s">The sound from one of the lists, or null if none matched.</param>
    /// <param name="soundName">The name that was looked up, for the warning.</param>
    /// <param name="atPosition">Where in the scene the sound plays.</param>
    private void PlaySound(Sound s, string soundName, Vector3 atPosition)
    {
        if (s == null)
        {
            Debug.LogWarning("Sound not found: " + soundName);
            return;
        }

        s.SoundData.Play(atPosition);
    }

    /// <summary>Plays a sound effect by name at a position, at a fraction of its normal volume.</summary>
    /// <param name="soundName">The sound's Name in the SFX list.</param>
    /// <param name="atPosition">Where in the scene the sound plays.</param>
    /// <param name="volumeScale">1 is the sound's normal volume, 0.5 is half.</param>
    public void PlaySFX(string soundName, Vector3 atPosition, float volumeScale)
    {
        Sound s = Array.Find(_sfxSounds, sfxSound => sfxSound.Name == soundName);
        if (s == null)
        {
            Debug.LogWarning("Sound not found: " + soundName);
            return;
        }

        // Scale the sound's volume range for this one play, then put it back.
        float minVolume = s.SoundData.MinVolume;
        float maxVolume = s.SoundData.MaxVolume;
        s.SoundData.MinVolume = minVolume * volumeScale;
        s.SoundData.MaxVolume = maxVolume * volumeScale;

        s.SoundData.Play(atPosition);

        s.SoundData.MinVolume = minVolume;
        s.SoundData.MaxVolume = maxVolume;
    }

    /// <summary>Stops every playing copy of a sound effect, such as a loop. Skipped if it isn't playing.</summary>
    /// <param name="soundName">The sound's Name in the SFX list.</param>
    public void StopSFX(string soundName)
    {
        Sound s = Array.Find(_sfxSounds, sfxSound => sfxSound.Name == soundName);
        if (s == null || !MMSoundManager.HasInstance)
        {
            return;
        }

        foreach (AudioSource source in FindPlayingSources(s))
        {
            MMSoundManager.Instance.FreeSound(source);
        }
    }

    /// <summary>Plays a sound effect by name at the SoundController's position.</summary>
    public void PlaySFX(string soundName)
    {
        PlaySFX(soundName, transform.position);
    }

    /// <summary>Crossfades to a music track by name, played at the SoundController's position.</summary>
    public void PlayMusic(string soundName)
    {
        PlayMusic(soundName, transform.position);
    }

    /// <summary>Plays a UI sound by name at the SoundController's position.</summary>
    public void PlayUI(string soundName)
    {
        PlayUI(soundName, transform.position);
    }

    private void FadeOutMusic()
    {
        if (_currentMusic == null) return;

        foreach (AudioSource source in FindPlayingSources(_currentMusic))
        {
            StartFade(source, source.volume, 0f, true);
        }
    }

    /// <summary>
    /// Every playing AudioSource with one of this sound's clips. Searches every AudioSource, because MMSoundManager's
    /// FindByClip can return an old, stopped source before the one that's still playing.
    /// </summary>
    private static List<AudioSource> FindPlayingSources(Sound s)
    {
        List<AudioSource> playing = new List<AudioSource>();

        foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            if (!source.isPlaying || source.clip == null) continue;

            bool isThisSound = source.clip == s.SoundData.Sfx
                               || (s.SoundData.RandomSfx != null && Array.IndexOf(s.SoundData.RandomSfx, source.clip) >= 0);

            if (isThisSound)
                playing.Add(source);
        }

        return playing;
    }

    /// <summary>Replaces any fade already running on the source, so two fades never fight over its volume.</summary>
    private void StartFade(AudioSource source, float fromVolume, float toVolume, bool freeWhenDone)
    {
        if (_fades.TryGetValue(source, out Coroutine running) && running != null)
        {
            StopCoroutine(running);
        }

        _fades[source] = StartCoroutine(Fade(source, fromVolume, toVolume, freeWhenDone));
    }

    // Unscaled time, so music still fades while the game is paused.
    private IEnumerator Fade(AudioSource source, float fromVolume, float toVolume, bool freeWhenDone)
    {
        float elapsed = 0f;

        while (elapsed < _musicFadeDuration && source != null)
        {
            source.volume = Mathf.Lerp(fromVolume, toVolume, elapsed / _musicFadeDuration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _fades.Remove(source);
        if (source == null) yield break;

        source.volume = toVolume;

        if (freeWhenDone && MMSoundManager.HasInstance)
        {
            MMSoundManager.Instance.FreeSound(source);
        }
    }
}