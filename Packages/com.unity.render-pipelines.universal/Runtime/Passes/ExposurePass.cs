using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    internal class ExposurePass : ScriptableRenderPass
    {
        private static readonly string k_ExposureMultiplierTextureString = "_ExposureMultiplierTexture";
        internal static readonly int k_ExposureMultiplierTextureID = Shader.PropertyToID(k_ExposureMultiplierTextureString);

        class PassData
        {
            internal float exposureMultiplier;
            internal TextureHandle exposureMultiplierTexture;
        }

        private TextureHandle CreateTexture(RenderGraph renderGraph)
        {
            var exposureDescriptor = new TextureDesc(1, 1);
            exposureDescriptor.name = ExposurePass.k_ExposureMultiplierTextureString;
            exposureDescriptor.format = GraphicsFormat.R32G32_SFloat;
            exposureDescriptor.filterMode = FilterMode.Point;
            exposureDescriptor.wrapMode = TextureWrapMode.Clamp;
            exposureDescriptor.useMipMap = false;
            exposureDescriptor.autoGenerateMips = false;
            exposureDescriptor.bindTextureMS = false;
            exposureDescriptor.clearBuffer = false;
            return renderGraph.CreateTexture(exposureDescriptor);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!GraphicsSettings.TryGetRenderPipelineSettings<URPExposureSettings>(out var exposureSetting) || !exposureSetting.UseExposure)
                return;

            var exposureVolume = VolumeManager.instance.stack.GetComponent<ExposureVolume>();
            if (exposureVolume.mode.value == ExposureVolume.Mode.Fixed)
            {
                float exposureMultiplier = Mathf.Pow(2.0f, -exposureVolume.fixedExposure.value);
                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();

                resourceData.exposureMultiplier = CreateTexture(renderGraph);

                if (cameraData.cameraType == CameraType.Reflection)
                {
                    var probe = cameraData.camera.reflectionProbeRendered;
                    if (probe != null)
                    {
                        probe.exposureValue = exposureVolume.fixedExposure.value;
                    }
                }

                using (var builder = renderGraph.AddRasterRenderPass("Set Fixed Exposure", out PassData passData))
                {
                    passData.exposureMultiplier = exposureMultiplier;
                    passData.exposureMultiplierTexture = resourceData.exposureMultiplier;
                    builder.SetRenderAttachment(resourceData.exposureMultiplier, 0);
                    builder.SetRenderFunc<PassData>(static (data, context) =>
                    {
                        context.cmd.ClearRenderTarget(false, true, new Color(data.exposureMultiplier, 1 / data.exposureMultiplier, 0));
                    });
                    builder.SetGlobalTextureAfterPass(resourceData.exposureMultiplier, k_ExposureMultiplierTextureID);
                }
            }
        }
    }
}
