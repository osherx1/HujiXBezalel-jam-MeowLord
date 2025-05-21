using System;
using UnityEngine;

[DisallowMultipleComponent]
public class MouseSensor : MonoBehaviour
{
    [Tooltip("Number of 'Mouse' objects required to activate the flag")]
    [SerializeField] private int requiredCount = 1;

    [Tooltip("Tag used by mouse objects")]
    [SerializeField] private string mouseTag = "Rat";
    
    [Tooltip("Layer to detect (use LayerMask.NameToLayer)")]
    [SerializeField] private string targetLayerName = "Enemy";
    private int targetLayer;
    
    public event Action OnPlatformDown;
    
    private void Awake()
    {
        targetLayer = LayerMask.NameToLayer(targetLayerName);
    }

    
    /// <summary>
    /// Public flag – true when enough mice are within range.
    /// </summary>
    public bool IsMouseInRange { get; private set; }

    private int currentCount;

    // Called when another collider enters the trigger
    // Called when another collider enters the trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(mouseTag) && other.gameObject.layer == targetLayer)
        {
            currentCount++;
            EvaluateState();
        }
    }

// Called when another collider exits the trigger
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(mouseTag) && other.gameObject.layer == targetLayer)
        {
            currentCount = Mathf.Max(0, currentCount - 1);
            EvaluateState();
        }
    }

    // Updates the IsMouseInRange flag based on the current count
    private void EvaluateState()
    {
        IsMouseInRange = currentCount >= requiredCount;
    }
}