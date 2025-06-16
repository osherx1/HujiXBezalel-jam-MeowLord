using System.Collections.Generic;
using Game.Core.Managers;
using Game.Core.Score;
using UnityEngine;
using TMPro; // If you use TMP

public class LeaderboardUI : MonoBehaviour
{
    public TMP_Text[] nameFields;
    public TMP_Text[] scoreFields;
    
    private void OnEnable()
    {
        GameEvents.OnEndSceneStarted += PrintHighScores;
    }

    private void OnDisable()
    {
        GameEvents.OnEndSceneStarted  -= PrintHighScores;
    }

    void CallBackForUpdatingScore(List<(int, int, string, float)> table)
    {
        Debug.Log("Entered Callback");
        UnityMainThreadDispatcher.Instance.Enqueue(() => {
        for (int i = 0; i < nameFields.Length; i++)
        {
            Debug.Log("Starting to assign values");
            if (i < table.Count)
            {
                nameFields[i].text = table[i].Item3;
                scoreFields[i].text = table[i].Item2.ToString();
            }
            else
            {
                nameFields[i].text = "";
                scoreFields[i].text = "";
            }
        }
        });
    }
    void PrintHighScores()
    {
        // Suppose you have access to your HighScoreManager
        GameManager.Instance.HighScoreManager.GetHighScoreTable(CallBackForUpdatingScore);
    }
    
    
    

    string FormatTime(float seconds)
    {
        int min = Mathf.FloorToInt(seconds / 60f);
        int sec = Mathf.CeilToInt(seconds % 60f);
        if (sec == 60)
        {
            min += 1;
            sec = 0;
        }
        return $"{min:00}:{sec:00}";
    }
}