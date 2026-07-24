#if VOLUMETRIC_FOG

using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using SharedIDs = UnityEngine.Rendering.Universal.VolumetricFogRendererFeature.ShaderIDs;

namespace UnityEngine.Rendering.Universal
{
    internal class VolumeVoxelizationPass : ScriptableRenderPass
    {
        static class ShaderIDs
        {
            public static readonly int _GlobalFogExtinction = Shader.PropertyToID("_GlobalFogExtinction");
            public static readonly int _GlobalFogScattering = Shader.PropertyToID("_GlobalFogScattering");
        }

        ComputeShader m_Shader;
        int m_Kernel;

        class PassData
        {
            public ComputeShader cs;
            public int kernel;

            public int vbufferW, vbufferH;
            public int sliceCount;
            public Vector4 vbufferViewportSize;
            public Matrix4x4 coordToViewDirWS;
            public Vector4 decodingParams;
            public Vector4 cameraPositionWS;
            public float cameraNearPlane;

            public Vector4 globalScattering;
            public float globalExtinction;

            public TextureHandle vbufferDensity;
        }

        public VolumeVoxelizationPass()
        {
            GraphicsSettings.TryGetRenderPipelineSettings<VolumetricFogResources>(out var resources);
            m_Shader = resources.volumeVoxelizationCS;
            if (m_Shader != null)
                m_Kernel = m_Shader.FindKernel("VolumeVoxelization");
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Shader == null)
                return;

            var cameraData = frameData.Get<UniversalCameraData>();
            var fogData = frameData.GetOrCreate<VolumetricFogFrameData>();

            if (!fogData.fogParamsValid)
                return;

            fogData.vBufferParams = new VBufferParams(cameraData.camera, cameraData.cameraTargetDescriptor, fogData.screenFraction, fogData.sliceCount, fogData.depthExtent, fogData.sliceDistributionUniformity);

            ref readonly var vBufferParams = ref fogData.vBufferParams;

            using (var builder = renderGraph.AddUnsafePass<PassData>("Volume Voxelization", out var passData))
            {
                passData.cs = m_Shader;
                passData.kernel = m_Kernel;

                // Uniforms
                passData.vbufferW = vBufferParams.vbufferW;
                passData.vbufferH = vBufferParams.vbufferH;
                passData.sliceCount = vBufferParams.sliceCount;
                passData.vbufferViewportSize = vBufferParams.viewportSize;
                passData.coordToViewDirWS = vBufferParams.coordToViewDirWS;
                passData.decodingParams = vBufferParams.decodingParams;
                passData.cameraPositionWS = cameraData.camera.transform.position;
                passData.cameraNearPlane = cameraData.camera.nearClipPlane;
                passData.globalScattering = fogData.globalScattering;
                passData.globalExtinction = fogData.globalExtinction;

                // Output texture
                passData.vbufferDensity = renderGraph.CreateTexture(new TextureDesc(vBufferParams.vbufferW, vBufferParams.vbufferH)
                {
                    format = GraphicsFormat.R16G16B16A16_SFloat,
                    dimension = TextureDimension.Tex3D,
                    slices = vBufferParams.sliceCount,
                    enableRandomWrite = true,
                    name = "VBuffer Density"
                });
                builder.UseTexture(passData.vbufferDensity, AccessFlags.Write);

                builder.SetRenderFunc(static (PassData data, UnsafeGraphContext ctx) =>
                {
                    var cmd = ctx.cmd;
                    var cs = data.cs;
                    int kernel = data.kernel;

                    cmd.SetComputeMatrixParam(cs, SharedIDs._VBufferCoordToViewDirWS, data.coordToViewDirWS);
                    cmd.SetComputeVectorParam(cs, SharedIDs._DepthDecodingParams, data.decodingParams);
                    cmd.SetComputeVectorParam(cs, SharedIDs._VBufferSize, data.vbufferViewportSize);
                    cmd.SetComputeVectorParam(cs, SharedIDs._CameraPositionWS, data.cameraPositionWS);
                    cmd.SetComputeVectorParam(cs, ShaderIDs._GlobalFogScattering, data.globalScattering);
                    cmd.SetComputeFloatParam(cs, ShaderIDs._GlobalFogExtinction, data.globalExtinction);
                    cmd.SetComputeFloatParam(cs, SharedIDs._CameraNearPlane, data.cameraNearPlane);
                    cmd.SetComputeIntParam(cs, SharedIDs._VBufferSliceCount, data.sliceCount);
                    cmd.SetComputeFloatParam(cs, SharedIDs._VBufferRcpSliceCount, 1.0f / data.sliceCount);

                    cmd.SetComputeTextureParam(cs, kernel, SharedIDs._VBufferDensity, data.vbufferDensity);

                    int dispatchX = CoreUtils.DivRoundUp(data.vbufferW, 8);
                    int dispatchY = CoreUtils.DivRoundUp(data.vbufferH, 8);
                    cmd.DispatchCompute(cs, kernel, dispatchX, dispatchY, 1);
                });

                fogData.vbufferDensity = passData.vbufferDensity;
            }
        }
    }
}

#endif
