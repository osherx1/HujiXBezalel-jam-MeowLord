using Game.Core.Audio;
using Game.Core.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.StartScene
{
    public class StartSceneManager : MonoBehaviour
    {
        [SerializeField] private Image viImage;
        [SerializeField] private bool startTutorial;
        [SerializeField] private TMP_InputField nameInputField;
        private string playerName;
        private bool active = true;


        public void Awake()
        {
            if (PlayerPrefs.GetInt("SawTutorial", 0) == 1)
            {
                viImage.enabled = false;
                startTutorial = false;
            }
            else
            {
                viImage.enabled = true;
                startTutorial = true;
            }

            playerName = PlayerPrefs.GetString("Name", null);
            GameManager.Instance.SetNickname(playerName);
        }

        public void Start()
        {
            nameInputField.text = playerName != null ? playerName : null;
            AudioManager.Instance.Play(AudioName.StartMusic, Vector3.zero);
            
        }

        public void Update()
        {
            if (playerName != nameInputField.text)
            {
                playerName = nameInputField.text;
                AudioManager.Instance.Play(AudioName.TypingLetter, Vector3.zero);
            }
        }

        public void OnTutorialButtonPressed()
        {
            viImage.enabled = !viImage.enabled;
            startTutorial = !startTutorial;
        }


        public void OnSubmitName()
        {
            if (!active) return;
            if (string.IsNullOrWhiteSpace(nameInputField.text))
            {
                // TODO indicate to player that his input not good by sound/text, or switch the name to the player prefs name
                return;
            }
            active = false;
            PlayerPrefs.SetInt("SawTutorial", 1);
            playerName = nameInputField.text.ToUpper();
            GameManager.Instance.SetNickname(playerName);
            PlayerPrefs.SetString("Name", playerName);
            GameEvents.GameInitialization();
            if (startTutorial)
            {
                GameManager.Instance.StartGameFromTutorial();
            }
            else
            {
                GameManager.Instance.StartGame();
            }
        }
    }
}