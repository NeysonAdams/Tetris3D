using System.Collections.Generic;
using UnityEngine;

namespace Tetris.Rendering
{
    public enum WaveType { PieceMove, LineClear }

    [System.Serializable]
    public class WaveSettings
    {
        [Range(0f, 0.2f)] public float amplitude = 0.025f;
        [Range(0.1f, 5f)] public float speed = 1.5f;
        [Range(0.05f, 1f)] public float wavelength = 0.25f;
        [Range(0.1f, 2f)] public float duration = 0.4f;
        public AnimationCurve decayCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    }

    internal class ActiveWave
    {
        public Vector3 worldOrigin;
        public Vector2 directionBias;
        public float elapsedTime;
        public WaveSettings settings;
        public bool IsAlive => elapsedTime < settings.duration;
    }

    [DisallowMultipleComponent]
    public sealed class WaterDistortionManager : MonoBehaviour
    {
        public static WaterDistortionManager Instance { get; private set; }

        private const int MAX_WAVES = 4;

        [SerializeField] private Camera _camera;

        [Header("Wave Presets")]
        [SerializeField] private WaveSettings _pieceMoveSettings = new WaveSettings
        {
            amplitude = 0.012f, speed = 2.0f, wavelength = 0.18f, duration = 0.25f
        };

        [SerializeField] private WaveSettings _lineClearSettings = new WaveSettings
        {
            amplitude = 0.04f, speed = 1.2f, wavelength = 0.3f, duration = 0.6f
        };

        private static readonly int WaveParamsAID = Shader.PropertyToID("_WaveParamsA");
        private static readonly int WaveParamsBID = Shader.PropertyToID("_WaveParamsB");

        private readonly List<ActiveWave> _activeWaves = new List<ActiveWave>(MAX_WAVES);
        private readonly Vector4[] _paramsA = new Vector4[MAX_WAVES];
        private readonly Vector4[] _paramsB = new Vector4[MAX_WAVES];

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

        public void TriggerWave(WaveType type, Vector3 worldOrigin, Vector2 directionBias)
        {
            if (_activeWaves.Count >= MAX_WAVES)
                _activeWaves.RemoveAt(0);

            _activeWaves.Add(new ActiveWave
            {
                worldOrigin = worldOrigin,
                directionBias = directionBias.normalized * Mathf.Min(1f, directionBias.magnitude),
                elapsedTime = 0f,
                settings = GetSettings(type)
            });
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            for (int i = _activeWaves.Count - 1; i >= 0; i--)
            {
                _activeWaves[i].elapsedTime += dt;
                if (!_activeWaves[i].IsAlive)
                    _activeWaves.RemoveAt(i);
            }

            UploadShaderParameters();
        }

        private void UploadShaderParameters()
        {
            for (int i = 0; i < MAX_WAVES; i++)
            {
                _paramsA[i] = Vector4.zero;
                _paramsB[i] = Vector4.zero;
            }

            int count = Mathf.Min(_activeWaves.Count, MAX_WAVES);
            for (int i = 0; i < count; i++)
            {
                var wave = _activeWaves[i];

                Vector3 viewportPos = _camera != null
                    ? _camera.WorldToViewportPoint(wave.worldOrigin)
                    : new Vector3(0.5f, 0.5f, 1f);

                if (viewportPos.z <= 0f) continue;

                Vector2 originUV = new Vector2(viewportPos.x, viewportPos.y);
                float t = Mathf.Clamp01(wave.elapsedTime / wave.settings.duration);
                float currentAmplitude = wave.settings.amplitude * wave.settings.decayCurve.Evaluate(t);

                _paramsA[i] = new Vector4(originUV.x, originUV.y, currentAmplitude, wave.elapsedTime);
                _paramsB[i] = new Vector4(wave.settings.speed, wave.settings.wavelength, wave.directionBias.x, wave.directionBias.y);
            }

            Shader.SetGlobalVectorArray(WaveParamsAID, _paramsA);
            Shader.SetGlobalVectorArray(WaveParamsBID, _paramsB);
        }

        private WaveSettings GetSettings(WaveType type) => type switch
        {
            WaveType.PieceMove => _pieceMoveSettings,
            WaveType.LineClear => _lineClearSettings,
            _ => _pieceMoveSettings
        };

#if UNITY_EDITOR
        [ContextMenu("Test: Piece Move Wave")]
        private void TestPieceMoveWave()
        {
            if (!Application.isPlaying) return;
            var origin = _camera != null ? _camera.transform.position + _camera.transform.forward * 5f : Vector3.zero;
            TriggerWave(WaveType.PieceMove, origin, Vector2.right);
        }

        [ContextMenu("Test: Line Clear Wave")]
        private void TestLineClearWave()
        {
            if (!Application.isPlaying) return;
            var origin = _camera != null ? _camera.transform.position + _camera.transform.forward * 5f : Vector3.zero;
            TriggerWave(WaveType.LineClear, origin, Vector2.zero);
        }
#endif
    }
}
