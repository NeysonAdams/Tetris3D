// ============================================================================
// WaterDistortionRenderPass.cs
// ----------------------------------------------------------------------------
// URP ScriptableRenderPass that applies WaterDistortion shader as a post-process
// blit. Reads camera color, distorts it via the shader, writes back.
//
// Wave parameters are uploaded as shader constants (uniform arrays) by
// WaterDistortionManager before this pass runs. The shader reads them.
//
// Compatibility: Unity 6 + URP 17+ RenderGraph.
// ============================================================================

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace TetrisAR.Rendering
{
    /// <summary>
    /// Performs screen-space water distortion via a custom HLSL post-process shader.
    /// </summary>
    public sealed class WaterDistortionRenderPass : ScriptableRenderPass
    {
        private const string PassName = "WaterDistortion";
        private static readonly ProfilingSampler Sampler = new ProfilingSampler(PassName);

        private readonly Material _material;

        public WaterDistortionRenderPass(Material material)
        {
            _material = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            requiresIntermediateTexture = true;
        }

        // ====================================================================
        // RENDER GRAPH PATH (Unity 6)
        // ====================================================================

        private class PassData
        {
            public Material Material;
            public TextureHandle Source;
            public TextureHandle Destination;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            if (!resourceData.activeColorTexture.IsValid()) return;
            if (cameraData.cameraType == CameraType.Preview ||
                cameraData.cameraType == CameraType.Reflection) return;

            // Allocate temp RT same size as camera color
            var desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            var tempRT = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "_WaterDistortion_Temp", false);

            // Pass 1: source -> tempRT (apply distortion)
            var blitParams = new RenderGraphUtils.BlitMaterialParameters(
                resourceData.activeColorTexture,
                tempRT,
                _material,
                0
            );
            renderGraph.AddBlitPass(blitParams, PassName + "_Distort");

            // Pass 2: tempRT -> source (copy back to camera color)
            var copyBackParams = new RenderGraphUtils.BlitMaterialParameters(
                tempRT,
                resourceData.activeColorTexture,
                _material,
                0
            );
            // Reuse same shader; second pass will distort again with same params
            // but since waves are radial offsets, double-distortion is acceptable
            // and creates richer effect. If undesired, use a passthrough shader here.
            // For cleaner result, copy back with a simple Blitter call:
            renderGraph.AddCopyPass(tempRT, resourceData.activeColorTexture, PassName + "_Copy");
        }
    }

    /// <summary>
    /// Helper extension for clean copy-back without shader effect.
    /// </summary>
    internal static class RenderGraphCopyExtensions
    {
        public static void AddCopyPass(this RenderGraph renderGraph,
            TextureHandle source, TextureHandle destination, string passName)
        {
            using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>(passName, out var data))
            {
                data.Source = source;
                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((CopyPassData d, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, d.Source, new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }

        private class CopyPassData
        {
            public TextureHandle Source;
        }
    }
}
