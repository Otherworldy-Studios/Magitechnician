using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using MoreMountains.Tools;
using UnityEngine.InputSystem;

public class SoundController : MonoBehaviour
{
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        public MMSoundManager.MMSoundManagerTracks track;

        [Range(0f, 1f)] public float volume = 1f;
        [Range(-3f, 3f)] public float pitch = 1f;

        public bool loop;
    }

    [SerializeField] private Sound[] sounds;

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
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.nKey.wasPressedThisFrame)
                PlaySound("UI Click");
            if (Keyboard.current.mKey.wasPressedThisFrame)
                PlaySound("Sewer Theme", new Vector3(-5f, -5f, 5f));
        }
    }
#endif

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
}
