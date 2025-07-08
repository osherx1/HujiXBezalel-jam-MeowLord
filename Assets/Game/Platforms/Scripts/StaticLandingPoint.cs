using UnityEngine;

public class StaticLandingPoint : MonoBehaviour
{
    [Tooltip("Set the active landing point for the cat (drag any Transform here)")]
    [SerializeField] private Transform landingPoint;

    public Transform GetActiveCatLandingPoint()
    {
        return landingPoint != null ? landingPoint : transform;
    }
}