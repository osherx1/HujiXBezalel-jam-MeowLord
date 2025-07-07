using System.Collections.Generic;
using Game.Core.Managers;
using Game.Core.Score;
using UnityEngine;
using TMPro; // If you use TMP

public class LeaderboardUI : MonoBehaviour
{
    public TMP_Text[] nameFields;
    public TMP_Text[] scoreFields;
    public TMP_Text[] timeFields;

    private void Start()
    {
        var leaderboardEntries = GameManager.Instance.HighScoreManager.GetHighScoreTable();
        UpdatingScore(leaderboardEntries);
    }
    void UpdatingScore(List<LeaderboardEntry> table)
    {
        Debug.Log("Entered Callback");
        UnityMainThreadDispatcher.Instance.Enqueue(() => {
        for (int i = 0; i < nameFields.Length; i++)
        {
            Debug.Log("Starting to assign values");
            if (i < table.Count)
            {
                nameFields[i].text = table[i].Name;
                scoreFields[i].text = table[i].Score.ToString();
                timeFields[i].text = FormatTime(table[i].FinishTime);
            }
            else
            {
                nameFields[i].text = "";
                scoreFields[i].text = "";
                timeFields[i].text = "";
            }
        }
        });
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