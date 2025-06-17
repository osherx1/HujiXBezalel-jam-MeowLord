using System;
using UnityEngine;

namespace Game.Core.Audio
{
    public class AudioManager : MonoBehaviour
    {
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

        public void Play(AudioName name, Vector3 position)
        {
            Sound sound = Array.Find(sounds, s => s.name == name);
            if (sound == null)
            {
                Debug.LogWarning($"❌ Sound {name} not found!");
                return;
            }

            if (sound.loop)
            {
                PlayLoopingMusic(sound);
            }
            else
            {
                PlayOneShot(sound, position);
            }
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

        private void PlayOneShot(Sound sound, Vector3 position)
        {
            GameObject sfxObj = new GameObject("SFX_" + sound.name);
            sfxObj.transform.position = position;
            AudioSource src = sfxObj.AddComponent<AudioSource>();

            src.clip = sound.clip;
            src.volume = sound.volume;
            src.pitch = sound.pitch;
            src.spatialBlend = sound.spatialBlend;
            src.Play();

            Destroy(sfxObj, sound.clip.length / sound.pitch);
        }
    }
}
