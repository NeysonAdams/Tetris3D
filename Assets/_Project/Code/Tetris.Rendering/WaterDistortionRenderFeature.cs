// ============================================================================
// WaterDistortionRenderFeature.cs
// ----------------------------------------------------------------------------
// URP Renderer Feature that injects WaterDistortionRenderPass into the pipeline.
//
// Setup:
//   1. Add this feature to your URP Renderer asset.
//   2. Assign the WaterDistortion shader.
//   3. Add WaterDistortionManager to a scene GameObject (singleton manager).
//   4. From game logic, call WaterDistortionManager.Instance.TriggerWave(...).
//
// Compatibility: Unity 6 + URP 17+.
// ============================================================================

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TetrisAR.Rendering
{
    /// <summary>
    /// URP Renderer Feature that injects WaterDistortionRenderPass.
    /// </summary>
    [DisallowMultipleRendererFeature("Water Distortion")]
    public sealed class WaterDistortionRenderFeature : ScriptableRendererFeature
    {
        [Tooltip("Shader for water distortion. Must be Hidden/WaterDistortion.")]
        [SerializeField] private Shader _shader;

        private Material _material;
        private WaterDistortionRenderPass _pass;

        public override void Create()
        {
            if (_shader == null)
            {
                Debug.LogWarning("[WaterDistortionRenderFeature] Shader is not assigned.");
                return;
            }

            if (_material != null)
            {
                CoreUtils.Destroy(_material);
            }
            _material = CoreUtils.CreateEngineMaterial(_shader);
            _pass = new WaterDistortionRenderPass(_material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null || _material == null) return;

            var cameraType = renderingData.cameraData.cameraType;
            if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection) return;

            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            if (_material != null)
            {
                CoreUtils.Destroy(_material);
                _material = null;
            }
            _pass = null;
        }
    }
}
