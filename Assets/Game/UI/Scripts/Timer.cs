using TMPro;
using UnityEngine;
using Game.Core.Managers;
using UnityEngine.UI;


namespace Game.UI.Scripts
{
    public class Timer : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI timerText;
        [SerializeField] private float timeLeft = 300;//5 minutes
        [SerializeField] private float blinkTimer = 20f;
        [SerializeField] private float redTimer = 5f;
        
        [SerializeField] private Graphic[] edgeFlashes;
        
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
                foreach (var edge in edgeFlashes)
                {
                    if (edge != null)
                        edge.color = Color.red;
                }
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
                    Color flashColor = _isRed ? Color.red : Color.white;
                    timerText.color = flashColor;

                    foreach (var edge in edgeFlashes)
                    {
                        if (edge != null)
                            edge.color = flashColor;
                    }
                }
            }
            else
            {
                // Reset to default
                timerText.color = Color.white;
                foreach (var edge in edgeFlashes)
                {
                    if (edge != null)
                        edge.color = Color.white;
                }
                _isFlashing = false;
            }
        }
    }
}
