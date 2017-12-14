using UnityEngine.Rendering;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    internal class SkyRenderingContext
    {

        IBLFilterGGX            m_IBLFilterGGX;
        SkySettings             m_SkySettings;
        SkyRenderer             m_Renderer;
        RenderTexture           m_SkyboxCubemapRT;
        RenderTexture           m_SkyboxGGXCubemapRT;
        RenderTexture           m_SkyboxMarginalRowCdfRT;
        RenderTexture           m_SkyboxConditionalCdfRT;
        Vector4                 m_CubemapScreenSize;
        Matrix4x4[]             m_facePixelCoordToViewDirMatrices   = new Matrix4x4[6];
        Matrix4x4[]             m_faceCameraInvViewProjectionMatrix = new Matrix4x4[6];
        BuiltinSkyParameters    m_BuiltinParameters = new BuiltinSkyParameters();
        int                     m_SkyParametersHash = -1;
        float                   m_CurrentUpdateTime = 0.0f;
        int                     m_UpdatedFramesRequired = 1; // The first frame after the scene load is currently not rendered correctly
        bool                    m_SupportsConvolution = false;

        public RenderTexture cubemapRT { get { return m_SkyboxCubemapRT; } }
        public Texture reflectionTexture { get { return m_SkyboxGGXCubemapRT; } }

        public SkySettings skySettings
        {
            get { return m_SkySettings; }
            set
            {
                if (m_SkySettings == value)
                    return;

                if (m_Renderer != null)
                {
                    m_Renderer.Cleanup();
                    m_Renderer = null;
                }

                m_SkyParametersHash = -1;
                m_SkySettings = value;
                m_UpdatedFramesRequired = 1;

                if (value != null)
                {
                    m_Renderer = value.GetRenderer();
                    m_Renderer.Build();
                }
            }
        }

        public SkyRenderer renderer { get { return m_Renderer; } }

        public SkyRenderingContext(bool supportsConvolution, IBLFilterGGX filterGGX)
        {
            m_SupportsConvolution = supportsConvolution;
            m_IBLFilterGGX = filterGGX;
        }

        public void RebuildTextures()
        {
            int resolution = 256;
            bool useMIS = false;

            // Parameters not set yet. We need them for the resolution.
            if (skySettings != null)
            {
                resolution = (int)skySettings.resolution.value;
                useMIS = skySettings.useMIS;
            }

            bool updateNeeded = m_SkyboxCubemapRT == null || (m_SkyboxCubemapRT.width != resolution);

            // Cleanup first if needed
            if(updateNeeded)
            {
                CoreUtils.Destroy(m_SkyboxCubemapRT);
                CoreUtils.Destroy(m_SkyboxGGXCubemapRT);

                m_SkyboxCubemapRT = null;
                m_SkyboxGGXCubemapRT = null;
            }

            if (!useMIS && (m_SkyboxConditionalCdfRT != null))
            {
                CoreUtils.Destroy(m_SkyboxConditionalCdfRT);
                CoreUtils.Destroy(m_SkyboxMarginalRowCdfRT);

                m_SkyboxConditionalCdfRT = null;
                m_SkyboxMarginalRowCdfRT = null;
            }

            // Reallocate everything
            if (m_SkyboxCubemapRT == null)
            {
                m_SkyboxCubemapRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)
                {
                    dimension = TextureDimension.Cube,
                    useMipMap = true,
                    autoGenerateMips = false, // We will generate regular mipmap for filtered importance sampling manually
                    filterMode = FilterMode.Trilinear
                };
                m_SkyboxCubemapRT.Create();
            }

            if (m_SkyboxGGXCubemapRT == null && m_SupportsConvolution)
            {
                m_SkyboxGGXCubemapRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)
                {
                    dimension = TextureDimension.Cube,
                    useMipMap = true,
                    autoGenerateMips = false,
                    filterMode = FilterMode.Trilinear
                };
                m_SkyboxGGXCubemapRT.Create();
            }

            if (useMIS && (m_SkyboxConditionalCdfRT == null))
            {
                // Temporary, it should be dependent on the sky resolution
                int width  = (int)LightSamplingParameters.TextureWidth;
                int height = (int)LightSamplingParameters.TextureHeight;

                // + 1 because we store the value of the integral of the cubemap at the end of the texture.
                m_SkyboxMarginalRowCdfRT = new RenderTexture(height + 1, 1, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
                {
                    useMipMap = false,
                    autoGenerateMips = false,
                    enableRandomWrite = true,
                    filterMode = FilterMode.Point
                };
                m_SkyboxMarginalRowCdfRT.Create();

                // TODO: switch the format to R16 (once it's available) to save some bandwidth.
                m_SkyboxConditionalCdfRT = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
                {
                    useMipMap = false,
                    autoGenerateMips = false,
                    enableRandomWrite = true,
                    filterMode = FilterMode.Point
                };
                m_SkyboxConditionalCdfRT.Create();
            }

            m_CubemapScreenSize = new Vector4((float)resolution, (float)resolution, 1.0f / (float)resolution, 1.0f / (float)resolution);

            if(updateNeeded)
            {
                m_UpdatedFramesRequired = 1; // Special case. Even if update mode is set to OnDemand, we need to regenerate the environment after destroying the texture.
                RebuildSkyMatrices(resolution);
            }
        }

        public void RebuildSkyMatrices(int resolution)
        {
            var cubeProj = Matrix4x4.Perspective(90.0f, 1.0f, 0.01f, 1.0f);

            for (int i = 0; i < 6; ++i)
            {
                var lookAt      = Matrix4x4.LookAt(Vector3.zero, CoreUtils.lookAtList[i], CoreUtils.upVectorList[i]);
                var worldToView = lookAt * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)); // Need to scale -1.0 on Z to match what is being done in the camera.wolrdToCameraMatrix API. ...

                m_facePixelCoordToViewDirMatrices[i] = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(0.5f * Mathf.PI, m_CubemapScreenSize, worldToView, true);
                m_faceCameraInvViewProjectionMatrix[i] = HDUtils.GetViewProjectionMatrix(lookAt, cubeProj).inverse;
            }
        }

        public bool IsValid()
        {
            return m_Renderer != null && m_Renderer.IsValid();
        }

        public void Cleanup()
        {
            CoreUtils.Destroy(m_SkyboxCubemapRT);
            CoreUtils.Destroy(m_SkyboxGGXCubemapRT);
            CoreUtils.Destroy(m_SkyboxMarginalRowCdfRT);
            CoreUtils.Destroy(m_SkyboxConditionalCdfRT);

            if (m_Renderer != null)
                m_Renderer.Cleanup();
        }

        void RenderSkyToCubemap()
        {
            for (int i = 0; i < 6; ++i)
            {
                m_BuiltinParameters.pixelCoordToViewDirMatrix = m_facePixelCoordToViewDirMatrices[i];
                m_BuiltinParameters.invViewProjMatrix = m_faceCameraInvViewProjectionMatrix[i];
                m_BuiltinParameters.colorBuffer = m_SkyboxCubemapRT;
                m_BuiltinParameters.depthBuffer = BuiltinSkyParameters.nullRT;

                CoreUtils.SetRenderTarget(m_BuiltinParameters.commandBuffer, m_SkyboxCubemapRT, ClearFlag.None, 0, (CubemapFace)i);
                m_Renderer.RenderSky(m_BuiltinParameters, true);
            }

            // Generate mipmap for our cubemap
            Debug.Assert(m_SkyboxCubemapRT.autoGenerateMips == false);
            m_BuiltinParameters.commandBuffer.GenerateMips(m_SkyboxCubemapRT);
        }

        void RenderCubemapGGXConvolution()
        {
            using (new ProfilingSample(m_BuiltinParameters.commandBuffer, "Update Env: GGX Convolution"))
            {
                if (skySettings.useMIS)
                    m_IBLFilterGGX.FilterCubemapMIS(m_BuiltinParameters.commandBuffer, m_SkyboxCubemapRT, m_SkyboxGGXCubemapRT, m_SkyboxConditionalCdfRT, m_SkyboxMarginalRowCdfRT);
                else
                    m_IBLFilterGGX.FilterCubemap(m_BuiltinParameters.commandBuffer, m_SkyboxCubemapRT, m_SkyboxGGXCubemapRT);
            }
        }

        public bool UpdateEnvironment(HDCamera camera, Light sunLight, bool updateRequired, CommandBuffer cmd)
        {
            bool result = false;
            if (IsValid())
            {
                m_CurrentUpdateTime += Time.deltaTime;

                m_BuiltinParameters.commandBuffer = cmd;
                m_BuiltinParameters.sunLight = sunLight;
                m_BuiltinParameters.screenSize = m_CubemapScreenSize;
                m_BuiltinParameters.cameraPosWS = camera.camera.transform.position;

                int sunHash = 0;
                if (sunLight != null)
                    sunHash = (sunLight.GetHashCode() * 23 + sunLight.transform.position.GetHashCode()) * 23 + sunLight.transform.rotation.GetHashCode();
                int skyHash = sunHash * 23 + m_SkySettings.GetHashCode();

                bool forceUpdate = (updateRequired || m_UpdatedFramesRequired > 0);
                if (forceUpdate ||
                    (m_SkySettings.updateMode == EnvironementUpdateMode.OnChanged && skyHash != m_SkyParametersHash) ||
                    (m_SkySettings.updateMode == EnvironementUpdateMode.Realtime && m_CurrentUpdateTime > m_SkySettings.updatePeriod))
                {
                    using (new ProfilingSample(cmd, "Sky Environment Pass"))
                    {
                        using (new ProfilingSample(cmd, "Update Env: Generate Lighting Cubemap"))
                        {
                            RenderSkyToCubemap();
                        }

                        if(m_SupportsConvolution)
                        {
                            using (new ProfilingSample(cmd, "Update Env: Convolve Lighting Cubemap"))
                            {
                                RenderCubemapGGXConvolution();
                            }
                        }

                        result = true;
                        m_SkyParametersHash = skyHash;
                        m_CurrentUpdateTime = 0.0f;
                        m_UpdatedFramesRequired--;

                    #if UNITY_EDITOR
                        // In the editor when we change the sky we want to make the GI dirty so when baking again the new sky is taken into account.
                        // Changing the hash of the rendertarget allow to say that GI is dirty
                        m_SkyboxCubemapRT.imageContentsHash = new Hash128((uint)m_SkySettings.GetHashCode(), 0, 0, 0);
                    #endif
                    }
                }
            }
            else
            {
                if (m_SkyParametersHash != 0 && m_SupportsConvolution)
                {
                    using (new ProfilingSample(cmd, "Reset Sky Environment"))
                    {
                        CoreUtils.ClearCubemap(cmd, m_SkyboxGGXCubemapRT, Color.black, true);

                        m_SkyParametersHash = 0;
                        result = true;
                    }
                }
            }

            return result;
        }

        public void RenderSky(HDCamera camera, Light sunLight, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, CommandBuffer cmd)
        {
            if (IsValid())
            {
                using (new ProfilingSample(cmd, "Sky Pass"))
                {
                    m_BuiltinParameters.commandBuffer = cmd;
                    m_BuiltinParameters.sunLight = sunLight;
                    m_BuiltinParameters.pixelCoordToViewDirMatrix = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(camera.camera.fieldOfView * Mathf.Deg2Rad, camera.screenSize, camera.viewMatrix, false);
                    m_BuiltinParameters.invViewProjMatrix = camera.viewProjMatrix.inverse;
                    m_BuiltinParameters.screenSize = camera.screenSize;
                    m_BuiltinParameters.cameraPosWS = camera.camera.transform.position;
                    m_BuiltinParameters.colorBuffer = colorBuffer;
                    m_BuiltinParameters.depthBuffer = depthBuffer;

                    m_Renderer.SetRenderTargets(m_BuiltinParameters);
                    m_Renderer.RenderSky(m_BuiltinParameters, false);
                }
            }
        }
    }
}
