using Tetris.Core.Commands;
using UnityEngine;
using UnityEngine.UI;

namespace Tetris.UI.Controllers
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _playButton = default!;
        [SerializeField] private Button _quitButton = default!;

        private ICommandDispatcher _dispatcher = default!;
        private bool _isInitialized;

        public void Initialize(ICommandDispatcher dispatcher)
        {
            if (_isInitialized)
            {
                throw new System.InvalidOperationException("MainMenuController already initialized.");
            }

            if (dispatcher == null) throw new System.ArgumentNullException(nameof(dispatcher));

            if (_playButton == null || _quitButton == null)
            {
                throw new System.InvalidOperationException(
                    "MainMenuController button references not assigned. Set both in Inspector.");
            }

            _dispatcher = dispatcher;

            _playButton.onClick.AddListener(OnPlayClicked);
            _quitButton.onClick.AddListener(OnQuitClicked);

            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (_isInitialized)
            {
                _playButton.onClick.RemoveListener(OnPlayClicked);
                _quitButton.onClick.RemoveListener(OnQuitClicked);
            }
        }

        private void OnPlayClicked()
        {
            _dispatcher.Dispatch(new StartCommand());
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}