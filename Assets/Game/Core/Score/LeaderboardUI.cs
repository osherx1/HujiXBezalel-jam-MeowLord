using Game.Core.Managers;
using Game.Core.Score;
using UnityEngine;
using TMPro; // If you use TMP

public class LeaderboardUI : MonoBehaviour
{
    public TMP_Text[] nameFields;
    public TMP_Text[] scoreFields;
    public TMP_Text[] timeFields;

    void Start()
    {
        // Suppose you have access to your HighScoreManager
        var table = GameManager.Instance.HighScoreManager.GetHighScoreTable();


        for (int i = 0; i < nameFields.Length; i++)
        {
            if (i < table.Count)
            {
                nameFields[i].text = table[i].Nickname;
                scoreFields[i].text = table[i].Score.ToString();
                timeFields[i].text = FormatTime(table[i].TimeFinished);
            }
            else
            {
                nameFields[i].text = "";
                scoreFields[i].text = "";
                timeFields[i].text = "";
            }
        }
    }

    string FormatTime(float seconds)
    {
        int min = Mathf.FloorToInt(seconds / 60f);
        int sec = Mathf.FloorToInt(seconds % 60f);
        return $"{min:00}:{sec:00}";
    }
}