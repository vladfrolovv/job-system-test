using UnityEngine;
namespace Enemies
{
    [CreateAssetMenu(fileName = "EnemySpawnerConfig", menuName = "SO/EnemySpawnerConfig")]
    public class EnemySpawnerConfig : ScriptableObject
    {

        [SerializeField] private int _enemiesAmount;
        [SerializeField] private float _spawnRadius;

        public int EnemiesAmount => _enemiesAmount;
        public float SpawnRadius => _spawnRadius;

    }
}
