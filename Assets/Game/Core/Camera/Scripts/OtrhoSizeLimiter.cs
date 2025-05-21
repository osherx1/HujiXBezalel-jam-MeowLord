using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Cinemachine extension to prevent orthographic camera from zooming out
/// farther than the background bounds.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("Cinemachine/Extensions/OrthoSizeLimiter")]
public class OrthoSizeLimiter : CinemachineExtension
{
    [Tooltip("Assign your background SpriteRenderer here")]
    [SerializeField] private SpriteRenderer backgroundRenderer;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (backgroundRenderer == null || stage != CinemachineCore.Stage.Body)
            return;

        if (!state.Lens.Orthographic)
            return;

        Bounds bounds = backgroundRenderer.bounds;
        float screenAspect = (float)Screen.width / Screen.height;
        float boundsAspect = bounds.size.x / bounds.size.y;
        float maxOrthoSize = bounds.size.y / 2f;

        // If screen is wider than the background, clamp by width
        if (screenAspect < boundsAspect)
            maxOrthoSize = bounds.size.x / 2f / screenAspect;

        // Clamp!
        state.Lens.OrthographicSize = Mathf.Min(state.Lens.OrthographicSize, maxOrthoSize);
    }
}