using UnityEngine;

namespace Game.Core.Audio.Scripts
{
    public class KeySoundPlayer : MonoBehaviour
    {
        [SerializeField]private AudioName keyPressSoundName; 

        void Update()
        {
            foreach (KeyCode kcode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (UnityEngine.Input.GetKeyDown(kcode))
                {
                    if ((kcode >= KeyCode.A && kcode <= KeyCode.Z) ||
                        (kcode >= KeyCode.Alpha0 && kcode <= KeyCode.Alpha9))
                    {
                        AudioManager.Instance.Play(keyPressSoundName, UnityEngine.Camera.main.transform.position);
                    }
                }
            }
        }

    }
}