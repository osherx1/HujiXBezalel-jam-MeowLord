using UnityEngine;
using System.Collections;
using Game.Core.Audio;
using Game.Core.Managers;

public static class CurtainMusicHelper
{
    public static void PlayWithFade(AudioName name, float delay = 2f, float fadeDuration = 1.5f)
    {
        GameManager.Instance.StartCoroutine(PlayAndFadeCoroutine(name, delay, fadeDuration));
    }

    private static IEnumerator PlayAndFadeCoroutine(AudioName name, float delay, float fadeDuration)
    {
        AudioManager.Instance.Play(name, Vector3.zero);
        yield return new WaitForSeconds(delay);
        AudioManager.Instance.StopAllMusicGradually(fadeDuration);
    }
}