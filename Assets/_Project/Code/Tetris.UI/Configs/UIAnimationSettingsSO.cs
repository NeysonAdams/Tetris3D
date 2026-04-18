using DG.Tweening;
using UnityEngine;

namespace Tetris.UI.Configs
{
    [CreateAssetMenu(
        fileName = "UIAnimationSettings",
        menuName = "Tetris/UI/UI Animation Settings",
        order = 60)]
    public sealed class UIAnimationSettingsSO : ScriptableObject
    {
        private const float MinDuration = 0.01f;

        [Header("Screen Transitions")]
        [SerializeField]
        private float _screenFadeDuration = 0.15f;

        [SerializeField]
        private Ease _screenFadeEase = Ease.OutQuad;

        [Header("Score / Level Pulse")]
        [SerializeField]
        private float _pulseDuration = 0.3f;

        [SerializeField]
        private float _pulseStrength = 0.25f;

        public float ScreenFadeDuration => _screenFadeDuration;
        public Ease ScreenFadeEase => _screenFadeEase;
        public float PulseDuration => _pulseDuration;
        public float PulseStrength => _pulseStrength;

        private void OnValidate()
        {
            _screenFadeDuration = Mathf.Max(MinDuration, _screenFadeDuration);
            _pulseDuration = Mathf.Max(MinDuration, _pulseDuration);
            _pulseStrength = Mathf.Max(0f, _pulseStrength);
        }
    }
}