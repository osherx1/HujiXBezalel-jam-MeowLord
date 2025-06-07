using TMPro;
using UnityEngine;
using Game.Core.Managers;


namespace Game.UI.Scripts
{
    public class Timer : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI timerText;
        [SerializeField] private float _timeLeft = 300;//5 minutes
        private bool _isFlashing = false;
        private float _flashTimer = 0f;
        private bool _isRed = false;

    
        // Update is called once per frame
        void Update()
        {
            if (_timeLeft > 0)
            {
                _timeLeft -= Time.deltaTime;
            }

            if (_timeLeft < 0)
            {
                _timeLeft = 0;
                GameEvents.GameFinished();
            }
            int minutes = Mathf.FloorToInt(_timeLeft / 60);
            int seconds = Mathf.FloorToInt(_timeLeft % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
            
            // Handle flashing
            if (_timeLeft <= 10)
            {
                // Stop flashing and stay red
                timerText.color = Color.red;
                _isFlashing = false;
            }
            else if (_timeLeft <= 30)
            {
                if (!_isFlashing)
                {
                    _isFlashing = true;
                    _flashTimer = 0f;
                }

                // Flashing logic
                _flashTimer += Time.deltaTime;
                if (_flashTimer >= 0.5f) // Toggle every 0.5 seconds
                {
                    _flashTimer = 0f;
                    _isRed = !_isRed;
                    timerText.color = _isRed ? Color.red : Color.white;
                }
            }
            else
            {
                // Reset to default
                timerText.color = Color.white;
                _isFlashing = false;
            }
        }
    }
}
