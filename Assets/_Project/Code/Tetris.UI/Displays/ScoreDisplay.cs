using DG.Tweening;
using Tetris.Core.Events;
using Tetris.Core.Scoring;
using Tetris.Core.StateMachine;
using Tetris.UI.Configs;
using TMPro;
using UnityEngine;

namespace Tetris.UI.Displays
{
    public sealed class ScoreDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _scoreText = default!;

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
                throw new System.InvalidOperationException("ScoreDisplay already initialized.");
            }

            if (events == null) throw new System.ArgumentNullException(nameof(events));
            if (stateReader == null) throw new System.ArgumentNullException(nameof(stateReader));
            if (animSettings == null) throw new System.ArgumentNullException(nameof(animSettings));

            if (_scoreText == null)
            {
                throw new System.InvalidOperationException(
                    "ScoreDisplay._scoreText reference not assigned. Set in Inspector.");
            }

            _events = events;
            _animSettings = animSettings;

            UpdateText(stateReader.Score.Score);

            _events.ScoreChnaged += HandleScoreChanged;
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (_isInitialized)
            {
                _events.ScoreChnaged -= HandleScoreChanged;
            }

            _pulseTween?.Kill();
        }

        private void HandleScoreChanged(ScoreState score)
        {
            UpdateText(score.Score);
            PlayPulse();
        }

        private void UpdateText(int score)
        {
            _scoreText.text = score.ToString("N0");
        }

        private void PlayPulse()
        {
            _pulseTween?.Kill();

            _scoreText.transform.localScale = Vector3.one;

            _pulseTween = _scoreText.transform
                .DOPunchScale(Vector3.one * _animSettings.PulseStrength, _animSettings.PulseDuration, vibrato: 1, elasticity: 0.5f)
                .SetUpdate(isIndependentUpdate: true);
        }
    }
}