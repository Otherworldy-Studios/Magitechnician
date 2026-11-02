using System;
using MoreMountains.Feedbacks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using MoreMountains.Tools;
using UnityEngine.InputSystem;

public class SoundController : MonoBehaviour
{
    
    [Serializable]
    public class Sound
    {
        public string name;
        public MMF_MMSoundManagerSoundData soundData;
    }

    [SerializeField] private Sound[] sfxSounds;
    [SerializeField] private Sound[] musicTracks;
    [SerializeField] private Sound[] uiSounds;

    private static SoundController _instance;

    private void Awake()
    {
        if (_instance == null)
            _instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        foreach (var sfxSound in  sfxSounds)
            sfxSound.soundData.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Sfx;
        foreach (var musicTrack in  musicTracks)
            musicTrack.soundData.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Music;
        foreach (var uiSound in  uiSounds)
            uiSound.soundData.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.UI;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.nKey.wasPressedThisFrame)
                PlaySound(MMSoundManager.MMSoundManagerTracks.UI,"U Click");
            if (Keyboard.current.mKey.wasPressedThisFrame)
                PlaySound(MMSoundManager.MMSoundManagerTracks.Music,"Sewer Theme", new Vector3(-5f, -5f, 5f));
        }
    }
#endif

    public void PlaySound(MMSoundManager.MMSoundManagerTracks trackType, string soundName, Vector3 atPosition)
    {
        Sound s;
        
        if (trackType == MMSoundManager.MMSoundManagerTracks.Sfx)
            s = Array.Find(sfxSounds, sfxSound => sfxSound.name == soundName);
        else if (trackType == MMSoundManager.MMSoundManagerTracks.Music)
            s = Array.Find(musicTracks, musicTrack => musicTrack.name == soundName);
        else if (trackType == MMSoundManager.MMSoundManagerTracks.UI)
            s = Array.Find(uiSounds, uiSound => uiSound.name == soundName);
        else
            s = null;
        
        if (s == null)
        {
            Debug.LogWarning("Sound not found: " + soundName);
            return;
        }
        
        s.soundData.Play(atPosition);
    }

    public void PlaySound(MMSoundManager.MMSoundManagerTracks trackType, string soundName)
    {
        PlaySound(trackType, soundName, transform.position);
    }

    /*
    public void PlaySound(string soundName, Vector3 uniquePosition)
    {
        Sound s = Array.Find(sounds, sound => sound.name == soundName);
        if (s == null)
        {
            Debug.LogWarning("Sound not found: " + soundName);
            return;
        }

        MMSoundManagerSoundPlayEvent.Trigger(s.clip, s.track, uniquePosition);

        MMSoundManagerPlayOptions options;
        options = MMSoundManagerPlayOptions.Default;
        options.Volume = s.volume;
        options.Pitch = s.pitch;
        options.Loop = s.loop;
    }

    public void PlaySound(string soundName)
    {
        PlaySound(soundName, transform.position);
    }
    */
}
