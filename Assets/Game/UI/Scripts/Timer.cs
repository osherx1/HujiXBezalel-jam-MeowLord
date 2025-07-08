using System;
using Game.Core.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Scripts
{
    public class Timer : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI timerText;
        [SerializeField] private float _timeLeft = 300;//5 minutes
        private bool _isFlashing = false;
        private float _flashTimer = 0f;
        private bool _isRed = false;
        private int _flashRed = 15;
        private int _allRed = 5;
        [SerializeField] private Graphic[] edgeFlashes;
        

        private bool activate = false;

        public void OnEnable()
        {
            GameEvents.OnGameStarted += Activate;
            GameEvents.OnGameFinished += Stop;
        }
        
        public void OnDisable()
        {
            GameEvents.OnGameStarted -= Activate;
            GameEvents.OnGameFinished += Stop;
        }

        private void Activate()
        {
            activate = true;
        }

        private void Stop()
        {
            activate = false;
        }

        // Update is called once per frame
        void Update()
        {
            if (!activate) return;
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
            if (_timeLeft <= _allRed)
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
            else if (_timeLeft <= _flashRed)
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
                if(edgeFlashes == null) return;
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

