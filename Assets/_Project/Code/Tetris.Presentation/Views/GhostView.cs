using System.Collections.Generic;
using DG.Tweening;
using Tetris.Core.Events;
using Tetris.Core.Tetrominoes;
using Tetris.Presentation.Configs;
using Tetris.Presentation.Pooling;
using UnityEngine;

namespace Tetris.Presentation.Views
{
    public class GhostView : MonoBehaviour
    {
        private GameEvents _events = default!;
        private BlockPool _pool = default!;
        private FieldLayoutSettingsSO _layout = default!;
        private DOTweenAnimationSettingsSO _animationSettings = default!;

        private readonly List<BlockView> _children = new();
        private Tween? _fadeTween;

        private bool _isInitialized;

        public void Initialize(
            GameEvents events,
            BlockPool pool,
            FieldLayoutSettingsSO layout,
            DOTweenAnimationSettingsSO animationSettings)
        {
            if (_isInitialized)
            {
                throw new System.InvalidOperationException("GhostView already initialized.");
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
            _events.GhostPieceChanged += HandleGhostPieceChanged;
            _events.PieceLocked += HandlePieceLocked;
        }

        private void UnsubscribeFromEvents()
        {
            _events.PieceSpawned -= HandlePieceSpawned;
            _events.GhostPieceChanged -= HandleGhostPieceChanged;
            _events.PieceLocked -= HandlePieceLocked;
        }

        private void HandlePieceSpawned(Piece piece)
        {
            if (piece.Shape == null)
            {
                return;
            }

            ReturnAllChildren();

            _fadeTween?.Kill();
            _fadeTween = null;

            SpawnChildren(piece);
        }

        private void HandleGhostPieceChanged(Piece ghost)
        {
            if (_children.Count == 0 || ghost.Shape == null)
            {
                return;
            } 

            transform.position = ghost.Position.GridToWorld(_layout);
            transform.rotation = Rotation3DToQuaternion(ghost.Rotation);
        }

        private void HandlePieceLocked(Piece piece, TetrominoType type)
        {
            _fadeTween?.Kill();
            _fadeTween = null;

            ReturnAllChildren();
        }

        private void SpawnChildren(Piece piece)
        {
            var color = piece.Shape.DefaultColor;
            var type = piece.Shape.Type;
            var cells = piece.Shape.Cells;
            var blockSize = _layout.BlockSize;

            for (var i = 0; i < cells.Length; i++)
            {
                var view = _pool.Rent(type, color);
                view.transform.SetParent(transform, worldPositionStays: false);
                view.transform.localPosition = new Vector3(cells[i].x, cells[i].y, cells[i].z) * blockSize;
                view.transform.localRotation = Quaternion.identity;
                view.SetGhostMode(true);

                _children.Add(view);
            }
        }

        private void ReturnAllChildren()
        {
            for (var i = 0; i < _children.Count; i++)
            {
                _pool.Return(_children[i]);
            }

            _children.Clear();
        }

        private static Quaternion Rotation3DToQuaternion(Rotation3D rotation)
        {
            var result = Quaternion.identity;
            result = Quaternion.AngleAxis(rotation.RxSteps * 90f, Vector3.right) * result;
            result = Quaternion.AngleAxis(rotation.RySteps * 90f, Vector3.up) * result;
            result = Quaternion.AngleAxis(rotation.RzSteps * 90f, Vector3.forward) * result;
            return result;
        }
    }
}
