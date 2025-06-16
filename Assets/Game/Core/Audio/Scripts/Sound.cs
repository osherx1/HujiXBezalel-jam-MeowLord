using UnityEngine;

namespace Game.Core.Audio
{
    [System.Serializable]
    public class Sound
    {
        public AudioName name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        [Range(0f, 1f)] public float spatialBlend = 0f;
        public bool loop = false;
    }
}