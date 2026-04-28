using DG.Tweening;
using Tetris.Core.Events;
using Tetris.Core.Tetrominoes;
using Tetris.Presentation.Configs;
using Tetris.Presentation.Pooling;
using UnityEngine;

namespace Tetris.Presentation.Views
{
    public class PieceView : MonoBehaviour
    {
        private GameEvents _events = default!;
        private BlockPool _pool = default!;
        private FieldLayoutSettingsSO _layout = default!;
        private DOTweenAnimationSettingsSO _animationSettings = default!;

        private readonly System.Collections.Generic.List<BlockView> _children = new();
        private Tween? _moveTween;
        private Tween? _rotateTween;

        private bool _isInitialized;

        public void Initialize(
            GameEvents events,
            BlockPool pool,
            FieldLayoutSettingsSO layout,
            DOTweenAnimationSettingsSO animationSettings)
        {
            if (_isInitialized)
            {
                throw new System.InvalidOperationException("PieceView already initialized.");
            }

            if (events == null) throw new System.ArgumentNullException(nameof(events));
            if (pool == null) throw new System.ArgumentNullException(nameof(pool));
            if (layout == null) throw new System.ArgumentNullException(nameof(layout));
            if (animationSettings == null) throw new System.ArgumentNullException(nameof(animationSettings));

            _events = events;
            _pool = pool;
            _layout = layout;
            _animationSettings = animationSettings;

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

        private void SubscribeToEvents() 
        {
            _events.PieceSpawned += HandlePieceSpawned;
            _events.PieceMoved += HandlePieceMoved;
            _events.PieceFell += HandlePieceFell;
            _events.PieceHardDropped += HandlePieceHardDropped;
            _events.PieceRotated += HandlePieceRotated;
            _events.PieceLocked += HandlePieceLocked;
        }
        private void UnsubscribeFromEvents() 
        {
            _events.PieceSpawned -= HandlePieceSpawned;
            _events.PieceMoved -= HandlePieceMoved;
            _events.PieceFell -= HandlePieceFell;
            _events.PieceHardDropped -= HandlePieceHardDropped;
            _events.PieceRotated -= HandlePieceRotated;
            _events.PieceLocked -= HandlePieceLocked;
        }

        // Event handlers:
        private void HandlePieceSpawned(Piece piece) 
        {
            if (piece.Shape == null) return;

            ReturnAllChildren();

            _moveTween?.Kill();
            _rotateTween?.Kill();
            _moveTween = null;
            _rotateTween = null;

            transform.position = piece.Position.GridToWorld(_layout);
            transform.rotation = Rotation3DToQuaternion(piece.Rotation);

            SpawnChildren(piece);
        }
        private void HandlePieceMoved(Piece piece) 
        {
            MoveToPieceWorldPosition(piece, _animationSettings.PieceMoveDuration, _animationSettings.PieceMoveEase);
        }
        private void HandlePieceFell(Piece piece) 
        {
            MoveToPieceWorldPosition(piece, _animationSettings.PieceMoveDuration, _animationSettings.PieceMoveEase);
        }
        private void HandlePieceHardDropped(Piece piece, int cellsDropped) 
        {
            _moveTween?.Kill();
            _moveTween = null;

            transform.position = piece.Position.GridToWorld(_layout);
        }
        private void HandlePieceRotated(Piece piece) 
        {
            _rotateTween?.Kill();
            var targetRotation = Rotation3DToQuaternion(piece.Rotation);
            _rotateTween = transform
                .DORotateQuaternion(targetRotation, _animationSettings.PieceRotateDuration)
                .SetEase(_animationSettings.PieceRotateEase);
        }
        private void HandlePieceLocked(Piece piece, TetrominoType type) 
        {
            _moveTween?.Kill();
            _rotateTween?.Kill();
            _moveTween = null;
            _rotateTween = null;

            ReturnAllChildren();
        }

        // Helpers:
        private void ReturnAllChildren() 
        {
            for (var i = 0; i < _children.Count; i++)
            {
                _pool.Return(_children[i]);
            }

            _children.Clear();
        }
        private void SpawnChildren(Piece piece) 
        {
            var color = piece.Shape.DefaultColor;
            var type = piece.Shape.Type;
            var cells = piece.Shape.Cells;
            var blockSize = _layout.BlockSize;

            for (int i = 0; i < cells.Length; i++)
            {
                var view = _pool.Rent(type, color);
                view.transform.SetParent(transform, false);
                view.transform.localPosition = new Vector3(cells[i].x, cells[i].y, cells[i].z) * blockSize;
                view.transform.localRotation = Quaternion.identity;

                _children.Add(view);
            }
        }
        private void MoveToPieceWorldPosition(Piece piece, float duration, Ease ease) 
        {
            _moveTween?.Kill();

            var targetPos = piece.Position.GridToWorld(_layout);
            _moveTween = transform.DOMove(targetPos, duration).SetEase(ease);
        }
        private static Quaternion Rotation3DToQuaternion(Rotation3D rotation) => rotation.Quaternion;
    }
}