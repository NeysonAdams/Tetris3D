using Tetris.Core.Commands;
using UnityEngine;
using UnityEngine.UI;

namespace Tetris.UI.Controllers
{
    public sealed class PauseController : MonoBehaviour
    {
        [SerializeField] private Button _resumeButton = default!;
        [SerializeField] private Button _quitButton = default!;

        private ICommandDispatcher _dispatcher = default!;
        private bool _isInitialized;

        public void Initialize(ICommandDispatcher dispatcher)
        {
            if (_isInitialized)
            {
                throw new System.InvalidOperationException("PauseController already initialized.");
            }

            if (dispatcher == null) throw new System.ArgumentNullException(nameof(dispatcher));

            if (_resumeButton == null || _quitButton == null)
            {
                throw new System.InvalidOperationException(
                    "PauseController button references not assigned. Set both in Inspector.");
            }

            _dispatcher = dispatcher;

            _resumeButton.onClick.AddListener(OnResumeClicked);
            _quitButton.onClick.AddListener(OnQuitClicked);

            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (_isInitialized)
            {
                _resumeButton.onClick.RemoveListener(OnResumeClicked);
                _quitButton.onClick.RemoveListener(OnQuitClicked);
            }
        }

        private void OnResumeClicked()
        {
            _dispatcher.Dispatch(new ResumeCommand());
        }

        private void OnQuitClicked()
        {
            _dispatcher.Dispatch(new QuitCommand());
        }
    }
}