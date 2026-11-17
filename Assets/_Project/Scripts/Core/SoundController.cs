using System;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Plays sound effects, music, and UI sounds by name through Feel's MMSoundManager.
/// Each track has its own list of named sounds, filled in the Inspector. Call PlaySFX, PlayMusic, or PlayUI
/// with a sound's name through SoundController.Instance.
/// </summary>
// INTEGRATION: was a MonoBehaviour with a private static instance and DontDestroyOnLoad, so no other script could reach it.
// Now inherits MGR<SoundController> and lives in the Managers prefab, so any script can use SoundController.Instance.
public class SoundController : MGR<SoundController>
{
    /// <summary>
    /// A named sound: the name other scripts pass to the Play methods, and the Feel sound data to play.
    /// </summary>
    [Serializable]
    public class Sound
    {
        [Tooltip("The name scripts use to play this sound, for example \"UI Click\".")]
        [SerializeField, FormerlySerializedAs("name")] private string _name;
        [Tooltip("The clip and its playback settings.")]
        [SerializeField, FormerlySerializedAs("soundData")] private MMF_MMSoundManagerSoundData _soundData;

        public string Name => _name;
        public MMF_MMSoundManagerSoundData SoundData => _soundData;
    }

    // One list per track. Each starts empty and is filled in the Inspector.
    [SerializeField, FormerlySerializedAs("sfxSounds")] private Sound[] _sfxSounds;    
    [SerializeField, FormerlySerializedAs("musicTracks")] private Sound[] _musicTracks;
    [SerializeField, FormerlySerializedAs("uiSounds")] private Sound[] _uiSounds;      

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

    // INTEGRATION: the N, M, and B debug keys became these Inspector buttons.

    /// <summary>Debug: plays a sound effect by name at the SoundController's position.</summary>
    [Button, DisableInEditorMode]
    public void DEBUGPlaySFX(string soundName)
    {
        PlaySFX(soundName, transform.position);
    }

    /// <summary>Debug: plays a music track by name.</summary>
    [Button, DisableInEditorMode]
    public void DEBUGPlayMusic(string soundName)
    {
        PlayMusic(soundName, transform.position);
    }

    /// <summary>Debug: plays a UI sound by name.</summary>
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

    /// <summary>Plays a music track by name at a position in the scene.</summary>
    public void PlayMusic(string soundName, Vector3 atPosition)
    {
        Sound s = Array.Find(_musicTracks, musicTrack => musicTrack.Name == soundName);
        PlaySound(s, soundName, atPosition);
    }

    /// <summary>Plays a UI sound by name at a position in the scene.</summary>
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

    // Overloads that play at the SoundController's own position.

    /// <summary>Plays a sound effect by name at the SoundController's position.</summary>
    public void PlaySFX(string soundName)
    {
        PlaySFX(soundName, transform.position);
    }

    /// <summary>Plays a music track by name at the SoundController's position.</summary>
    public void PlayMusic(string soundName)
    {
        PlayMusic(soundName, transform.position);
    }

    /// <summary>Plays a UI sound by name at the SoundController's position.</summary>
    public void PlayUI(string soundName)
    {
        PlayUI(soundName, transform.position);
    }
}
