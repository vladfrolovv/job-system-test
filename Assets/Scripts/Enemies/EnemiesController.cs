using System;
using System.Collections.Generic;
using Player;
using UniRx;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using Object = UnityEngine.Object;
using Unity.Mathematics;
namespace Enemies
{
    [BurstCompile]
    public class EnemiesController : IDisposable
    {

        private readonly EnemyView _enemyViewPrefab;
        private readonly EnemyConfig _enemyConfig;
        private readonly EnemiesParent _enemiesParent;
        private readonly EnemySpawnerConfig _enemySpawnerConfig;

        private readonly PlayerView _playerView;

        private readonly CompositeDisposable _compositeDisposable = new();

        private readonly List<EnemyView> _enemies = new();

        private TransformAccessArray _transformAccessArray;
        private Transform[] _enemyTransforms;

        public EnemiesController(EnemyView enemyViewPrefab, EnemyConfig enemyConfig, PlayerView playerView,
                                 EnemySpawnerConfig enemySpawnerConfig, EnemiesParent enemiesParent)
        {
            _enemyViewPrefab = enemyViewPrefab;
            _enemyConfig = enemyConfig;
            _enemiesParent = enemiesParent;
            _enemySpawnerConfig = enemySpawnerConfig;
            _playerView = playerView;

            _enemyTransforms = new Transform[_enemySpawnerConfig.EnemiesAmount];

            SpawnAllEnemies();
            Observable.EveryUpdate().Subscribe(delegate
            {
                MoveEnemies();
            }).AddTo(_compositeDisposable);
        }

        public void Dispose()
        {
            _compositeDisposable?.Dispose();
        }

        private void SpawnAllEnemies()
        {
            for (int i = 0; i < _enemySpawnerConfig.EnemiesAmount; i++)
            {
                EnemyView enemy = SpawnEnemy();
                _enemyTransforms[i] = enemy.transform;
            }

            _transformAccessArray = new TransformAccessArray(_enemyTransforms);
        }

        private void MoveEnemies()
        {
            EnemyControlJob job = new EnemyControlJob()
            {
                PlayerPosition = _playerView.transform.position,
                DeltaTime = Time.deltaTime,
                DistanceThreshold = _enemyConfig.DistanceThreshold,
                Speed = _enemyConfig.Speed
            };

            JobHandle jobHandle = job.Schedule(_transformAccessArray);
            jobHandle.Complete();
        }

        private EnemyView SpawnEnemy()
        {
            Vector2 spawnPosition = UnityEngine.Random.insideUnitCircle * _enemySpawnerConfig.SpawnRadius;
            EnemyView enemy = Object.Instantiate(_enemyViewPrefab, spawnPosition, Quaternion.identity);
            enemy.transform.SetParent(_enemiesParent.transform);

            _enemies.Add(enemy);
            return enemy;
        }

    }

    [BurstCompile]
    public struct EnemyControlJob : IJobParallelForTransform
    {
        [ReadOnly] public float3 PlayerPosition;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public float DistanceThreshold;
        [ReadOnly] public float Speed;

        public void Execute(int index, TransformAccess transform)
        {
            float3 direction = math.normalize(PlayerPosition - (float3)transform.position);
            float angle = math.atan2(direction.y, direction.x);
            angle -= math.radians(90f);

            quaternion lookAtRotation = quaternion.AxisAngle(new float3(0, 0, 1), angle);
            transform.rotation = lookAtRotation;

            float distance = math.distance(PlayerPosition, transform.position);
            float3 newPosition = (float3)transform.position - direction * DeltaTime * Speed;
            float3 maskedPosition = math.select(transform.position, newPosition, distance <= DistanceThreshold);

            transform.position = maskedPosition;
        }
    }

}
