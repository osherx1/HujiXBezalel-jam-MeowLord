using UnityEngine;

namespace Game.Player.Scripts
{
    public class PlayerTrigger: MonoBehaviour
    {
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private string layerOneName;
        [SerializeField] private string layerTwoName;
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.isTrigger || !other.CompareTag("BackCollider"))
                return;
            _meshRenderer.sortingLayerName = _meshRenderer.sortingLayerName == layerOneName ? layerTwoName : layerOneName;
        }
    }
}