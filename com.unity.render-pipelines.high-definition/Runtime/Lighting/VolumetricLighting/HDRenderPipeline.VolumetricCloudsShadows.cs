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
                && GetMainLight() != null
                && settings.shadows.value);
        }

        bool HasVolumetricCloudsShadows(HDCamera hdCamera)
        {
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();
            return HasVolumetricCloudsShadows(hdCamera, settings);
        }

        bool HasVolumetricCloudsShadows_IgnoreSun(HDCamera hdCamera, in VolumetricClouds settings)
        {
            return (HasVolumetricClouds(hdCamera, in settings) && settings.shadows.value);
        }

        bool HasVolumetricCloudsShadows_IgnoreSun(HDCamera hdCamera)
        {
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();
            return HasVolumetricCloudsShadows_IgnoreSun(hdCamera, settings);
        }

        static void TraceVolumetricCloudShadow(CommandBuffer cmd, VolumetricCloudsParameters parameters, RTHandle shadowTexture)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.VolumetricCloudsShadow)))
            {
                CoreUtils.SetKeyword(cmd, "LOCAL_VOLUMETRIC_CLOUDS", parameters.localClouds);

                // Bind the constant buffer
                ConstantBuffer.Push(cmd, parameters.cloudsCB, parameters.volumetricCloudsCS, HDShaderIDs._ShaderVariablesClouds);

                // Compute the final resolution parameters
                int shadowTX = (parameters.cloudsCB._ShadowCookieResolution + (8 - 1)) / 8;
                int shadowTY = (parameters.cloudsCB._ShadowCookieResolution + (8 - 1)) / 8;
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.shadowsKernel, HDShaderIDs._CloudMapTexture, parameters.cloudMapTexture);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.shadowsKernel, HDShaderIDs._CloudLutTexture, parameters.cloudLutTexture);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.shadowsKernel, HDShaderIDs._Worley128RGBA, parameters.worley128RGBA);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.shadowsKernel, HDShaderIDs._ErosionNoise, parameters.erosionNoise);
                cmd.SetComputeTextureParam(parameters.volumetricCloudsCS, parameters.shadowsKernel, HDShaderIDs._VolumetricCloudsShadowRW, shadowTexture);
                cmd.DispatchCompute(parameters.volumetricCloudsCS, parameters.shadowsKernel, shadowTX, shadowTY, parameters.viewCount);

                // Bump the texture version
                shadowTexture.rt.IncrementUpdateCount();
            }
        }

        class VolumetricCloudsShadowData
        {
            public VolumetricCloudsParameters parameters;
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
            }

            if (m_VolumetricCloudsShadowTexture[shadowResIndex] == null)
            {
                m_VolumetricCloudsShadowTexture[shadowResIndex] = RTHandles.Alloc(shadowResolution, shadowResolution, 1, colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
                    enableRandomWrite: true, useDynamicScale: false, useMipMap: false, wrapMode: TextureWrapMode.Clamp, name: "Volumetric Clouds Shadow Texture");
            }

            return m_VolumetricCloudsShadowTexture[shadowResIndex];
        }

        CookieParameters RenderVolumetricCloudsShadows(CommandBuffer cmd, HDCamera hdCamera)
        {
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();

            // Make sure we should compute the shadow otherwise we return
            if (!HasVolumetricCloudsShadows(hdCamera, in settings))
                return default;

            // Make sure the shadow texture is the right size
            // TODO: Right now we can end up with a bunch of textures allocated which should be solved by an other PR.
            RTHandle currentHandle = RequestVolumetricCloudsShadowTexture(settings);

            // Evaluate and return the shadow
            var parameters = PrepareVolumetricCloudsParameters(hdCamera, settings, true, false);
            TraceVolumetricCloudShadow(cmd, parameters, currentHandle);

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
