using Tetris.Core.Events;
using Tetris.Core.Tetrominoes;
using Tetris.Presentation;
using Tetris.Presentation.Configs;
using UnityEngine;

namespace Tetris.Rendering
{
    public sealed class WaterDistortionEffectController : MonoBehaviour
    {
        private GameEvents _events;
        private FieldLayoutSettingsSO _layout;
        private bool _isInitialized;

        public void Initialize(GameEvents events, FieldLayoutSettingsSO layout)
        {
            if (_isInitialized) return;
            _events = events;
            _layout = layout;
            SubscribeToEvents();
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (_isInitialized) UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            _events.PieceMoved += HandlePieceMoved;
            _events.PieceRotated += HandlePieceRotated;
            _events.PieceHardDropped += HandlePieceHardDropped;
            _events.LayerClearing += HandleLayerClearing;
        }

        private void UnsubscribeFromEvents()
        {
            _events.PieceMoved -= HandlePieceMoved;
            _events.PieceRotated -= HandlePieceRotated;
            _events.PieceHardDropped -= HandlePieceHardDropped;
            _events.LayerClearing -= HandleLayerClearing;
        }

        private void HandlePieceMoved(Piece piece, Vector3Int direction)
        {
            if (WaterDistortionManager.Instance == null) return;
            var worldOrigin = piece.Position.GridToWorld(_layout);
            var directionBias = DirectionToScreenBias(direction);
            WaterDistortionManager.Instance.TriggerWave(WaveType.PieceMove, worldOrigin, directionBias);
        }

        private void HandlePieceRotated(Piece piece)
        {
            if (WaterDistortionManager.Instance == null) return;
            var worldOrigin = piece.Position.GridToWorld(_layout);
            WaterDistortionManager.Instance.TriggerWave(WaveType.PieceMove, worldOrigin, Vector2.zero);
        }

        private void HandlePieceHardDropped(Piece piece, int cellsDropped)
        {
            if (WaterDistortionManager.Instance == null) return;
            var worldOrigin = piece.Position.GridToWorld(_layout);
            WaterDistortionManager.Instance.TriggerWave(WaveType.PieceMove, worldOrigin, new Vector2(0f, -1f));
        }

        private void HandleLayerClearing(int[] clearingYs, TetrominoType type)
        {
            if (WaterDistortionManager.Instance == null) return;
            if (clearingYs == null || clearingYs.Length == 0) return;

            foreach (var y in clearingYs)
            {
                var rowCenter = new Vector3Int(0, y, 0).GridToWorld(_layout);
                WaterDistortionManager.Instance.TriggerWave(WaveType.LineClear, rowCenter, Vector2.zero);
            }
        }

        private static Vector2 DirectionToScreenBias(Vector3Int direction)
        {
            float x = direction.x;
            float y = direction.y + direction.z * 0.5f;
            var bias = new Vector2(x, y);
            return bias.sqrMagnitude > 0.01f ? bias.normalized : Vector2.zero;
        }
    }
}
