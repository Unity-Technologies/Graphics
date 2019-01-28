using System;
using UnityEngine.Experimental.VoxelizedShadowMaps;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public class ScreenSpaceShadowComputePass : ScriptableRenderPass
    {
        private static class VxShadowMapConstantBuffer
        {
            public static int _WorldToShadow;
            public static int _ShadowData;
            public static int _CascadeShadowSplitSpheres0;
            public static int _CascadeShadowSplitSpheres1;
            public static int _CascadeShadowSplitSpheres2;
            public static int _CascadeShadowSplitSpheres3;
            public static int _CascadeShadowSplitSphereRadii;
            public static int _ShadowOffset0;
            public static int _ShadowOffset1;
            public static int _ShadowOffset2;
            public static int _ShadowOffset3;
            public static int _ShadowmapSize;

            public static int _InvViewProjMatrixID;
            public static int _ScreenSizeID;

            public static int _VoxelResolutionID;
            public static int _VoxelZBiasID;
            public static int _VoxelUpBiasID;
            public static int _MaxScaleID;
            public static int _WorldToShadowMatrixID;

            public static int _VxShadowMapBufferID;
            public static int _ScreenSpaceShadowOutputID;
        }

        static readonly int TileSize = 8;
        static readonly int TileAdditive = TileSize - 1;

        const string k_CollectShadowsTag = "Collect Shadows";
        RenderTextureFormat m_ColorFormat;

        public ScreenSpaceShadowComputePass()
        {
            VxShadowMapConstantBuffer._WorldToShadow = Shader.PropertyToID("_MainLightWorldToShadow");
            VxShadowMapConstantBuffer._ShadowData = Shader.PropertyToID("_MainLightShadowData");
            VxShadowMapConstantBuffer._CascadeShadowSplitSpheres0 = Shader.PropertyToID("_CascadeShadowSplitSpheres0");
            VxShadowMapConstantBuffer._CascadeShadowSplitSpheres1 = Shader.PropertyToID("_CascadeShadowSplitSpheres1");
            VxShadowMapConstantBuffer._CascadeShadowSplitSpheres2 = Shader.PropertyToID("_CascadeShadowSplitSpheres2");
            VxShadowMapConstantBuffer._CascadeShadowSplitSpheres3 = Shader.PropertyToID("_CascadeShadowSplitSpheres3");
            VxShadowMapConstantBuffer._CascadeShadowSplitSphereRadii = Shader.PropertyToID("_CascadeShadowSplitSphereRadii");
            VxShadowMapConstantBuffer._ShadowOffset0 = Shader.PropertyToID("_MainLightShadowOffset0");
            VxShadowMapConstantBuffer._ShadowOffset1 = Shader.PropertyToID("_MainLightShadowOffset1");
            VxShadowMapConstantBuffer._ShadowOffset2 = Shader.PropertyToID("_MainLightShadowOffset2");
            VxShadowMapConstantBuffer._ShadowOffset3 = Shader.PropertyToID("_MainLightShadowOffset3");
            VxShadowMapConstantBuffer._ShadowmapSize = Shader.PropertyToID("_MainLightShadowmapSize");

            VxShadowMapConstantBuffer._InvViewProjMatrixID = Shader.PropertyToID("_InvViewProjMatrix");
            VxShadowMapConstantBuffer._ScreenSizeID = Shader.PropertyToID("_ScreenSize");

            VxShadowMapConstantBuffer._VoxelResolutionID = Shader.PropertyToID("_VoxelResolution");
            VxShadowMapConstantBuffer._VoxelZBiasID = Shader.PropertyToID("_VoxelZBias");
            VxShadowMapConstantBuffer._VoxelUpBiasID = Shader.PropertyToID("_VoxelUpBias");
            VxShadowMapConstantBuffer._MaxScaleID = Shader.PropertyToID("_MaxScale");
            VxShadowMapConstantBuffer._WorldToShadowMatrixID = Shader.PropertyToID("_WorldToShadowMatrix");

            VxShadowMapConstantBuffer._VxShadowMapBufferID = Shader.PropertyToID("_VxShadowMapBuffer");
            VxShadowMapConstantBuffer._ScreenSpaceShadowOutputID = Shader.PropertyToID("_ScreenSpaceShadowOutput");

            bool R8_UNorm = SystemInfo.IsFormatSupported(GraphicsFormat.R8_UNorm, FormatUsage.LoadStore);
            bool R8_SNorm = SystemInfo.IsFormatSupported(GraphicsFormat.R8_SNorm, FormatUsage.LoadStore);
            bool R8_UInt  = SystemInfo.IsFormatSupported(GraphicsFormat.R8_UInt,  FormatUsage.LoadStore);
            bool R8_SInt  = SystemInfo.IsFormatSupported(GraphicsFormat.R8_SInt,  FormatUsage.LoadStore);
            
            bool R8 = R8_UNorm || R8_SNorm || R8_UInt || R8_SInt;
            
            m_ColorFormat = R8 ? RenderTextureFormat.R8 : RenderTextureFormat.RFloat;

            //Debug.Log("Screen Space Shadow Target format = " + m_ColorFormat);
        }

        private RenderTargetHandle colorAttachmentHandle { get; set; }
        private RenderTextureDescriptor descriptor { get; set; }
        private bool mainLightDynamicShadows = false;
        DirectionalVxShadowMap dirVxShadowMap;

        public void Setup(
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorAttachmentHandle,
            bool mainLightDynamicShadows)
        {
            this.colorAttachmentHandle = colorAttachmentHandle;

            baseDescriptor.autoGenerateMips = false;
            baseDescriptor.useMipMap = false;
            baseDescriptor.sRGB = false;
            baseDescriptor.depthBufferBits = 0;
            baseDescriptor.colorFormat = m_ColorFormat;
            baseDescriptor.enableRandomWrite = true;
            this.descriptor = baseDescriptor;
            this.mainLightDynamicShadows = mainLightDynamicShadows;
        }

        private int GetComputeShaderKernel(ref ComputeShader computeShader, ref ShadowData shadowData)
        {
            int kernel = -1;

            if (computeShader != null)
            {
                string blendModeName;
                if (mainLightDynamicShadows)
                    blendModeName = "BlendDynamicShadows";
                else
                    blendModeName = "NoBlend";

                string filteringName = "NoFilter";
                switch (shadowData.mainLightVxShadowQuality)
                {
                    case 1: filteringName = "Bilinear";  break;
                    case 2: filteringName = "Trilinear"; break;
                }

                string kernelName = blendModeName + filteringName;

                kernel = computeShader.FindKernel(kernelName);
            }

            return kernel;
        }

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var computeShader = renderer.GetComputeShader(ComputeShaderHandle.ScreenSpaceShadow);
            int kernel = GetComputeShaderKernel(ref computeShader, ref renderingData.shadowData);
            if (kernel == -1)
                return;

            var shadowLightIndex = renderingData.lightData.mainLightIndex;
            var shadowLight = renderingData.lightData.visibleLights[shadowLightIndex];

            var light = shadowLight.light;
            dirVxShadowMap = light.GetComponent<DirectionalVxShadowMap>();

            CommandBuffer cmd = CommandBufferPool.Get(k_CollectShadowsTag);

            cmd.GetTemporaryRT(colorAttachmentHandle.id, descriptor, FilterMode.Bilinear);

            if (mainLightDynamicShadows)
                SetupMainLightShadowReceiverConstants(cmd, kernel, ref computeShader, ref renderingData.shadowData, shadowLight);
            SetupVxShadowReceiverConstants(cmd, kernel, ref computeShader, ref renderingData.cameraData.camera, ref shadowLight);

            int x = (renderingData.cameraData.camera.pixelWidth + TileAdditive) / TileSize;
            int y = (renderingData.cameraData.camera.pixelHeight + TileAdditive) / TileSize;

            cmd.DispatchCompute(computeShader, kernel, x, y, 1);

            // even if the main light doesn't have dynamic shadows,
            // cascades keyword is needed for screen space shadow map texture in opaque rendering pass.
            if (mainLightDynamicShadows == false)
            {
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, true);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, true);
            }
            else
            {
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, true);
            }

            if (renderingData.cameraData.isStereoEnabled)
            {
                Camera camera = renderingData.cameraData.camera;
                context.StartMultiEye(camera);
                context.ExecuteCommandBuffer(cmd);
                context.StopMultiEye(camera);
            }
            else
            {
                context.ExecuteCommandBuffer(cmd);
            }
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            if (colorAttachmentHandle != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(colorAttachmentHandle.id);
                colorAttachmentHandle = RenderTargetHandle.CameraTarget;
            }
        }

        void SetupMainLightShadowReceiverConstants(CommandBuffer cmd, int kernel, ref ComputeShader computeShader, ref ShadowData shadowData, VisibleLight shadowLight)
        {
            float invShadowAtlasWidth = 1.0f / shadowData.mainLightShadowmapWidth;
            float invShadowAtlasHeight = 1.0f / shadowData.mainLightShadowmapHeight;
            float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
            float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;

            cmd.SetComputeMatrixArrayParam(computeShader, VxShadowMapConstantBuffer._WorldToShadow, dirVxShadowMap.cascadesMatrices);
            cmd.SetComputeVectorParam(computeShader, VxShadowMapConstantBuffer._CascadeShadowSplitSpheres0, dirVxShadowMap.cascadeSplitDistances[0]);
            cmd.SetComputeVectorParam(computeShader, VxShadowMapConstantBuffer._CascadeShadowSplitSpheres1, dirVxShadowMap.cascadeSplitDistances[1]);
            cmd.SetComputeVectorParam(computeShader, VxShadowMapConstantBuffer._CascadeShadowSplitSpheres2, dirVxShadowMap.cascadeSplitDistances[2]);
            cmd.SetComputeVectorParam(computeShader, VxShadowMapConstantBuffer._CascadeShadowSplitSpheres3, dirVxShadowMap.cascadeSplitDistances[3]);
            cmd.SetComputeVectorParam(computeShader, VxShadowMapConstantBuffer._CascadeShadowSplitSphereRadii, new Vector4(
                dirVxShadowMap.cascadeSplitDistances[0].w * dirVxShadowMap.cascadeSplitDistances[0].w,
                dirVxShadowMap.cascadeSplitDistances[1].w * dirVxShadowMap.cascadeSplitDistances[1].w,
                dirVxShadowMap.cascadeSplitDistances[2].w * dirVxShadowMap.cascadeSplitDistances[2].w,
                dirVxShadowMap.cascadeSplitDistances[3].w * dirVxShadowMap.cascadeSplitDistances[3].w));
            cmd.SetComputeVectorParam(computeShader, VxShadowMapConstantBuffer._ShadowOffset0, new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetComputeVectorParam(computeShader, VxShadowMapConstantBuffer._ShadowOffset1, new Vector4(invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetComputeVectorParam(computeShader, VxShadowMapConstantBuffer._ShadowOffset2, new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetComputeVectorParam(computeShader, VxShadowMapConstantBuffer._ShadowOffset3, new Vector4(invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetComputeVectorParam(computeShader, VxShadowMapConstantBuffer._ShadowmapSize, new Vector4(invShadowAtlasWidth, invShadowAtlasHeight,
                shadowData.mainLightShadowmapWidth, shadowData.mainLightShadowmapHeight));
        }

        void SetupVxShadowReceiverConstants(CommandBuffer cmd, int kernel, ref ComputeShader computeShader, ref Camera camera, ref VisibleLight shadowLight)
        {
            var light = shadowLight.light;

            float screenSizeX = (float)camera.pixelWidth;
            float screenSizeY = (float)camera.pixelHeight;
            float invScreenSizeX = 1.0f / screenSizeX;
            float invScreenSizeY = 1.0f / screenSizeY;

            var gpuView = camera.worldToCameraMatrix;
            var gpuProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);

            var viewMatrix = gpuView;
            var projMatrix = gpuProj;
            var viewProjMatrix = projMatrix * viewMatrix;

            float voxelUpBias = dirVxShadowMap.voxelUpBias * (dirVxShadowMap.volumeScale / dirVxShadowMap.voxelResolutionInt);

            cmd.SetComputeVectorParam(computeShader, VxShadowMapConstantBuffer._ShadowData, new Vector4(light.shadowStrength, 0.0f, 0.0f, 0.0f));

            cmd.SetComputeMatrixParam(computeShader, VxShadowMapConstantBuffer._InvViewProjMatrixID, viewProjMatrix.inverse);
            cmd.SetComputeVectorParam(computeShader, VxShadowMapConstantBuffer._ScreenSizeID, new Vector4(screenSizeX, screenSizeY, invScreenSizeX, invScreenSizeY));

            cmd.SetComputeIntParam(computeShader, VxShadowMapConstantBuffer._VoxelResolutionID, dirVxShadowMap.voxelResolutionInt);
            cmd.SetComputeIntParam(computeShader, VxShadowMapConstantBuffer._VoxelZBiasID, dirVxShadowMap.voxelZBias);
            cmd.SetComputeFloatParam(computeShader, VxShadowMapConstantBuffer._VoxelUpBiasID, voxelUpBias);
            cmd.SetComputeIntParam(computeShader, VxShadowMapConstantBuffer._MaxScaleID, dirVxShadowMap.maxScale);
            cmd.SetComputeMatrixParam(computeShader, VxShadowMapConstantBuffer._WorldToShadowMatrixID, dirVxShadowMap.worldToShadowMatrix);

            cmd.SetComputeBufferParam(computeShader, kernel, VxShadowMapConstantBuffer._VxShadowMapBufferID, dirVxShadowMap.computeBuffer);
            cmd.SetComputeTextureParam(computeShader, kernel, VxShadowMapConstantBuffer._ScreenSpaceShadowOutputID, colorAttachmentHandle.Identifier());
        }
    }
}
