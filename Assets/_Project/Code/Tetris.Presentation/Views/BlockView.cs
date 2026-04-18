using UnityEngine;
using DG.Tweening;
using Tetris.Core.Tetrominoes;
using Tetris.Presentation.Configs;
using System;

namespace Tetris.Presentation.Views
{
    [RequireComponent(typeof(MeshRenderer))]
    public class BlockView : MonoBehaviour
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

        private const float GhostAlpha = 0.25f;
        private const float NormalAlpha = 0.7f;

        [SerializeField] private MeshRenderer _renderer = default!;

        private MaterialPropertyBlock _propertyBlock = default!;
        private DOTweenAnimationSettingsSO _animationSettings = default!;

        private Tween? _flashTween;
        private Tween? _scaleTween;

        private bool _isInitialized;
        private bool _isGhostMode;

        public void Initialize(DOTweenAnimationSettingsSO animationSettings)
        {
            if (animationSettings == null)
                throw new ArgumentNullException(nameof(animationSettings));

            _animationSettings = animationSettings;
            _propertyBlock = new MaterialPropertyBlock();
            _isInitialized = true;
        }

        public void Bind(TetrominoType type, Color color, int layer = 0)
        {
            if (!_isInitialized)
            {
                throw new System.InvalidOperationException(
                    "BlockView.Bind called before Initialize. BlockPool must call Initialize in Create-callback.");
            }

            var t = transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;

            gameObject.layer = layer;

            _isGhostMode = false;
            ApplyColor(color, NormalAlpha);
        }

        public void SetGhostMode(bool isGhost)
        {
            if (_isGhostMode == isGhost) return;

            _isGhostMode = isGhost;

            _renderer.GetPropertyBlock(_propertyBlock);
            var current = _propertyBlock.GetColor(BaseColorProperty);
            var newAlpha = isGhost ? GhostAlpha : 1f;
            ApplyColor(current, newAlpha);
        }

        public void PlayLockFlash()
        {
            if (!_isInitialized) return;

            _flashTween?.Kill();
            _scaleTween?.Kill();

            var duration = _animationSettings.LockFlashDuration;

            _scaleTween = transform.DOPunchScale(Vector3.one * 0.15f, duration, vibrato: 1, elasticity: 0.5f);
        }

        public void PlayClearFlash()
        {
            if(!_isInitialized) return;

            _flashTween?.Kill();
            _scaleTween?.Kill();

            var duration = _animationSettings.ClearFlashDuration;

            _scaleTween = transform.DOScale(Vector3.one * 1.1f, duration * 0.25f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);

            _flashTween = DOVirtual.DelayedCall(duration, () =>
            {
                if (this == null) return;
                _scaleTween.Kill();
                transform.localScale = Vector3.one;
            });
        }

        public void OnDespawn(Transform poolContainer)
        {
            transform.SetParent(poolContainer, worldPositionStays: false);

            transform.DOKill(complete: false);
            _flashTween?.Kill();
            _scaleTween?.Kill();

            _flashTween = null;
            _scaleTween = null;

            transform.localScale = Vector3.zero;

            _isGhostMode = false;
        }

        private void OnValidate()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<MeshRenderer>();
            }
        }

        private void ApplyColor(Color color, float alpha)
        {
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorProperty, color);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
