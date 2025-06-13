using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Core.Managers
{
    public class StartSceneManager: MonoBehaviour
    {
        [SerializeField] private Image viImage;
        [SerializeField] private bool startTutorial;
        [SerializeField] private TMP_InputField nameInputField;
        private string playerName;


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
            playerName = PlayerPrefs.GetString("Name", "Name");
            GameManager.Instance.SetNickname(playerName);
        }

        public void Start()
        {
            nameInputField.text = playerName;
        }

        public void OnTutorialButtonPressed()
        {
            viImage.enabled = !viImage.enabled;
            startTutorial = !startTutorial;
        }
        

        public void OnSubmitName()
        {
            if (string.IsNullOrWhiteSpace(nameInputField.text))
            {
                // TODO indicate to player that his input not good by sound/text, or switch the name to the player prefs name
                return;
            }
            PlayerPrefs.SetInt("SawTutorial", 1);
            playerName = nameInputField.text;
            GameManager.Instance.SetNickname(playerName);
            PlayerPrefs.SetString("Name", playerName);
            if (startTutorial)
            {
                GameEvents.GameInitialization();
                SceneManager.LoadScene(1);
            }
            else
            {
                GameEvents.GameInitialization();
                SceneManager.LoadScene(2);
            }
            
        } 

    }
}