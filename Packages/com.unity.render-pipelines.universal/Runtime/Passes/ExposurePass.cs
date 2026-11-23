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
            var cameraData = frameData.Get<UniversalCameraData>();
            var probe = cameraData.cameraType == CameraType.Reflection ? cameraData.camera.reflectionProbeRendered : null;

            if (!GraphicsSettings.TryGetRenderPipelineSettings<URPExposureSettings>(out var exposureSetting) || !exposureSetting.UseExposure)
            {
                // This capture is not pre-exposed. Reset the probe's EV so it doesn't keep the value from an earlier
                // capture made with exposure enabled, which would be applied once exposure is enabled again.
                if (probe != null)
                    probe.exposureValue = 0f;
                return;
            }

            var exposureVolume = VolumeManager.instance.stack.GetComponent<ExposureVolume>();
            if (exposureVolume.mode.value == ExposureVolume.Mode.Fixed)
            {
                float fixedExposure = exposureVolume.GetClampedFixedExposure();
                float exposureMultiplier = Mathf.Pow(2.0f, -fixedExposure);
                var resourceData = frameData.Get<UniversalResourceData>();

                resourceData.exposureMultiplier = CreateTexture(renderGraph);

                if (probe != null)
                    probe.exposureValue = fixedExposure;

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
