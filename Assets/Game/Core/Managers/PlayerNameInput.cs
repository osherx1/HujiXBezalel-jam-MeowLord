using UnityEngine;
using TMPro;
using Game.Core.Managers;

public class PlayerNameInput : MonoBehaviour
{
    public TMP_InputField nameInputField;

    public void OnSubmitName()
    {
        string playerName = nameInputField.text;
        if (!string.IsNullOrWhiteSpace(playerName))
        {
            GameManager.Instance.SetNickname(playerName); // Update the nickname in GameManager
        }
        else
        {
            GameManager.Instance.SetNickname(null);
        }
        GameEvents.GameInitialization();
        
        
    }
}