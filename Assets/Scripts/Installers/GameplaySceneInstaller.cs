using System;
using Enemies;
using Player;
using UnityEngine;
using Zenject;
namespace Installers
{
    public class GameplaySceneInstaller : MonoInstaller
    {

        [SerializeField] private EnemyView _enemyViewPrefab;

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<EnemiesController>().AsSingle().NonLazy();
            Container.Bind<EnemiesParent>().FromComponentInHierarchy().AsSingle();

            Container.BindInterfacesAndSelfTo<PlayerMovement>().AsSingle().NonLazy();
            Container.Bind<PlayerView>().FromComponentInHierarchy().AsSingle();

            Container.BindInstance(_enemyViewPrefab).AsSingle();
        }

    }
}
