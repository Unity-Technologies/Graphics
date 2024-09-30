using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class VolumetricCloudsSystem
    {
        struct VolumetricCloudsShadowRegion
        {
            public bool valid;
            public float3 origin;
            public float3 dirX;
            public float3 dirY;
            public float3 lightDir;
            public float2 regionSize;
            public float fallbackValue;
        }

        // The set of kernels that are required
        ComputeShader m_VolumetricCloudsTraceShadowsCS;
        int m_TraceVolumetricCloudsShadowsKernel;
        ComputeShader m_VolumetricCloudsShadowFilterCS;
        int m_FilterShadowCloudsKernel;

        // Shadow Region for the current frame
        VolumetricCloudsShadowRegion m_VolumetricCloudsShadowRegion = new VolumetricCloudsShadowRegion();

        void InitializeVolumetricCloudsShadows()
        {
            // Grab the kernels we need
            m_VolumetricCloudsTraceShadowsCS = m_RuntimeResources.volumetricCloudsTraceShadowsCS;
            m_TraceVolumetricCloudsShadowsKernel = m_VolumetricCloudsTraceShadowsCS.FindKernel("TraceVolumetricCloudsShadows");
            m_VolumetricCloudsShadowFilterCS = m_RuntimeResources.volumetricCloudsShadowFilterCS;
            m_FilterShadowCloudsKernel = m_VolumetricCloudsShadowFilterCS.FindKernel("FilterVolumetricCloudsShadow");

            // Invalidate the shadow region
            m_VolumetricCloudsShadowRegion.valid = false;
        }

        bool HasVolumetricCloudsShadows(HDCamera hdCamera, in VolumetricClouds settings)
        {
            if (!HasVolumetricClouds(hdCamera, settings) || !settings.shadows.value)
                return false;

            var sunLight = m_RenderPipeline.GetMainLight();
            var additionalData = m_RenderPipeline.GetMainLightAdditionalData();
            return sunLight != null && sunLight.shadows != LightShadows.None && additionalData.shadowDimmer != 0.0f;
        }

        internal void EvaluateShadowRegionData(HDCamera hdCamera, CommandBuffer cmd)
        {
            // Invalidate the region in case something goes wrong
            m_VolumetricCloudsShadowRegion.valid = false;
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();

            // Grab the light and make sure it is valid
            if (!HasVolumetricCloudsShadows(hdCamera, in settings))
            {
                // Bind the invalid volumetric clouds shadow texture
                cmd.SetGlobalTexture(HDShaderIDs._VolumetricCloudsShadowsTexture, Texture2D.blackTexture);
                return;
            }

            // Grab the volume profile of the volumetric clouds
            Light targetLight = m_RenderPipeline.GetMainLight();
            Matrix4x4 wsToLSMat = targetLight.transform.worldToLocalMatrix;
            Matrix4x4 lsToWSMat = targetLight.transform.localToWorldMatrix;

            // Generate the light space bounds of the camera frustum
            Bounds lightSpaceBounds = new Bounds();
            lightSpaceBounds.SetMinMax(new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue));
            lightSpaceBounds.Encapsulate(wsToLSMat.MultiplyPoint(hdCamera.camera.transform.position));
            float perspectiveCorrectedShadowDistance = settings.shadowDistance.value / cos(hdCamera.camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            for (int cornerIdx = 0; cornerIdx < 4; ++cornerIdx)
            {
                Vector3 corner = hdCamera.frustum.corners[cornerIdx + 4];
                float diag = corner.magnitude;
                corner = corner / diag * Mathf.Min(perspectiveCorrectedShadowDistance, diag);
                Vector3 posLightSpace = wsToLSMat.MultiplyPoint(corner + hdCamera.camera.transform.position);
                lightSpaceBounds.Encapsulate(posLightSpace);
            }

            // If  ray tracing and extended shadow culling is enabled, let's extended the shadow area
            RayTracingSettings rtSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && rtSettings.extendShadowCulling.value)
            {
                for (int cornerIdx = 0; cornerIdx < 4; ++cornerIdx)
                {
                    Vector3 corner = hdCamera.frustum.corners[cornerIdx + 4];
                    float diag = corner.magnitude;
                    corner = corner / diag * Mathf.Min(perspectiveCorrectedShadowDistance, diag);
                    Vector3 posLightSpace = wsToLSMat.MultiplyPoint(-corner + hdCamera.camera.transform.position);
                    lightSpaceBounds.Encapsulate(posLightSpace);
                }
            }

            // Compute the four corners we need
            float3 c0 = lsToWSMat.MultiplyPoint(lightSpaceBounds.center + new Vector3(-lightSpaceBounds.extents.x, -lightSpaceBounds.extents.y, lightSpaceBounds.extents.z));
            float3 c1 = lsToWSMat.MultiplyPoint(lightSpaceBounds.center + new Vector3(lightSpaceBounds.extents.x, -lightSpaceBounds.extents.y, lightSpaceBounds.extents.z));
            float3 c2 = lsToWSMat.MultiplyPoint(lightSpaceBounds.center + new Vector3(-lightSpaceBounds.extents.x, lightSpaceBounds.extents.y, lightSpaceBounds.extents.z));

            // Evaluate the shadow region
            m_VolumetricCloudsShadowRegion.origin = c0;
            m_VolumetricCloudsShadowRegion.dirX = c1 - c0;
            m_VolumetricCloudsShadowRegion.dirY = c2 - c0;
            m_VolumetricCloudsShadowRegion.lightDir = -targetLight.transform.forward;
            m_VolumetricCloudsShadowRegion.regionSize = float2(length(m_VolumetricCloudsShadowRegion.dirX), length(m_VolumetricCloudsShadowRegion.dirY));
            m_VolumetricCloudsShadowRegion.fallbackValue = 1.0f - settings.shadowOpacityFallback.value;
            m_VolumetricCloudsShadowRegion.valid = true;
        }

        internal void UpdateShaderVariablesGlobalVolumetricClouds(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            // Volumetric Clouds Shadow Data
            cb._VolumetricCloudsShadowScale = m_VolumetricCloudsShadowRegion.regionSize;
            cb._VolumetricCloudsFallBackValue = m_VolumetricCloudsShadowRegion.fallbackValue;

            cb._VolumetricCloudsShadowOriginToggle = new Vector4(m_VolumetricCloudsShadowRegion.origin.x, m_VolumetricCloudsShadowRegion.origin.y, m_VolumetricCloudsShadowRegion.origin.z, m_VolumetricCloudsShadowRegion.valid ? 1 : 0);
            if (ShaderConfig.s_CameraRelativeRendering != 0)
                cb._VolumetricCloudsShadowOriginToggle -= new Vector4(hdCamera.camera.transform.position.x, hdCamera.camera.transform.position.y, hdCamera.camera.transform.position.z, 0);
        }

        void UpdateShaderVariablesCloudsShadow(ref ShaderVariablesCloudsShadows cb, HDCamera hdCamera, VolumetricClouds settings, VolumetricCloudsShadowRegion shadowRegion)
        {
            // Resolution of the cloud shadow
            cb._ShadowCookieResolution = (int)settings.shadowResolution.value;
            cb._ShadowIntensity = settings.shadowOpacity.value;
            cb._CloudShadowSunOrigin = float4(shadowRegion.origin - new float3(hdCamera.planet.center), 1);
            cb._CloudShadowSunRight = float4(shadowRegion.dirX, 0);
            cb._CloudShadowSunUp = float4(shadowRegion.dirY, 0);
            cb._CloudShadowSunForward = float4(shadowRegion.lightDir, 0);
            cb._CameraPositionPS = float4(hdCamera.mainViewConstants.worldSpaceCameraPos - hdCamera.planet.center, 0);
        }

        struct VolumetricCloudsShadowsParameters
        {
            // Data common to all volumetric cloud passes
            public VolumetricCloudCommonData commonData;
            public ComputeShader traceShadowsCS;
            public int shadowsKernel;
            public ComputeShader shadowFilterCS;
            public int filterShadowsKernel;
            public VolumetricCloudsShadowRegion shadowRegion;
            public ShaderVariablesCloudsShadows cloudsShadowCB;
        }

        VolumetricCloudsShadowsParameters PrepareVolumetricCloudsShadowsParameters(HDCamera hdCamera, VolumetricClouds settings)
        {
            VolumetricCloudsShadowsParameters parameters = new VolumetricCloudsShadowsParameters();

            // Compute the cloud model data
            CloudModelData cloudModelData = GetCloudModelData(settings);

            // CS & Kernels
            parameters.traceShadowsCS = m_VolumetricCloudsTraceShadowsCS;
            parameters.shadowsKernel = m_TraceVolumetricCloudsShadowsKernel;

            parameters.shadowFilterCS = m_VolumetricCloudsShadowFilterCS;
            parameters.filterShadowsKernel = m_FilterShadowCloudsKernel;

            // Shadow region
            parameters.shadowRegion = m_VolumetricCloudsShadowRegion;

            // Fill the common data
            FillVolumetricCloudsCommonData(hdCamera, false, settings, TVolumetricCloudsCameraType.Default, in cloudModelData, ref parameters.commonData);

            // Update the main constant buffer
            VolumetricCloudsCameraData cameraData;
            cameraData.cameraType = parameters.commonData.cameraType;
            cameraData.traceWidth = 1;
            cameraData.traceHeight = 1;
            cameraData.intermediateWidth = 1;
            cameraData.intermediateHeight = 1;
            cameraData.finalWidth = 1;
            cameraData.finalHeight = 1;
            cameraData.enableExposureControl = false;
            cameraData.lowResolution = false;
            cameraData.enableIntegration = false;
            UpdateShaderVariablesClouds(ref parameters.commonData.cloudsCB, hdCamera, settings, cameraData, cloudModelData, true);

            // Update the shadow constant buffer
            UpdateShaderVariablesCloudsShadow(ref parameters.cloudsShadowCB, hdCamera, settings, parameters.shadowRegion);

            return parameters;
        }

        static void TraceVolumetricCloudShadow(CommandBuffer cmd, VolumetricCloudsShadowsParameters parameters, RTHandle intermediateTexture, RTHandle shadowTexture)
        {
            CoreUtils.SetKeyword(cmd, "CLOUDS_SIMPLE_PRESET", parameters.commonData.simplePreset);

            // Bind the constant buffer for the trace CS
            ConstantBuffer.Push(cmd, parameters.commonData.cloudsCB, parameters.traceShadowsCS, HDShaderIDs._ShaderVariablesClouds);
            ConstantBuffer.Push(cmd, parameters.cloudsShadowCB, parameters.traceShadowsCS, HDShaderIDs._ShaderVariablesCloudsShadows);

            // Compute the number of tiles to dispatch
            int tileCount = (parameters.cloudsShadowCB._ShadowCookieResolution + 7) / 8;

            // Input textures
            cmd.SetComputeTextureParam(parameters.traceShadowsCS, parameters.shadowsKernel, HDShaderIDs._CloudMapTexture, parameters.commonData.cloudMapTexture);
            cmd.SetComputeTextureParam(parameters.traceShadowsCS, parameters.shadowsKernel, HDShaderIDs._CloudLutTexture, parameters.commonData.cloudLutTexture);
            cmd.SetComputeTextureParam(parameters.traceShadowsCS, parameters.shadowsKernel, HDShaderIDs._Worley128RGBA, parameters.commonData.worley128RGBA);
            cmd.SetComputeTextureParam(parameters.traceShadowsCS, parameters.shadowsKernel, HDShaderIDs._ErosionNoise, parameters.commonData.erosionNoise);

            // Output texture
            cmd.SetComputeTextureParam(parameters.traceShadowsCS, parameters.shadowsKernel, HDShaderIDs._VolumetricCloudsShadowRW, shadowTexture);

            // Evaluate the shadow
            cmd.DispatchCompute(parameters.traceShadowsCS, parameters.shadowsKernel, tileCount, tileCount, 1);

            // Bind the constant buffer for the other CS
            ConstantBuffer.Push(cmd, parameters.cloudsShadowCB, parameters.shadowFilterCS, HDShaderIDs._ShaderVariablesCloudsShadows);

            // Given the low number of steps available and the absence of noise in the integration, we try to reduce the artifacts by doing two consecutive 3x3 blur passes.
            cmd.SetComputeTextureParam(parameters.shadowFilterCS, parameters.filterShadowsKernel, HDShaderIDs._VolumetricCloudsShadow, shadowTexture);
            cmd.SetComputeTextureParam(parameters.shadowFilterCS, parameters.filterShadowsKernel, HDShaderIDs._VolumetricCloudsShadowRW, intermediateTexture);
            cmd.DispatchCompute(parameters.shadowFilterCS, parameters.filterShadowsKernel, tileCount, tileCount, 1);

            // Filter the shadow
            cmd.SetComputeTextureParam(parameters.shadowFilterCS, parameters.filterShadowsKernel, HDShaderIDs._VolumetricCloudsShadow, intermediateTexture);
            cmd.SetComputeTextureParam(parameters.shadowFilterCS, parameters.filterShadowsKernel, HDShaderIDs._VolumetricCloudsShadowRW, shadowTexture);
            cmd.DispatchCompute(parameters.shadowFilterCS, parameters.filterShadowsKernel, tileCount, tileCount, 1);
        }

        class VolumetricCloudsShadowsData
        {
            public VolumetricCloudsShadowsParameters parameters;
            public TextureHandle intermediateShadowTexture;
            public TextureHandle shadowTexture;
        }

        internal void RenderVolumetricCloudsShadows(RenderGraph renderGraph, HDCamera hdCamera, in VolumetricClouds settings)
        {
            // Make sure we should compute the shadow otherwise we return
            if (!HasVolumetricCloudsShadows(hdCamera, in settings))
                return;

            // Evaluate and bind the shadow
            int shadowResolution = (int)settings.shadowResolution.value;
            TextureHandle shadowTexture = renderGraph.CreateTexture(new TextureDesc(shadowResolution, shadowResolution, false, false)
            { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Volumetric Clouds Shadow Texture" });

            using (var builder = renderGraph.AddRenderPass<VolumetricCloudsShadowsData>("Volumetric Clouds Shadows", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricCloudsShadow)))
            {
                // Disable pass culling
                builder.AllowPassCulling(false);

                // Evaluate the parameters
                passData.parameters = PrepareVolumetricCloudsShadowsParameters(hdCamera, settings);

                // Manage the resources
                passData.intermediateShadowTexture = builder.CreateTransientTexture(new TextureDesc(shadowResolution, shadowResolution, false, false)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Volumetric Clouds Shadow Temp Texture" });
                passData.shadowTexture = builder.ReadWriteTexture(shadowTexture);

                // Evaluate the shadow
                builder.SetRenderFunc((VolumetricCloudsShadowsData data, RenderGraphContext ctx) =>
                {
                    TraceVolumetricCloudShadow(ctx.cmd, data.parameters, data.intermediateShadowTexture, data.shadowTexture);

                    // Bind the volumetric clouds shadow
                    ctx.cmd.SetGlobalTexture(HDShaderIDs._VolumetricCloudsShadowsTexture, data.shadowTexture);
                });
            }

            // Given that the rendering of the shadow happens before the render graph execution, we can only have the display debug here (and not during the light data build).
            m_RenderPipeline.PushFullScreenDebugTexture(renderGraph, shadowTexture, FullScreenDebugMode.VolumetricCloudsShadow, xrTexture: false);
        }
    }
}
