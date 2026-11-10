using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.ProBuilder.MeshOperations;

public class SoundController : MGR<SoundController>
{
    /// <summary>
    /// Sound Class holds the name and SoundData file of a certain SFX, Song, or UI sound
    /// </summary>
    /// <param name="name">string</param>
    /// <param name="soundData">MMF_MMSoundManagerSoundData</param>
    [Serializable]
    public class Sound
    {
        public string name;
        public MMF_MMSoundManagerSoundData soundData;
    }

    // Separate Sound arrays of each track type begin empty and can be expanded upon in the Inspector
    [SerializeField] private Sound[] sfxSounds;
    [SerializeField] private Sound[] musicTracks;
    [SerializeField] private Sound[] uiSounds;


    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }
        // Ensures that on awake, each Sound Data in each Sound of each array is matched to the track it should fall under
        foreach (var sfxSound in  sfxSounds)
        {
            sfxSound.soundData.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Sfx;
        }
        
        foreach (var musicTrack in  musicTracks)
        {
            musicTrack.soundData.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Music;
            musicTrack.soundData.DoNotPlayIfClipAlreadyPlaying = true;
        }
        
        foreach (var uiSound in  uiSounds)
        {
            uiSound.soundData.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.UI;
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
    public void DEBUGPlayUI(string soundName)
    {
        PlayUI(soundName, transform.position);
    }

    // Plays Sound based on the name for each array. A position can be specified for where in the scene the Sound plays.
    
    public void PlaySFX(string soundName, Vector3 atPosition)
    {
        Sound s = Array.Find(sfxSounds, sfxSound => sfxSound.name == soundName);
        PlaySound(s, soundName, atPosition);
    }
    
    public void PlayMusic(string soundName, Vector3 atPosition)
    {
        Sound s = Array.Find(musicTracks, musicTrack => musicTrack.name == soundName);
        PlaySound(s, soundName, atPosition);
    }
   
    public void PlayUI(string soundName, Vector3 atPosition)
    {
        Sound s = Array.Find(uiSounds, uiSound => uiSound.name == soundName);
        PlaySound(s, soundName, atPosition);
    }

    /// <summary>
    /// Plays Sound based on the track type and name it falls under. A position can be specified for where in the scene the Sound plays
    /// </summary>
    /// <param name="trackType"></param>
    /// <param name="soundName"></param>
    /// <param name="atPosition"></param>
    private void PlaySound(Sound s, string soundName,  Vector3 atPosition)
    {
        if (s == null)
        {
            Debug.LogWarning("Sound not found: " + soundName);
            return;
        }
        
        s.soundData.Play(atPosition);
    }

    // Constructors of PLaySFX, PlayMusic, and PlayUI that automatically place the
    // audio at the position of the Sound Controller in the scene space.
    
    public void PlaySFX(string soundName)
    {
        PlaySFX(soundName, transform.position);
    }
    
    public void PlayMusic(string soundName)
    {
        PlayMusic(soundName, transform.position);
    }
    
    public void PlayUI(string soundName)
    {
        PlayUI(soundName, transform.position);
    }
}
