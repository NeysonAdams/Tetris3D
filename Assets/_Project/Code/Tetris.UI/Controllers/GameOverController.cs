using Tetris.Core.Commands;
using Tetris.Core.Events;
using Tetris.Core.Scoring;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tetris.UI.Controllers
{
    public sealed class GameOverController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _finalScoreText = default!;
        [SerializeField] private Button _restartButton = default!;
        [SerializeField] private Button _quitButton = default!;

        private ICommandDispatcher _dispatcher = default!;
        private GameEvents _events = default!;
        private bool _isInitialized;

        public void Initialize(ICommandDispatcher dispatcher, GameEvents events)
        {
            if (_isInitialized)
            {
                throw new System.InvalidOperationException("GameOverController already initialized.");
            }

            if (dispatcher == null) throw new System.ArgumentNullException(nameof(dispatcher));
            if (events == null) throw new System.ArgumentNullException(nameof(events));

            if (_finalScoreText == null || _restartButton == null || _quitButton == null)
            {
                throw new System.InvalidOperationException(
                    "GameOverController references not assigned. Set finalScoreText, restartButton, quitButton in Inspector.");
            }

            _dispatcher = dispatcher;
            _events = events;

            _restartButton.onClick.AddListener(OnRestartClicked);
            _quitButton.onClick.AddListener(OnQuitClicked);
            _events.GameOver += HandleGameOver;

            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (_isInitialized)
            {
                _restartButton.onClick.RemoveListener(OnRestartClicked);
                _quitButton.onClick.RemoveListener(OnQuitClicked);
                _events.GameOver -= HandleGameOver;
            }
        }

        private void HandleGameOver(ScoreState finalScore)
        {
            _finalScoreText.text = finalScore.Score.ToString("N0");
        }

        private void OnRestartClicked()
        {
            _dispatcher.Dispatch(new RestartCommand());
        }

        private void OnQuitClicked()
        {
            _dispatcher.Dispatch(new QuitCommand());
        }
    }
}