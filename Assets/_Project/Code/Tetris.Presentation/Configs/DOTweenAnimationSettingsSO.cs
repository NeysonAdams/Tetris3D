using UnityEngine;
using DG.Tweening;

namespace Tetris.Presentation.Configs
{
    [CreateAssetMenu(
        fileName = "DOTweenAnimationSettings", 
        menuName = "Tetris/Presentation/DOTween Animation Settings",
        order = 51)]
    public sealed class DOTweenAnimationSettingsSO : ScriptableObject
    {
        private const float MinDuration = 0.01f;

        [Header("Piece - Movement")]
        [SerializeField]
        private float _pieceMoveDuration = 0.08f;

        [SerializeField]
        private Ease _pieceMoveEase = Ease.OutQuad;

        [Header("Piece — Rotation")]
        [SerializeField]
        private float _pieceRotateDuration = 0.15f;

        [SerializeField]
        private Ease _pieceRotateEase = Ease.OutBack;

        [Header("Piece - Hard Drop")]
        [SerializeField]
        private float _hardDropShakeStrength = 0.3f;

        [SerializeField]
        private float _hardDropShakeDuration = 0.2f;

        [Header("Block - Lock Flash")]
        [SerializeField]
        private float _lockFlashDuration = 0.2f;

        [Header("Block - Clear Flash")]
        [SerializeField]
        private float _clearFalshDuration = 0.4f;

        [Header("Cascade Fall")]
        [SerializeField]
        private float _cascadeFallDuration = 0.4f;

        [SerializeField]
        private Ease _cascadeEase = Ease.OutBounce;

        [Header("Ghost")]
        [SerializeField]
        private float _ghostFadeDuration = 0.1f;

        //Piece
        public float PieceMoveDuration => _pieceMoveDuration;
        public Ease PieceMoveEase => _pieceMoveEase;
        public float PieceRotateDuration => _pieceRotateDuration;
        public Ease PieceRotateEase => _pieceRotateEase;
        public float HardDropShakeStrength => _hardDropShakeStrength;
        public float HardDropShakeDuration => _hardDropShakeDuration;

        //Block
        public float LockFlashDuration => _lockFlashDuration;
        public float ClearFlashDuration => _clearFalshDuration;

        //Cascade
        public float CascadeFallDuration => _cascadeFallDuration;
        public Ease CascadeFallEase => _cascadeEase;

        //Ghost
        public float GhostFadeDuration => _ghostFadeDuration;

        private void OnValidate()
        {
            _pieceMoveDuration = Mathf.Max(MinDuration, _pieceMoveDuration);
            _pieceRotateDuration = Mathf.Max(MinDuration, _pieceRotateDuration);
            _hardDropShakeStrength = Mathf.Max(0f, _hardDropShakeStrength);
            _hardDropShakeDuration = Mathf.Max(MinDuration, _hardDropShakeDuration);
            _lockFlashDuration = Mathf.Max(MinDuration, _lockFlashDuration);
            _clearFalshDuration = Mathf.Max(MinDuration, _clearFalshDuration);
            _cascadeFallDuration = Mathf.Max(MinDuration, _cascadeFallDuration);
            _ghostFadeDuration = Mathf.Max(0f, _ghostFadeDuration);
        }
    }
}
