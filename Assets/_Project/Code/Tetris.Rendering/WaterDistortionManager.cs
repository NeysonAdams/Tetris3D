// ============================================================================
// WaterDistortionManager.cs
// ----------------------------------------------------------------------------
// Singleton manager for water distortion waves.
//
// Game systems trigger waves via simple API:
//   WaterDistortionManager.Instance.TriggerWave(WaveType.PieceMove, worldPos, direction);
//   WaterDistortionManager.Instance.TriggerWave(WaveType.LineClear, worldPos, Vector2.zero);
//
// The manager:
//   - Maintains up to MAX_WAVES (4) active waves simultaneously
//   - Updates each wave's elapsed time and amplitude (with decay curve)
//   - Projects world position to screen UV every frame for the camera
//   - Uploads wave parameters to the shader as global uniform arrays
//
// Wave parameters per type are configurable in Inspector via WaveSettings.
//
// Compatibility: Unity 6 + URP 17+.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace TetrisAR.Rendering
{
    /// <summary>
    /// Type of distortion wave. Each type has its own configurable settings.
    /// </summary>
    public enum WaveType
    {
        PieceMove,    // small, brief — when piece rotates / shifts
        LineClear     // large, dramatic — when a row clears
    }

    /// <summary>
    /// Tunable per-wave parameters. Surface in Inspector.
    /// </summary>
    [System.Serializable]
    public class WaveSettings
    {
        [Tooltip("Initial amplitude of the wave (UV-space offset magnitude).")]
        [Range(0f, 0.2f)] public float amplitude = 0.025f;

        [Tooltip("How fast the wavefront travels across screen UV per second.")]
        [Range(0.1f, 5f)] public float speed = 1.5f;

        [Tooltip("Distance between wave crests in UV space.")]
        [Range(0.05f, 1f)] public float wavelength = 0.25f;

        [Tooltip("How long the wave stays alive (seconds).")]
        [Range(0.1f, 2f)] public float duration = 0.4f;

        [Tooltip("Amplitude decay curve over wave lifetime. X=time fraction (0..1), Y=multiplier.")]
        public AnimationCurve decayCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    }

    /// <summary>
    /// Active wave instance — updated each frame.
    /// </summary>
    internal class ActiveWave
    {
        public Vector3 worldOrigin;
        public Vector2 directionBias; // (0,0) = radial, otherwise directional
        public float elapsedTime;
        public WaveSettings settings;
        public bool IsAlive => elapsedTime < settings.duration;
    }

    /// <summary>
    /// Singleton manager for water distortion waves.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WaterDistortionManager : MonoBehaviour
    {
        public static WaterDistortionManager Instance { get; private set; }

        private const int MAX_WAVES = 4;

        [Header("Camera")]
        [Tooltip("Camera used to project world positions to screen UV. Defaults to Camera.main.")]
        [SerializeField] private Camera _camera;

        [Header("Wave Presets")]
        [SerializeField] private WaveSettings _pieceMoveSettings = new WaveSettings
        {
            amplitude = 0.012f,
            speed = 2.0f,
            wavelength = 0.18f,
            duration = 0.25f
        };

        [SerializeField] private WaveSettings _lineClearSettings = new WaveSettings
        {
            amplitude = 0.04f,
            speed = 1.2f,
            wavelength = 0.3f,
            duration = 0.6f
        };

        // Shader uniform IDs (cached)
        private static readonly int WaveParamsAID = Shader.PropertyToID("_WaveParamsA");
        private static readonly int WaveParamsBID = Shader.PropertyToID("_WaveParamsB");

        private readonly List<ActiveWave> _activeWaves = new List<ActiveWave>(MAX_WAVES);
        private readonly Vector4[] _paramsA = new Vector4[MAX_WAVES];
        private readonly Vector4[] _paramsB = new Vector4[MAX_WAVES];

        // --------------------------------------------------------------------
        // LIFECYCLE
        // --------------------------------------------------------------------

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_camera == null) _camera = Camera.main;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // --------------------------------------------------------------------
        // PUBLIC API
        // --------------------------------------------------------------------

        /// <summary>
        /// Trigger a distortion wave. Game systems call this from movement or clear events.
        /// </summary>
        /// <param name="type">Wave type — selects preset settings.</param>
        /// <param name="worldOrigin">World-space epicenter of the wave.</param>
        /// <param name="directionBias">
        /// Direction bias in screen space. Vector2.zero = radial (best for line clear).
        /// Non-zero = directional (best for piece movement: pass movement delta as direction).
        /// </param>
        public void TriggerWave(WaveType type, Vector3 worldOrigin, Vector2 directionBias)
        {
            // Drop oldest wave if we're at capacity
            if (_activeWaves.Count >= MAX_WAVES)
            {
                _activeWaves.RemoveAt(0);
            }

            var wave = new ActiveWave
            {
                worldOrigin = worldOrigin,
                directionBias = directionBias.normalized * Mathf.Min(1f, directionBias.magnitude),
                elapsedTime = 0f,
                settings = GetSettings(type)
            };

            _activeWaves.Add(wave);
        }

        // --------------------------------------------------------------------
        // UPDATE
        // --------------------------------------------------------------------

        private void Update()
        {
            float dt = Time.deltaTime;

            // Advance time, remove dead waves
            for (int i = _activeWaves.Count - 1; i >= 0; i--)
            {
                _activeWaves[i].elapsedTime += dt;
                if (!_activeWaves[i].IsAlive)
                {
                    _activeWaves.RemoveAt(i);
                }
            }

            UploadShaderParameters();
        }

        // --------------------------------------------------------------------
        // SHADER UPLOAD
        // --------------------------------------------------------------------

        private void UploadShaderParameters()
        {
            // Initialize all slots as inactive (amplitude = 0)
            for (int i = 0; i < MAX_WAVES; i++)
            {
                _paramsA[i] = Vector4.zero;
                _paramsB[i] = Vector4.zero;
            }

            // Pack active waves
            int count = Mathf.Min(_activeWaves.Count, MAX_WAVES);
            for (int i = 0; i < count; i++)
            {
                var wave = _activeWaves[i];

                // Project world origin to screen UV
                Vector3 viewportPos = _camera != null
                    ? _camera.WorldToViewportPoint(wave.worldOrigin)
                    : new Vector3(0.5f, 0.5f, 1f);

                // If origin is behind camera, skip
                if (viewportPos.z <= 0f) continue;

                Vector2 originUV = new Vector2(viewportPos.x, viewportPos.y);

                // Compute current amplitude via decay curve
                float t = Mathf.Clamp01(wave.elapsedTime / wave.settings.duration);
                float decayMultiplier = wave.settings.decayCurve.Evaluate(t);
                float currentAmplitude = wave.settings.amplitude * decayMultiplier;

                // Pack into Vector4 arrays
                _paramsA[i] = new Vector4(originUV.x, originUV.y, currentAmplitude, wave.elapsedTime);
                _paramsB[i] = new Vector4(
                    wave.settings.speed,
                    wave.settings.wavelength,
                    wave.directionBias.x,
                    wave.directionBias.y
                );
            }

            Shader.SetGlobalVectorArray(WaveParamsAID, _paramsA);
            Shader.SetGlobalVectorArray(WaveParamsBID, _paramsB);
        }

        // --------------------------------------------------------------------
        // INTERNAL
        // --------------------------------------------------------------------

        private WaveSettings GetSettings(WaveType type)
        {
            switch (type)
            {
                case WaveType.PieceMove: return _pieceMoveSettings;
                case WaveType.LineClear: return _lineClearSettings;
                default: return _pieceMoveSettings;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Test: Trigger Piece Move Wave")]
        private void TestPieceMoveWave()
        {
            if (!Application.isPlaying) return;
            Vector3 origin = _camera != null
                ? _camera.transform.position + _camera.transform.forward * 5f
                : Vector3.zero;
            TriggerWave(WaveType.PieceMove, origin, Vector2.right);
        }

        [ContextMenu("Test: Trigger Line Clear Wave")]
        private void TestLineClearWave()
        {
            if (!Application.isPlaying) return;
            Vector3 origin = _camera != null
                ? _camera.transform.position + _camera.transform.forward * 5f
                : Vector3.zero;
            TriggerWave(WaveType.LineClear, origin, Vector2.zero);
        }
#endif
    }
}
