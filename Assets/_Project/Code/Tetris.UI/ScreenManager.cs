using DG.Tweening;
using Tetris.Core.Events;
using Tetris.Core.Scoring;
using Tetris.UI.Configs;
using UnityEngine;

namespace Tetris.UI
{
    public sealed class ScreenManager : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _mainMenu = default!;
        [SerializeField] private CanvasGroup _hud = default!;
        [SerializeField] private CanvasGroup _pauseOverlay = default!;
        [SerializeField] private CanvasGroup _gameOver = default!;

        private GameEvents _events = default!;
        private UIAnimationSettingsSO _animSettings = default!;

        private bool _isInitialized;

        public void Initialize(GameEvents events, UIAnimationSettingsSO animSettings)
        {
            if (_isInitialized) throw new System.InvalidOperationException("ScreenManager already initialized.");

            if (events == null) throw new System.ArgumentNullException(nameof(events));
            if (animSettings == null) throw new System.ArgumentNullException(nameof(animSettings));

            if (_mainMenu == null || _hud == null || _pauseOverlay == null || _gameOver == null)
            {
                throw new System.InvalidOperationException(
                    "ScreenManager CanvasGroup references not assigned. Set all 4 in Inspector.");
            }

            _events = events;
            _animSettings = animSettings;

            SetScreenInstant(_mainMenu, visible: true);
            SetScreenInstant(_hud, visible: false);
            SetScreenInstant(_pauseOverlay, visible: false);
            SetScreenInstant(_gameOver, visible: false);

            SubscribeToEvents();
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (_isInitialized)
            {
                UnsubscribeFromEvents();
            }
        }

        // Event handlers:
        private void HandleGameStarted()
        {
            HideScreen(_mainMenu);
            HideScreen(_gameOver);
            ShowScreen(_hud);
        }
        private void HandleGamePaused()
        {
            ShowScreen(_pauseOverlay);
        }
        private void HandleGameResumed()
        {
            HideScreen(_pauseOverlay);
        }
        private void HandleGameOver(ScoreState finalScore)
        {
            ShowScreen(_gameOver);
        }

        // Helpers:
        private void ShowScreen(CanvasGroup canvas)
        {
            canvas.DOKill();
            canvas.DOFade(1f, _animSettings.ScreenFadeDuration)
                .SetEase(_animSettings.ScreenFadeEase)
                .SetUpdate(isIndependentUpdate: true);
            canvas.interactable = true;
            canvas.blocksRaycasts = true;
        }
        private void HideScreen(CanvasGroup canvas)
        {
            canvas.DOKill();
            canvas.DOFade(0f, _animSettings.ScreenFadeDuration)
                .SetEase(_animSettings.ScreenFadeEase)
                .SetUpdate(isIndependentUpdate: true);
            canvas.interactable = false;
            canvas.blocksRaycasts = false;
        }
        private void SubscribeToEvents()
        {
            _events.GameStarted += HandleGameStarted;
            _events.GamePaused += HandleGamePaused;
            _events.GameResumed += HandleGameResumed;
            _events.GameOver += HandleGameOver;
        }
        private void UnsubscribeFromEvents()
        {
            _events.GameStarted -= HandleGameStarted;
            _events.GamePaused -= HandleGamePaused;
            _events.GameResumed -= HandleGameResumed;
            _events.GameOver -= HandleGameOver;
        }

        private static void SetScreenInstant(CanvasGroup canvas, bool visible)
        {
            canvas.alpha = visible ? 1f : 0f;
            canvas.interactable = visible;
            canvas.blocksRaycasts = visible;
        }
    }
}