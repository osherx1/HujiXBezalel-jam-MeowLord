using UnityEngine;

namespace Game.Player.Scripts
{
    public class SegmentCollider: MonoBehaviour
    {
        [SerializeField] private Segment parentSegment;
        public void OnTriggerEnter2D(Collider2D other)
        {
            parentSegment.OnChildTriggerEnter(other);
        }
    }
}