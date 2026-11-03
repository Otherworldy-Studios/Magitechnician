using System;
using MoreMountains.Feedbacks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using MoreMountains.Tools;
using UnityEngine.InputSystem;
using UnityEngine.ProBuilder.MeshOperations;

public class SoundController : MonoBehaviour
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

    // Instance of SounController class, ensures class remains as singleton
    private static SoundController _instance;

    private void Awake()
    {
        // Keep class as singleton
        if (_instance == null)
            _instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

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

    /// <summary>
    /// Used to test out various sounds in Play Mode
    /// </summary>
    /// <param name="N Key">Plays UI sound in the same position as Sound Manager</param>
    /// <param name="M Key">Plays Music track in a distinct position in the scene</param>
    /// <param name="B Key">Plays randomized SFX in a distinct position in the scene</param>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.nKey.wasPressedThisFrame)
                PlayUI("UI Click");
            if (Keyboard.current.mKey.wasPressedThisFrame)
                PlayMusic("Sewer Theme", new Vector3(-5f, -5f, 5f));
            if (Keyboard.current.bKey.wasPressedThisFrame)
                PlaySFX("Hammer Strike", new Vector3(1, 2, -3));
        }
    }
#endif

    
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
