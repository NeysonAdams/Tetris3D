using DG.Tweening;
using Tetris.Core.Events;
using Tetris.Core.StateMachine;
using Tetris.UI.Configs;
using TMPro;
using UnityEngine;

namespace Tetris.UI.Displays
{
    public sealed class LevelDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _levelText = default!;

        private GameEvents _events = default!;
        private UIAnimationSettingsSO _animSettings = default!;
        private Tween? _pulseTween;

        private bool _isInitialized;

        public void Initialize(
            GameEvents events,
            IGameStateReader stateReader,
            UIAnimationSettingsSO animSettings)
        {
            if (_isInitialized)
            {
                throw new System.InvalidOperationException("LevelDisplay already initialized.");
            }

            if (events == null) throw new System.ArgumentNullException(nameof(events));
            if (stateReader == null) throw new System.ArgumentNullException(nameof(stateReader));
            if (animSettings == null) throw new System.ArgumentNullException(nameof(animSettings));

            if (_levelText == null)
            {
                throw new System.InvalidOperationException(
                    "LevelDisplay._levelText reference not assigned. Set in Inspector.");
            }

            _events = events;
            _animSettings = animSettings;

            UpdateText(stateReader.Score.Level);

            _events.LevelChanged += HandleLevelChanged;
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (_isInitialized)
            {
                _events.LevelChanged -= HandleLevelChanged;
            }

            _pulseTween?.Kill();
        }

        private void HandleLevelChanged(int level)
        {
            UpdateText(level);
            PlayPulse();
        }

        private void UpdateText(int level)
        {
            _levelText.text = level.ToString();
        }

        private void PlayPulse()
        {
            _pulseTween?.Kill();

            _levelText.transform.localScale = Vector3.one;

            _pulseTween = _levelText.transform
                .DOPunchScale(Vector3.one * _animSettings.PulseStrength, _animSettings.PulseDuration, vibrato: 1, elasticity: 0.5f)
                .SetUpdate(isIndependentUpdate: true);
        }
    }
}