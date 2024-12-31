using Enemies;
using Player;
using UnityEngine;
using Zenject;
namespace Installers
{
    [CreateAssetMenu(fileName = "ConfigsInstaller", menuName = "SO/ConfigsInstaller")]
    public class ConfigsInstaller : ScriptableObjectInstaller
    {

        [SerializeField] private EnemyConfig _enemyConfig;
        [SerializeField] private EnemySpawnerConfig _enemySpawnerConfig;
        [SerializeField] private PlayerConfig _playerConfig;

        public override void InstallBindings()
        {
            Container.BindInstance(_enemyConfig);
            Container.BindInstance(_enemySpawnerConfig);
            Container.BindInstance(_playerConfig);
        }

    }
}
