using UnityEngine;
using Game.Core.Audio;

public class BackgroundMusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioName musicToPlay = AudioName.BackgroundMusic;

    private void Start()
    {
        TryPlayBackgroundMusic();
    }

    private void TryPlayBackgroundMusic()
    {
       
        AudioSource currentMusicSource = AudioManager.Instance.GetMusicSource();

        if (currentMusicSource != null &&
            currentMusicSource.clip != null &&
            currentMusicSource.clip.name == GetClipName(musicToPlay) &&
            currentMusicSource.isPlaying)
        {
            return;
        }

        // נגן את המוזיקה
        AudioManager.Instance.Play(musicToPlay, Vector3.zero);
    }

    private string GetClipName(AudioName name)
    {
        Sound[] sounds = AudioManager.Instance.GetAllSounds();
        foreach (var s in sounds)
        {
            if (s.name == name && s.clip != null)
                return s.clip.name;
        }
        return null;
    }
}