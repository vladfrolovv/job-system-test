using System;
using UniRx;
using UnityEngine;
namespace Player
{
    public class PlayerMovement : IDisposable
    {

        private readonly CompositeDisposable _compositeDisposable = new();

        public PlayerMovement(PlayerView playerView, PlayerConfig playerConfig)
        {
            Observable.EveryUpdate().Subscribe(delegate
            {
                Vector2 direction = GetMovementDirection();
                playerView.transform.Translate(direction * playerConfig.Speed * Time.deltaTime);
            }).AddTo(_compositeDisposable);
        }

        public void Dispose()
        {
            _compositeDisposable?.Dispose();
        }

        private Vector2 GetMovementDirection()
        {
            float x = Input.GetAxis("Horizontal");
            float y = Input.GetAxis("Vertical");

            return new Vector2(x, y);
        }

    }
}
