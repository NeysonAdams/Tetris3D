using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tetris.Rendering
{
    [DisallowMultipleRendererFeature("Water Distortion")]
    public sealed class WaterDistortionRenderFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader _shader;

        private Material _material;
        private WaterDistortionRenderPass _pass;

        public override void Create()
        {
            if (_shader == null) return;

            if (_material != null) CoreUtils.Destroy(_material);
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
