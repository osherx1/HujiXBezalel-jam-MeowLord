using TMPro;
using UnityEngine;
using Game.Core.Managers;


namespace Game.UI.Scripts
{
    public class Timer : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI timerText;
        [SerializeField] private float timeLeft = 300;//5 minutes
        [SerializeField] private float blinkTimer = 20f;
        [SerializeField] private float redTimer = 5f;
        
        private bool _isFlashing;
        private float _flashTimer;
        private bool _isRed;

    
        // Update is called once per frame
        void Update()
        {
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
            timerText.text = $"{minutes:00}:{seconds:00}";
            
            // Handle flashing
            if (timeLeft <= redTimer)
            {
                // Stop flashing and stay red
                timerText.color = Color.red;
                _isFlashing = false;
            }
            else if (timeLeft <= blinkTimer)
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
