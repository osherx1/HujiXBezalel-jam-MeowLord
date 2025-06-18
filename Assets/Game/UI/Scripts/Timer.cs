using System;
using Game.Core.Managers;
using TMPro;
using UnityEngine;

namespace Game.UI.Scripts
{
    public class Timer : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI timerText;

        private float _timePass = 0f;
        private bool activate = false;

        public void OnEnable()
        {
            GameEvents.OnGameStarted += Activate;
        }
        
        public void OnDisable()
        {
            GameEvents.OnGameStarted -= Activate;
        }

        private void Activate()
        {
            activate = true;
        }

        // Update is called once per frame
        void Update()
        {
            if(!activate) return;
            _timePass += Time.deltaTime;
            int minutes = Mathf.FloorToInt(_timePass / 60);
            int seconds = Mathf.FloorToInt(_timePass % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
}

