using UnityEngine;

namespace Game.Player.Scripts
{
    [CreateAssetMenu(fileName = "PlayerStats", menuName = "Player/PlayerStats")]
    public class PlayerStats : ScriptableObject
    {
        public float radarRadius;
        public LayerMask platformLayer;
    }
}
