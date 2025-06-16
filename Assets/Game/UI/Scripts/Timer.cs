using TMPro;
using UnityEngine;

namespace Game.UI.Scripts
{
    public class Timer : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI timerText;
        private float _timePass;
        
        // Update is called once per frame
        void Update()
        {
            _timePass += Time.deltaTime;
            int minutes = Mathf.FloorToInt(_timePass / 60);
            int seconds = Mathf.FloorToInt(_timePass % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
}
