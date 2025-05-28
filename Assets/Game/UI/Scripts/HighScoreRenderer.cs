using UnityEngine;
using Game.Core.Managers;
using System.Text;

namespace Game.UI.Scripts
{
    public class HighScoreRenderer : MonoBehaviour
    {
        private void OnEnable()
        {
            GameEvents.OnEndSceneStarted += PrintHighScores;
        }

        private void OnDisable()
        {
            GameEvents.OnEndSceneStarted  -= PrintHighScores;
        }

        private void PrintHighScores()
        {
            var highScoreManager = GameManager.Instance.HighScoreManager;
            highScoreManager.GetHighScoreTable(table => {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("High Scores:");
                foreach (var entry in table)
                {
                    sb.AppendLine($"{entry.Item1}. {entry.Item3} - {entry.Item2}");
                }
                Debug.Log(sb.ToString());
            });
        }
    }
} 