using System;
using UnityEngine;
using System.Collections.Generic;

namespace Game.Core.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Settings")]
        [SerializeField] private AudioSource musicSource;

        [SerializeField] private Sound[] sounds;

        private float lastMusicVolume = 1f;
        private bool isMuted = false;

        private void Awake()
        {
            Instance = this;
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
            }
            else
            {
                PlayOneShot(s, pos);
            }
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

        public AudioSource GetMusicSource()
        {
            return musicSource;
        }

        public Sound[] GetAllSounds()
        {
            return sounds;
        }

        /// <summary>
        /// Instantly stops all music (all looping AudioSources).
        /// </summary>
        public void StopAllMusic()
        {
            if (musicSource != null && musicSource.isPlaying)
                musicSource.Stop();

            AudioSource[] allSources = FindObjectsOfType<AudioSource>();
            foreach (var src in allSources)
            {
                if (src != musicSource && src.isPlaying && src.loop)
                {
                    src.Stop();
                }
            }
        }

        /// <summary>
        /// Gradually fades out and stops all music (all looping AudioSources).
        /// </summary>
        /// <param name="fadeDuration">Fade-out time in seconds</param>
        public void StopAllMusicGradually(float fadeDuration = 1.0f)
        {
            StartCoroutine(FadeOutAllMusic(fadeDuration));
        }

        private System.Collections.IEnumerator FadeOutAllMusic(float fadeDuration)
        {
            AudioSource[] allSources = FindObjectsOfType<AudioSource>();
            var sourcesToFade = new List<AudioSource>();

            foreach (var src in allSources)
            {
                if (src != null && src.isPlaying && src.loop)
                    sourcesToFade.Add(src);
            }

            var startVolumes = new float[sourcesToFade.Count];
            for (int i = 0; i < sourcesToFade.Count; i++)
                startVolumes[i] = sourcesToFade[i].volume;

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float ratio = 1f - Mathf.Clamp01(t / fadeDuration);
                for (int i = 0; i < sourcesToFade.Count; i++)
                {
                    if (sourcesToFade[i] != null)
                        sourcesToFade[i].volume = startVolumes[i] * ratio;
                }
                yield return null;
            }

            for (int i = 0; i < sourcesToFade.Count; i++)
            {
                if (sourcesToFade[i] != null)
                {
                    sourcesToFade[i].volume = 0f;
                    sourcesToFade[i].Stop();
                }
            }
        }
    }
}
