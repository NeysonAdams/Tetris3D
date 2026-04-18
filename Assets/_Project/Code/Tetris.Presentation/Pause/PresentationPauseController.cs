using DG.Tweening;
using Tetris.Core.Events;
using UnityEngine;

namespace Tetris.Presentation.Pause
{
    public sealed class PresentationPauseController : MonoBehaviour
    {
        private GameEvents _events = default!;
        private bool _isInitialized;

        public void Initialize(GameEvents events)
        {
            if (_isInitialized)
            {
                throw new System.InvalidOperationException("PresentationPauseController already initialized.");
            }

            if (events == null) throw new System.ArgumentNullException(nameof(events));

            _events = events;

            _events.GamePaused += HandleGamePaused;
            _events.GameResumed += HandleGameResumed;

            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (_isInitialized)
            {
                _events.GamePaused -= HandleGamePaused;
                _events.GameResumed -= HandleGameResumed;

                DOTween.timeScale = 1f;
            }
        }

        private void HandleGamePaused()
        {
            DOTween.timeScale = 0f;
        }

        private void HandleGameResumed()
        {
            DOTween.timeScale = 1f;
        }
    }
}