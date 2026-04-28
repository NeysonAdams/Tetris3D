using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace Tetris.Rendering
{
    public sealed class WaterDistortionRenderPass : ScriptableRenderPass
    {
        private const string PassName = "WaterDistortion";
        private readonly Material _material;

        public WaterDistortionRenderPass(Material material)
        {
            _material = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            requiresIntermediateTexture = true;
        }

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
            if (cameraData.cameraType == CameraType.Preview || cameraData.cameraType == CameraType.Reflection) return;

            var desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            var tempRT = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_WaterDistortion_Temp", false);

            var blitParams = new RenderGraphUtils.BlitMaterialParameters(resourceData.activeColorTexture, tempRT, _material, 0);
            renderGraph.AddBlitPass(blitParams, PassName + "_Distort");
            renderGraph.AddCopyPass(tempRT, resourceData.activeColorTexture, PassName + "_Copy");
        }
    }

    internal static class RenderGraphCopyExtensions
    {
        public static void AddCopyPass(this RenderGraph renderGraph, TextureHandle source, TextureHandle destination, string passName)
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

        private class CopyPassData { public TextureHandle Source; }
    }
}
