// ============================================================================
// WaterDistortionEffectController.cs
// ----------------------------------------------------------------------------
// Listens to game events and triggers water distortion waves.
// This bridges the game logic (events from Tetris.Core) with the rendering
// effect (WaterDistortionManager).
// ============================================================================

using Tetris.Core.Events;
using Tetris.Core.Tetrominoes;
using Tetris.Presentation;
using Tetris.Presentation.Configs;
using UnityEngine;

namespace TetrisAR.Rendering
{
    /// <summary>
    /// Subscribes to game events and triggers water distortion waves.
    /// Must be initialized with GameEvents and FieldLayoutSettingsSO.
    /// </summary>
    public sealed class WaterDistortionEffectController : MonoBehaviour
    {
        private GameEvents _events = default!;
        private FieldLayoutSettingsSO _layout = default!;

        private bool _isInitialized;

        public void Initialize(GameEvents events, FieldLayoutSettingsSO layout)
        {
            if (_isInitialized)
            {
                throw new System.InvalidOperationException(
                    "WaterDistortionEffectController already initialized.");
            }

            if (events == null) throw new System.ArgumentNullException(nameof(events));
            if (layout == null) throw new System.ArgumentNullException(nameof(layout));

            _events = events;
            _layout = layout;

            SubscribeToEvents();
            _isInitialized = true;

            Debug.Log("[WaterDistortion] EffectController initialized successfully.");
        }

        private void OnDestroy()
        {
            if (_isInitialized)
            {
                UnsubscribeFromEvents();
            }
        }

        // --------------------------------------------------------------------
        // EVENT SUBSCRIPTIONS
        // --------------------------------------------------------------------

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

        // --------------------------------------------------------------------
        // EVENT HANDLERS
        // --------------------------------------------------------------------

        private void HandlePieceMoved(Piece piece, Vector3Int direction)
        {
            if (WaterDistortionManager.Instance == null)
            {
                Debug.LogWarning("[WaterDistortion] Manager.Instance is null!");
                return;
            }

            var worldOrigin = piece.Position.GridToWorld(_layout);
            var directionBias = DirectionToScreenBias(direction);

            Debug.Log($"[WaterDistortion] PieceMoved: origin={worldOrigin}, dir={directionBias}");
            WaterDistortionManager.Instance.TriggerWave(
                WaveType.PieceMove,
                worldOrigin,
                directionBias
            );
        }

        private void HandlePieceRotated(Piece piece)
        {
            if (WaterDistortionManager.Instance == null)
            {
                Debug.LogWarning("[WaterDistortion] Manager.Instance is null!");
                return;
            }

            var worldOrigin = piece.Position.GridToWorld(_layout);

            Debug.Log($"[WaterDistortion] PieceRotated: origin={worldOrigin}");
            WaterDistortionManager.Instance.TriggerWave(
                WaveType.PieceMove,
                worldOrigin,
                Vector2.zero
            );
        }

        private void HandlePieceHardDropped(Piece piece, int cellsDropped)
        {
            if (WaterDistortionManager.Instance == null) return;

            var worldOrigin = piece.Position.GridToWorld(_layout);

            // Downward wave for hard drop
            WaterDistortionManager.Instance.TriggerWave(
                WaveType.PieceMove,
                worldOrigin,
                new Vector2(0f, -1f)
            );
        }

        private void HandleLayerClearing(int[] clearingYs, TetrominoType type)
        {
            if (WaterDistortionManager.Instance == null)
            {
                Debug.LogWarning("[WaterDistortion] Manager.Instance is null!");
                return;
            }
            if (clearingYs == null || clearingYs.Length == 0) return;

            Debug.Log($"[WaterDistortion] LayerClearing: {clearingYs.Length} layers");

            // Trigger a wave for each cleared layer
            foreach (var y in clearingYs)
            {
                var rowCenter = GetRowCenterWorldPosition(y);

                WaterDistortionManager.Instance.TriggerWave(
                    WaveType.LineClear,
                    rowCenter,
                    Vector2.zero  // Radial wave for line clear
                );
            }
        }

        // --------------------------------------------------------------------
        // HELPERS
        // --------------------------------------------------------------------

        /// <summary>
        /// Converts 3D grid movement direction to 2D screen-space bias.
        /// </summary>
        private static Vector2 DirectionToScreenBias(Vector3Int direction)
        {
            // Map X movement to screen X, Y movement to screen Y
            // Z movement (forward/back in 3D space) is mapped to Y as well
            // since the camera typically looks down at the field
            float x = direction.x;
            float y = direction.y + direction.z * 0.5f;

            var bias = new Vector2(x, y);
            return bias.sqrMagnitude > 0.01f ? bias.normalized : Vector2.zero;
        }

        /// <summary>
        /// Gets world position of the center of a row at given Y level.
        /// </summary>
        private Vector3 GetRowCenterWorldPosition(int y)
        {
            // Center of the playfield at the given Y level
            // Assuming X and Z are centered around 0
            var gridPos = new Vector3Int(0, y, 0);
            return gridPos.GridToWorld(_layout);
        }
    }
}
