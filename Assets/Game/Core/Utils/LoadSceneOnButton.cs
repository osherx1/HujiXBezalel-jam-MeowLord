using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class LoadSceneOnButton : MonoBehaviour
    {
        [Tooltip("Build index of the scene to load (must be in Build Settings ▶ Scenes In Build)")]
        [SerializeField] private int sceneIndex;

        /// <summary>
        /// Call this from your Button’s OnClick().
        /// </summary>
        public void LoadScene()
        {
            // Validate against the total number of scenes in Build Settings
            int total = SceneManager.sceneCountInBuildSettings;
            if (sceneIndex < 0 || sceneIndex >= total)
            {
                Debug.LogError($"LoadSceneOnButton: sceneIndex {sceneIndex} is out of range (0–{total - 1})");
                return;
            }

            SceneManager.LoadScene(sceneIndex);
        }
    }
}