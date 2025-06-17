using UnityEngine;
using UnityEngine.UI;
using Game.Core.Audio;

public class MusicToggleButton : MonoBehaviour
{
    [SerializeField] private Button toggleButton;
    [SerializeField] private Sprite muteIcon;
    [SerializeField] private Sprite unmuteIcon;
    [SerializeField] private Image iconImage;

    private void Start()
    {
        if (toggleButton == null) toggleButton = GetComponent<Button>();
        toggleButton.onClick.AddListener(OnToggleMusic);

        UpdateIcon();
    }

    private void OnToggleMusic()
    {
        AudioManager.Instance.ToggleMusicMute();
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (iconImage == null) return;

        if (AudioManager.Instance.IsMusicMuted())
            iconImage.sprite = muteIcon;
        else
            iconImage.sprite = unmuteIcon;
    }
}