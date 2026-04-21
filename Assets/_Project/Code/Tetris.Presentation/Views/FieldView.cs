using System.Collections.Generic;
using DG.Tweening;
using Tetris.Core.Events;
using Tetris.Core.Fields;
using Tetris.Core.Scoring;
using Tetris.Core.Tetrominoes;
using Tetris.Presentation.Configs;
using Tetris.Presentation.Pooling;
using UnityEngine;
using System;

namespace Tetris.Presentation.Views
{
    public sealed class FieldView : MonoBehaviour
    {
        [SerializeField] private Transform _blockParent = default;

        private GameEvents _events = default!;
        private BlockPool _pool = default!;
        private FieldLayoutSettingsSO _layout = default!;
        private DOTweenAnimationSettingsSO _animationSettings = default!;
        private IReadOnlyGameField _field = default!;

        private readonly Dictionary<Vector3Int, BlockView> _blocks = new();

        private int[] _pendingClearLayers = Array.Empty<int>();

        private bool _isInitialized;

        public void Initialize (
            GameEvents events,
            BlockPool pool,
            FieldLayoutSettingsSO layout,
            DOTweenAnimationSettingsSO animationSettings,
            IReadOnlyGameField field)
        {
            if (_isInitialized) throw new InvalidOperationException("FieldView already initialized.");

            if(events == null) throw new ArgumentNullException("events");
            if(pool == null) throw new ArgumentNullException(nameof(pool));
            if(layout == null) throw new ArgumentNullException(nameof (layout));
            if(animationSettings == null) throw new ArgumentNullException(nameof (animationSettings));
            if(field == null) throw new ArgumentNullException(nameof(field));

            if (_blockParent == null)
            {
                throw new System.InvalidOperationException(
                    "FieldView._blockParent is not assigned. Set it in Inspector or via prefab.");
            }

            _events = events;
            _pool = pool;
            _layout = layout;
            _animationSettings = animationSettings;
            _field = field;

            SubscribeToEvents();
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if(_isInitialized)
            {
                UnsubscribeFromEvents();
            }
        }

        //Event handlers

        private void HandleGameStarted()
        {
            ClearAllBlocks();
        }

        private void HandlePieceLocked(Piece piece, TetrominoType type)
        {
            if (piece.Shape == null) return;

            var color = piece.Shape.DefaultColor;
            var cells= piece.Shape.Cells;

            for (int i = 0; i < cells.Length; i++)
            {
                var rotated = RoatationMath.ApplyRotation(cells[i], piece.Rotation);
                var logical = piece.Position + rotated;

                if (logical.y >= _field.SizeY) continue;
                if(!_field.IsInside(logical)) continue;
                if(_blocks.ContainsKey(logical)) continue;

                var worldPos = logical.GridToWorld(_layout);

                var view = _pool.Rent(type, color);
                view.transform.SetParent(_blockParent, worldPositionStays: false);
                view.transform.position = worldPos;

                _blocks.Add(logical, view);
            }
        }

        private void HandleLayersClearing(int[] ys, TetrominoType type)
        {
            if (ys == null || ys.Length == 0) return;

            _pendingClearLayers = ys;

            foreach (var kvp in _blocks)
                if(IsInLayers(kvp.Key.y, ys))
                    kvp.Value.PlayClearFlash();
        }

        private void HandleLayersCleared(ScoreState score, int layersCount)
        {
            if(_pendingClearLayers == null || _pendingClearLayers.Length == 0) return;

            var clearedYs = _pendingClearLayers;
            _pendingClearLayers = Array.Empty<int>();

            DisposeClearedBlocks(clearedYs);

            AnimateSurvivingBlocksFall(clearedYs);
        }

        //helpers

        private void DisposeClearedBlocks(int[] clearedYs)
        {
            var toRemove = new List<Vector3Int>();

            foreach(var kvp in _blocks)
                if (IsInLayers(kvp.Key.y, clearedYs))
                    toRemove.Add(kvp.Key);

            var disposeDuration = _animationSettings.CascadeFallDuration * 0.5f;

            foreach(var key in toRemove)
            {
                var view = _blocks[key];
                _blocks.Remove(key);

                var capturedView = view;
                capturedView.transform.DOScale(Vector3.zero, disposeDuration)
                    .SetEase(Ease.InBack)
                    .OnComplete(()=>
                    {
                        if (capturedView != null) return;
                        _pool.Return(capturedView);
                    });
            }
        }

        private void AnimateSurvivingBlocksFall(int[] clearedYs)
        {
            var duration = _animationSettings.CascadeFallDuration;
            var ease = _animationSettings.CascadeFallEase;

            var updates = new List<(Vector3Int oldKey, Vector3Int newKey, BlockView view)>();

            foreach (var kvp in _blocks)
            {
                var oldKey = kvp.Key;
                var shift = CountLayersBelow(oldKey.y, clearedYs);
                if(shift == 0) continue;

                var newKey = new Vector3Int(oldKey.x, oldKey.y - shift, oldKey.z);
                updates.Add((oldKey, newKey, kvp.Value));
            }

            foreach (var (oldKey, newKey, view) in updates)
            {
                _blocks.Remove(oldKey);
                _blocks[newKey] = view;

                var newWorldPos = newKey.GridToWorld(_layout);
                view.transform.DOMove(newWorldPos, duration).SetEase(ease);
            }
        }

        private static int CountLayersBelow(int y, int[] layers)
        {
            var count = 0;
            for (var i = 0; i < layers.Length; i++)
            {
                if (layers[i] < y)
                {
                    count++;
                }
            }
            return count;
        }

        private void ClearAllBlocks()
        {
            foreach(var kvp in _blocks)
            {
                _pool.Return(kvp.Value);
            }

            _blocks.Clear();
        }
        private void SubscribeToEvents()
        {
            _events.GameStarted += HandleGameStarted;
            _events.PieceLocked += HandlePieceLocked;
            _events.LayerClearing += HandleLayersClearing;
            _events.LayerCleared += HandleLayersCleared;
        }
        private void UnsubscribeFromEvents() 
        {
            _events.GameStarted -= HandleGameStarted;
            _events.PieceLocked -= HandlePieceLocked;
            _events.LayerClearing -= HandleLayersClearing;
            _events.LayerCleared -= HandleLayersCleared;
        }

        private static bool IsInLayers(int y, int[] layers)
        {
            for (var i = 0; i < layers.Length; i++)
                if (layers[i] == y)
                    return true;

            return false;
        }
    }
}
