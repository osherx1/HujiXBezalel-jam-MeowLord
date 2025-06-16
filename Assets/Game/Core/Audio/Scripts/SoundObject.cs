using UnityEngine;
using Generics_IPoolable = Game.Core.Generics.IPoolable;

namespace Game.Core.Audio
{
    public class SoundObject : MonoBehaviour, Generics_IPoolable
    {
        [SerializeField] public AudioSource audioSource;

        public void Update()
        {
            if (!audioSource.isPlaying)
            {
                SoundPool.Instance.Return(this);
            }
        }

        public void Play(Sound sound, Vector3 pos)
        {
            gameObject.transform.position = pos;
            audioSource.clip = sound.clip;
            audioSource.volume = sound.volume;
            audioSource.pitch = sound.pitch;
            audioSource.loop = sound.loop;
            audioSource.spatialBlend = sound.spatialBlend;
            audioSource.Play();
        }
        
        public void StopSound()
        {
            if (audioSource.isPlaying) 
            {
                audioSource.Stop();
            }
            
        }

        public void Reset()
        {
        
        }
    }
}
