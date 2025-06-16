using System;
using Game.Core.Generics;
using UnityEngine;

namespace Game.Core.Audio
{
    public class AudioManager : MonoSingleton<AudioManager>
    {
        public Sound[] sounds;
    
        public SoundObject Play(AudioName name, Vector3 pos)
        {
            Sound s = Array.Find(sounds, sound => sound.name == name);
            if (s == null)
            {
                Debug.LogWarning("Sound: " + name + " not found!");
                return null;
            }

       

            SoundObject soundObject = SoundPool.Instance.Get();
        
            soundObject.Play(s,pos);
            return soundObject;
        }
    
    }

    public enum AudioName
    {
        BackgroundMusic,
        ButtonClick,
        CatJump,
        Rats,
        CatBadJump,
        CatLand,
        MouseDeath
    }
}