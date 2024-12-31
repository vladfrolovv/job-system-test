using UnityEngine;
namespace Enemies
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "SO/EnemyConfig")]
    public class EnemyConfig : ScriptableObject
    {

        [SerializeField] private float _speed;
        [SerializeField] private float _distanceThreshold;

        public float Speed => _speed;
        public float DistanceThreshold => _distanceThreshold;

    }
}
