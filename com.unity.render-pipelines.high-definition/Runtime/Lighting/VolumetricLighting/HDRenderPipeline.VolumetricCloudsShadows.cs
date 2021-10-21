using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Cloud preset maps
        RTHandle[] m_VolumetricCloudsShadowTexture = new RTHandle[VolumetricClouds.CloudShadowResolutionCount];
        RTHandle m_VolumetricCloudsIntermediateShadowTexture;

        // The set of kernels that are required
        int m_ComputeShadowCloudsKernel;
        int m_FilterShadowCloudsKernel;

        void InitializeVolumetricCloudsShadows()
        {
            // Grab the kernels we need
            ComputeShader volumetricCloudsCS = m_Asset.renderPipelineResources.shaders.volumetricCloudsCS;
            m_ComputeShadowCloudsKernel = volumetricCloudsCS.FindKernel("ComputeVolumetricCloudsShadow");
            m_FilterShadowCloudsKernel = volumetricCloudsCS.FindKernel("FilterVolumetricCloudsShadow");
        }

        void ReleaseVolumetricCloudsShadows()
        {
            for (int i = 0; i < VolumetricClouds.CloudShadowResolutionCount; ++i)
                RTHandles.Release(m_VolumetricCloudsShadowTexture[i]);
            RTHandles.Release(m_VolumetricCloudsIntermediateShadowTexture);
        }

        bool HasVolumetricCloudsShadows(HDCamera hdCamera, in VolumetricClouds settings)
        {
            return (HasVolumetricClouds(hdCamera, in settings)
                && GetMainLight() != null
                && settings.shadows.value);
        }

        bool HasVolumetricCloudsShadows(HDCamera hdCamera)
        {
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();
            return HasVolumetricCloudsShadows(hdCamera, settings);
        }

        internal bool HasVolumetricCloudsShadows_IgnoreSun(HDCamera hdCamera, in VolumetricClouds settings)
        {
            return (HasVolumetricClouds(hdCamera, in settings) && settings.shadows.value);
        }

        internal bool HasVolumetricCloudsShadows_IgnoreSun(HDCamera hdCamera)
        {
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();
            return HasVolumetricCloudsShadows_IgnoreSun(hdCamera, settings);
        }

        struct VolumetricCloudsShadowsParameters
        {
            // Data common to all volumetric cloud passes
            public VolumetricCloudCommonData commonData;
            public int shadowsKernel;
            public int filterShadowsKernel;
        }

        VolumetricCloudsShadowsParameters PrepareVolumetricCloudsShadowsParameters(HDCamera hdCamera, VolumetricClouds settings)
        {
            VolumetricCloudsShadowsParameters parameters = new VolumetricCloudsShadowsParameters();

            // Compute the cloud model data
            CloudModelData cloudModelData = GetCloudModelData(settings);

            // Fill the common data
            FillVolumetricCloudsCommonData(false, settings, TVolumetricCloudsCameraType.Default, in cloudModelData, ref parameters.commonData);

            parameters.shadowsKernel = m_ComputeShadowCloudsKernel;
            parameters.filterShadowsKernel = m_FilterShadowCloudsKernel;

            // Update the constant buffer
            VolumetricCloudsCameraData cameraData;
            cameraData.cameraType = parameters.commonData.cameraType;
            cameraData.traceWidth = 1;
            cameraData.traceHeight = 1;
            cameraData.intermediateWidth = 1;
            cameraData.intermediateHeight = 1;
            cameraData.finalWidth = 1;
            cameraData.finalHeight = 1;
            cameraData.viewCount = 1;
            cameraData.enableExposureControl = false;
            cameraData.lowResolution = false;
            cameraData.enableIntegration = false;
            UpdateShaderVariableslClouds(ref parameters.commonData.cloudsCB, hdCamera, settings, cameraData, cloudModelData, true);

            return parameters;
        }

        static void TraceVolumetricCloudShadow(CommandBuffer cmd, VolumetricCloudsShadowsParameters parameters, RTHandle intermediateTexture, RTHandle shadowTexture)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsShadow)))
            {
                CoreUtils.SetKeyword(cmd, "LOCAL_VOLUMETRIC_CLOUDS", parameters.commonData.localClouds);

                // Bind the constant buffer
                ConstantBuffer.Push(cmd, parameters.commonData.cloudsCB, parameters.commonData.volumetricCloudsCS, HDShaderIDs._ShaderVariablesClouds);

                // Compute the number of tiles to dispatch
                int shadowTX = (parameters.commonData.cloudsCB._ShadowCookieResolution + (8 - 1)) / 8;
                int shadowTY = (parameters.commonData.cloudsCB._ShadowCookieResolution + (8 - 1)) / 8;
                // Input textures
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.shadowsKernel, HDShaderIDs._CloudMapTexture, parameters.commonData.cloudMapTexture);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.shadowsKernel, HDShaderIDs._CloudLutTexture, parameters.commonData.cloudLutTexture);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.shadowsKernel, HDShaderIDs._Worley128RGBA, parameters.commonData.worley128RGBA);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.shadowsKernel, HDShaderIDs._ErosionNoise, parameters.commonData.erosionNoise);

                // Output texture
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.shadowsKernel, HDShaderIDs._VolumetricCloudsShadowRW, shadowTexture);

                // Evaluate the shadow
                cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, parameters.shadowsKernel, shadowTX, shadowTY, 1);

                // Given the low number of steps available and the absence of noise in the integration, we try to reduce the artifacts by doing two consecutive 3x3 blur passes.
                // Filter the shadow
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.filterShadowsKernel, HDShaderIDs._VolumetricCloudsShadow, shadowTexture);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.filterShadowsKernel, HDShaderIDs._VolumetricCloudsShadowRW, intermediateTexture);
                cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, parameters.filterShadowsKernel, shadowTX, shadowTY, 1);

                // Filter the shadow
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.filterShadowsKernel, HDShaderIDs._VolumetricCloudsShadow, intermediateTexture);
                cmd.SetComputeTextureParam(parameters.commonData.volumetricCloudsCS, parameters.filterShadowsKernel, HDShaderIDs._VolumetricCloudsShadowRW, shadowTexture);
                cmd.DispatchCompute(parameters.commonData.volumetricCloudsCS, parameters.filterShadowsKernel, shadowTX, shadowTY, 1);

                // Bump the texture version
                shadowTexture.rt.IncrementUpdateCount();
            }
        }

        class VolumetricCloudsShadowData
        {
            public VolumetricCloudsShadowsParameters parameters;
            public TextureHandle shadowTexture;
        }

        RTHandle RequestVolumetricCloudsShadowTexture(HDCamera hdCamera)
        {
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();
            return RequestVolumetricCloudsShadowTexture(in settings);
        }

        RTHandle RequestVolumetricCloudsShadowTexture(in VolumetricClouds settings)
        {
            int shadowResolution = (int)settings.shadowResolution.value;
            int shadowResIndex = 0;
            switch (settings.shadowResolution.value)
            {
                case VolumetricClouds.CloudShadowResolution.VeryLow64:
                    shadowResIndex = 0;
                    break;
                case VolumetricClouds.CloudShadowResolution.Low128:
                    shadowResIndex = 1;
                    break;
                case VolumetricClouds.CloudShadowResolution.Medium256:
                    shadowResIndex = 2;
                    break;
                case VolumetricClouds.CloudShadowResolution.High512:
                    shadowResIndex = 3;
                    break;
                case VolumetricClouds.CloudShadowResolution.Ultra1024:
                    shadowResIndex = 4;
                    break;
            }

            if (m_VolumetricCloudsShadowTexture[shadowResIndex] == null)
            {
                m_VolumetricCloudsShadowTexture[shadowResIndex] = RTHandles.Alloc(shadowResolution, shadowResolution, 1, colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
                    enableRandomWrite: true, useDynamicScale: false, useMipMap: false, filterMode: FilterMode.Bilinear, wrapMode: TextureWrapMode.Clamp, name: "Volumetric Clouds Shadow Texture");
            }

            return m_VolumetricCloudsShadowTexture[shadowResIndex];
        }

        internal CookieParameters RenderVolumetricCloudsShadows(CommandBuffer cmd, HDCamera hdCamera)
        {
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();

            // Make sure we should compute the shadow otherwise we return
            if (!HasVolumetricCloudsShadows(hdCamera, in settings))
                return default;

            // Make sure the shadow texture is the right size
            // TODO: Right now we can end up with a bunch of textures allocated which should be solved by an other PR.
            RTHandle currentHandle = RequestVolumetricCloudsShadowTexture(settings);

            // Check if the intermediate texture that we need for the filtering has already been allocated
            if (m_VolumetricCloudsIntermediateShadowTexture == null)
            {
                m_VolumetricCloudsIntermediateShadowTexture = RTHandles.Alloc((int)VolumetricClouds.CloudShadowResolution.Ultra1024, (int)VolumetricClouds.CloudShadowResolution.Ultra1024,
                        1, colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
                        enableRandomWrite: true, useDynamicScale: false, useMipMap: false,
                        wrapMode: TextureWrapMode.Clamp, name: "Intermediate Volumetric Clouds Shadow Texture");
            }

            // Evaluate and return the shadow
            var parameters = PrepareVolumetricCloudsShadowsParameters(hdCamera, settings);
            TraceVolumetricCloudShadow(cmd, parameters, m_VolumetricCloudsIntermediateShadowTexture, currentHandle);

            // Grab the current sun light
            Light sunLight = GetMainLight();

            // Compute the shadow size
            float groundShadowSize = settings.shadowDistance.value * 2.0f;
            float scaleX = Mathf.Abs(Vector3.Dot(sunLight.transform.right, Vector3.Normalize(new Vector3(sunLight.transform.right.x, 0.0f, sunLight.transform.right.z))));
            float scaleY = Mathf.Abs(Vector3.Dot(sunLight.transform.up, Vector3.Normalize(new Vector3(sunLight.transform.up.x, 0.0f, sunLight.transform.up.z))));
            Vector2 shadowSize = new Vector2(groundShadowSize * scaleX, groundShadowSize * scaleY);

            Vector3 positionWS = hdCamera.mainViewConstants.worldSpaceCameraPos;
            positionWS.y = settings.shadowPlaneHeightOffset.value;

            return new CookieParameters()
            {
                texture = currentHandle,
                size = shadowSize,
                position = positionWS
            };
        }
    }
}
