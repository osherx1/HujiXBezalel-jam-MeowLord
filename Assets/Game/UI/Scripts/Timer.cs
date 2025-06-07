using TMPro;
using UnityEngine;

namespace Game.UI.Scripts
{
    public class Timer : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI timerText;
        private float _timeLeft = 300;//5 minutes
    
        // Update is called once per frame
        void Update()
        {
            _timeLeft -= Time.deltaTime;
            int minutes = Mathf.FloorToInt(_timeLeft / 60);
            int seconds = Mathf.FloorToInt(_timeLeft % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
}
