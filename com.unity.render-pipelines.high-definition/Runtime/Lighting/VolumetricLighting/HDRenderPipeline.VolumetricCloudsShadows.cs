using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Cloud preset maps
        RTHandle[] m_VolumetricCloudsShadowTexture = new RTHandle[VolumetricClouds.CloudShadowResolutionCount];

        // The set of kernels that are required
        int m_ComputeShadowCloudsKernel;

        void InitializeVolumetricCloudsShadows()
        {
            // Grab the kernels we need
            ComputeShader volumetricCloudsCS = m_Asset.renderPipelineResources.shaders.volumetricCloudsCS;
            m_ComputeShadowCloudsKernel = volumetricCloudsCS.FindKernel("ComputeVolumetricCloudsShadow");
        }

        void ReleaseVolumetricCloudsShadows()
        {
            for (int i = 0; i < VolumetricClouds.CloudShadowResolutionCount; ++i)
            {
                RTHandles.Release(m_VolumetricCloudsShadowTexture[i]);
            }
        }

        bool HasVolumetricCloudsShadows(HDCamera hdCamera, in VolumetricClouds settings)
        {
            return (HasVolumetricClouds(hdCamera, in settings)
                && GetCurrentSunLight() != null
                && settings.shadows.value);
        }

        bool HasVolumetricCloudsShadows(HDCamera hdCamera)
        {
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();
            return HasVolumetricCloudsShadows(hdCamera, settings);
        }

        DirectionalLightData OverrideDirectionalLightData(HDCamera hdCamera, DirectionalLightData directionalLightData)
        {
            // Grab the current sun light
            Light sunLight = GetCurrentSunLight();

            // Compute the shadow size
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();
            float groundShadowSize = settings.shadowDistance.value * 2.0f;
            float scaleX = Mathf.Abs(Vector3.Dot(sunLight.transform.right, Vector3.Normalize(new Vector3(sunLight.transform.right.x, 0.0f, sunLight.transform.right.z))));
            float scaleY = Mathf.Abs(Vector3.Dot(sunLight.transform.up, Vector3.Normalize(new Vector3(sunLight.transform.up.x, 0.0f, sunLight.transform.up.z))));
            Vector2 shadowSize = new Vector2(groundShadowSize * scaleX, groundShadowSize * scaleY);

            // Override the parameters that we are interested in
            directionalLightData.right = sunLight.transform.right * 2 / Mathf.Max(shadowSize.x, 0.001f);
            directionalLightData.up = sunLight.transform.up * 2 / Mathf.Max(shadowSize.y, 0.001f);
            directionalLightData.positionRWS = Vector3.zero - new Vector3(0.0f, hdCamera.camera.transform.position.y - settings.shadowPlaneHeightOffset.value, 0.0f);

            // Return the overridden light data
            return directionalLightData;
        }

        static void TraceVolumetricCloudShadow(CommandBuffer cmd, VolumetricCloudsParameters parameters, RTHandle shadowTexture)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsShadow)))
            {
                // Bind the constant buffer
                ConstantBuffer.Push(cmd, parameters.cloudsCB, parameters.volumetricCloudsCS, HDShaderIDs._ShaderVariablesClouds);

                // Compute the final resolution parameters
                int shadowTX = (parameters.cloudsCB._ShadowCookieResolution + (8 - 1)) / 8;
                int shadowTY = (parameters.cloudsCB._ShadowCookieResolution + (8 - 1)) / 8;
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.shadowsKernel, HDShaderIDs._CloudMapTexture, parameters.cloudMapTexture);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.shadowsKernel, HDShaderIDs._CloudLutTexture, parameters.cloudLutTexture);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.shadowsKernel, HDShaderIDs._Worley128RGBA, parameters.worley128RGBA);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.shadowsKernel, HDShaderIDs._Worley32RGB, parameters.worley32RGB);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.shadowsKernel, HDShaderIDs._VolumetricCloudsShadowRW, shadowTexture);
                cmd.DispatchCompute(parameters.volumetricCloudsCS, parameters.shadowsKernel, shadowTX, shadowTY, parameters.viewCount);

                // Apply the texture to the sun
                shadowTexture.rt.IncrementUpdateCount();
                parameters.sunLight.cookie = shadowTexture;
            }
        }

        class VolumetricCloudsShadowData
        {
            public VolumetricCloudsParameters parameters;
            public TextureHandle shadowTexture;
        }
        

        RTHandle RequestShadowTexture(in VolumetricClouds settings)
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
            }

            if (m_VolumetricCloudsShadowTexture[shadowResIndex] == null)
            {
                m_VolumetricCloudsShadowTexture[shadowResIndex] = RTHandles.Alloc(shadowResolution, shadowResolution, 1, colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
                    enableRandomWrite: true, useDynamicScale: false, useMipMap: false, wrapMode: TextureWrapMode.Clamp, name: "Volumetric Clouds Shadow Texture");
            }

            return m_VolumetricCloudsShadowTexture[shadowResIndex];
        }

        void PreRenderVolumetricCloudsShadows(RenderGraph renderGraph, HDCamera hdCamera, HDUtils.PackedMipChainInfo info, in VolumetricClouds settings)
        {
            // Make sure we need to compute the shadow
            if (!HasVolumetricCloudsShadows(hdCamera, settings))
            {
                // We need to make sure that none of the textures that the component owns is assigned to the light
                Light currentSun = GetCurrentSunLight();
                if (currentSun != null)
                {
                    for (int i = 0; i < VolumetricClouds.CloudShadowResolutionCount; ++i)
                    {
                        if (currentSun.cookie == m_VolumetricCloudsShadowTexture[i])
                        {
                            currentSun.cookie = null;
                            break;
                        }
                    }
                }
                return;
            }

            // Make sure the shadow texture is the right size
            // TODO: Right now we can endup with a bunch of textures allocated which should be solved by an other PR.
            RTHandle currentHandle = RequestShadowTexture(settings);

            // Evaluate the shadow
            TextureHandle shadowHandle;
            using (var builder = renderGraph.AddRenderPass<VolumetricCloudsShadowData>("Volumetric cloud shadow", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricCloudsShadow)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = PrepareVolumetricCloudsParameters(hdCamera, settings, info, true, false);
                int shadowResolution = (int)settings.shadowResolution.value;
                passData.shadowTexture = builder.WriteTexture(renderGraph.ImportTexture(currentHandle));

                builder.SetRenderFunc(
                    (VolumetricCloudsShadowData data, RenderGraphContext ctx) =>
                    {
                        TraceVolumetricCloudShadow(ctx.cmd, data.parameters, data.shadowTexture);
                    });

                shadowHandle = passData.shadowTexture;
            }

            // Push the shadow
            PushFullScreenDebugTexture(m_RenderGraph, shadowHandle, FullScreenDebugMode.VolumetricCloudsShadow, xrTexture: false);
        }
    }
}
