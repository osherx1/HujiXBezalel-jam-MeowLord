using TMPro;
using UnityEngine;

namespace Game.UI.Scripts
{
    public class Timer : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI timerText;
        private float _timePass;
        
<<<<<<< HEAD
        // Update is called once per frame
        void Update()
        {
            _timePass += Time.deltaTime;
            int minutes = Mathf.FloorToInt(_timePass / 60);
            int seconds = Mathf.FloorToInt(_timePass % 60);
=======
        [SerializeField] private Graphic[] edgeFlashes;
        
        private bool _isFlashing;
        private float _flashTimer;
        private bool _isRed;
        [SerializeField] private bool active = true;

        void Start()
        {
            int minutes = Mathf.FloorToInt(timeLeft / 60);
            int seconds = Mathf.FloorToInt(timeLeft % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
        // Update is called once per frame
        void Update()
        {
            if (!active) return;
            if (timeLeft > 0)
            {
                timeLeft -= Time.deltaTime;
            }

            if (timeLeft < 0)
            {
                timeLeft = 0;
                GameEvents.GameFinished();
            }
            int minutes = Mathf.FloorToInt(timeLeft / 60);
            int seconds = Mathf.FloorToInt(timeLeft % 60);
>>>>>>> da0ec8e42b824017bbf46c340d711ac686e61728
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
}
