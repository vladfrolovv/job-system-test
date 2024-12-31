using UnityEngine;
namespace Player
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "SO/PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {

        [SerializeField] private float _speed;

        public float Speed => _speed;

    }

}
