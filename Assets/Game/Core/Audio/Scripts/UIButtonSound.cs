using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.Audio.Scripts
{
    [RequireComponent(typeof(Button))]
    public class UIButtonSound : MonoBehaviour
    {
        [SerializeField] private AudioName clickSound = AudioName.ButtonClick;
        
        void Start()
        {
            Debug.Log("✅ UIButtonSound script loaded");
        }


        public void PlayClickSound()
        {
            Debug.Log($"🔊 Playing click sound: {clickSound}");
            AudioManager.Instance.Play(clickSound, Vector3.one);
        }
    }
}