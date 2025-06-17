using System;
using UnityEngine;

namespace Game.Core.Audio
{
    public class AudioManager : MonoBehaviour
    {
        
        private float lastMusicVolume = 1f;
        private bool isMuted = false;
        public static AudioManager Instance { get; private set; }

        [SerializeField] private Sound[] sounds;

        [Header("Assigned Audio Source for music")]
        [SerializeField] private AudioSource musicSource; // ← תוכל לגרור לפה AudioSource מהסצנה

        private void Awake()
        {
            
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Play(AudioName name, Vector3 pos)
        {
            Sound s = Array.Find(sounds, sound => sound.name == name);
            if (s == null)
            {
                Debug.LogWarning("❌ Sound not found: " + name);
                return;
            }

            if (s.loop)
            {
                PlayLoopingMusic(s);
                return;
            }

            PlayOneShot(s, pos);
        }


        private void PlayOneShot(Sound s, Vector3 pos)
        {
            GameObject sfxObject = new GameObject("SFX_" + s.name);
            sfxObject.transform.position = pos;

            AudioSource src = sfxObject.AddComponent<AudioSource>();
            src.clip = s.clip;
            src.volume = s.volume;
            src.pitch = s.pitch;
            src.spatialBlend = s.spatialBlend;
            src.loop = false;
            src.Play();
            
            Destroy(sfxObject, s.clip.length / s.pitch);
        }

        public AudioSource GetMusicSource()
        {
            return musicSource;
        }

        public Sound[] GetAllSounds()
        {
            return sounds;
        }


        private void PlayLoopingMusic(Sound sound)
        {
            if (musicSource == null)
            {
                Debug.LogError("❌ No AudioSource assigned to AudioManager!");
                return;
            }

            if (musicSource.clip == sound.clip && musicSource.isPlaying)
                return;

            musicSource.clip = sound.clip;
            musicSource.loop = true;
            musicSource.volume = sound.volume;
            musicSource.pitch = sound.pitch;
            musicSource.spatialBlend = 0f;
            musicSource.Play();

            Debug.Log($"🎵 Playing background music: {sound.clip.name}");
        }
        
        public void ToggleMusicMute()
        {
            if (musicSource == null) return;

            if (isMuted)
            {
                musicSource.volume = lastMusicVolume;
                isMuted = false;
            }
            else
            {
                lastMusicVolume = musicSource.volume;
                musicSource.volume = 0f;
                isMuted = true;
            }

            Debug.Log($"🔇 Music muted: {isMuted}");
        }

        public bool IsMusicMuted()
        {
            return isMuted;
        }
    }
}
