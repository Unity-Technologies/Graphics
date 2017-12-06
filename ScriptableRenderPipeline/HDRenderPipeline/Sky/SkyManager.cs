using UnityEngine.Rendering;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public enum SkyResolution
    {
        SkyResolution128 = 128,
        SkyResolution256 = 256,
        SkyResolution512 = 512,
        SkyResolution1024 = 1024,
        // TODO: Anything above 1024 cause a crash in Unity...
        //SkyResolution2048 = 2048,
        //SkyResolution4096 = 4096
    }

    public enum EnvironementUpdateMode
    {
        OnChanged = 0,
        OnDemand,
        Realtime
    }

    public class BuiltinSkyParameters
    {
        public Matrix4x4                pixelCoordToViewDirMatrix;
        public Matrix4x4                invViewProjMatrix;
        public Vector3                  cameraPosWS;
        public Vector4                  screenSize;
        public CommandBuffer            commandBuffer;
        public Light                    sunLight;
        public RenderTargetIdentifier   colorBuffer;
        public RenderTargetIdentifier   depthBuffer;

        public static RenderTargetIdentifier nullRT = -1;
    }

    public class SkyManager
    {
        RenderTexture           m_SkyboxCubemapRT;
        RenderTexture           m_SkyboxGGXCubemapRT;
        RenderTexture           m_SkyboxMarginalRowCdfRT;
        RenderTexture           m_SkyboxConditionalCdfRT;

        Material                m_StandardSkyboxMaterial; // This is the Unity standard skybox material. Used to pass the correct cubemap to Enlighten.
        Material                m_BlitCubemapMaterial;
        Material                m_OpaqueAtmScatteringMaterial;

        IBLFilterGGX            m_iblFilterGgx;

        Vector4                 m_CubemapScreenSize;
        Matrix4x4[]             m_facePixelCoordToViewDirMatrices   = new Matrix4x4[6];
        Matrix4x4[]             m_faceCameraInvViewProjectionMatrix = new Matrix4x4[6];

        BuiltinSkyParameters    m_BuiltinParameters = new BuiltinSkyParameters();
        SkyRenderer             m_Renderer;
        int                     m_SkyParametersHash = -1;
        bool                    m_NeedLowLevelUpdateEnvironment;
        int                     m_UpdatedFramesRequired = 2; // The first frame after the scene load is currently not rendered correctly
        float                   m_CurrentUpdateTime;
        int                     m_LastFrameUpdated = -1;

        bool                    m_useMIS = false;

        SkySettings m_SkySettings;
        public SkySettings skySettings
        {
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
                m_UpdatedFramesRequired = 2;

                if (value != null)
                {
                    m_Renderer = value.GetRenderer();
                    m_Renderer.Build();
                }
            }
            get { return m_SkySettings; }
        }

        public Texture skyReflection { get { return m_SkyboxGGXCubemapRT; } }

        void RebuildTextures(SkySettings skySettings)
        {
            int resolution = 256;

            // Parameters not set yet. We need them for the resolution.
            if (skySettings != null)
                resolution = (int)skySettings.resolution;

            if ((m_SkyboxCubemapRT != null) && (m_SkyboxCubemapRT.width != resolution))
            {
                CoreUtils.Destroy(m_SkyboxCubemapRT);
                CoreUtils.Destroy(m_SkyboxGGXCubemapRT);
                CoreUtils.Destroy(m_SkyboxMarginalRowCdfRT);
                CoreUtils.Destroy(m_SkyboxConditionalCdfRT);

                m_SkyboxCubemapRT = null;
                m_SkyboxGGXCubemapRT = null;
                m_SkyboxMarginalRowCdfRT = null;
                m_SkyboxConditionalCdfRT = null;
            }

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

                m_SkyboxGGXCubemapRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)
                {
                    dimension = TextureDimension.Cube,
                    useMipMap = true,
                    autoGenerateMips = false,
                    filterMode = FilterMode.Trilinear
                };
                m_SkyboxGGXCubemapRT.Create();

                if (m_useMIS)
                {
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

                m_UpdatedFramesRequired = 2; // Special case. Even if update mode is set to OnDemand, we need to regenerate the environment after destroying the texture.
                m_LastFrameUpdated = -1;
            }

            m_CubemapScreenSize = new Vector4((float)resolution, (float)resolution, 1.0f / (float)resolution, 1.0f / (float)resolution);
        }

        void RebuildSkyMatrices(float nearPlane, float farPlane)
        {
            if (!m_SkySettings) return;

            var cubeProj = Matrix4x4.Perspective(90.0f, 1.0f, nearPlane, farPlane);

            for (int i = 0; i < 6; ++i)
            {
                var lookAt      = Matrix4x4.LookAt(Vector3.zero, CoreUtils.lookAtList[i], CoreUtils.upVectorList[i]);
                var worldToView = lookAt * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)); // Need to scale -1.0 on Z to match what is being done in the camera.wolrdToCameraMatrix API. ...
                var screenSize  = new Vector4((int)m_SkySettings.resolution, (int)m_SkySettings.resolution, 1.0f / (int)m_SkySettings.resolution, 1.0f / (int)m_SkySettings.resolution);

                m_facePixelCoordToViewDirMatrices[i]   = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(0.5f * Mathf.PI, screenSize, worldToView, true);
                m_faceCameraInvViewProjectionMatrix[i] = HDUtils.GetViewProjectionMatrix(lookAt, cubeProj).inverse;
            }
        }

        // Sets the global MIP-mapped cubemap '_SkyTexture' in the shader.
        // The texture being set is the sky (environment) map pre-convolved with GGX.
        public void SetGlobalSkyTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._SkyTexture, m_SkyboxGGXCubemapRT);
            float mipCount = Mathf.Clamp(Mathf.Log((float)m_SkyboxGGXCubemapRT.width, 2.0f) + 1, 0.0f, 6.0f);
            cmd.SetGlobalFloat(HDShaderIDs._SkyTextureMipCount, mipCount);
        }

        public void Resize(float nearPlane, float farPlane)
        {
            // When loading RenderDoc, RenderTextures will go null
            RebuildTextures(skySettings);
            RebuildSkyMatrices(nearPlane, farPlane);
        }

        public void Build(RenderPipelineResources renderPipelinesResources, IBLFilterGGX iblFilterGGX)
        {
            m_iblFilterGgx = iblFilterGGX;

            // TODO: We need to have an API to send our sky information to Enlighten. For now use a workaround through skybox/cubemap material...
            m_StandardSkyboxMaterial = CoreUtils.CreateEngineMaterial(renderPipelinesResources.skyboxCubemap);

            m_BlitCubemapMaterial = CoreUtils.CreateEngineMaterial(renderPipelinesResources.blitCubemap);

            m_OpaqueAtmScatteringMaterial = CoreUtils.CreateEngineMaterial(renderPipelinesResources.opaqueAtmosphericScattering);

            m_CurrentUpdateTime = 0.0f;
        }

        public void Cleanup()
        {
            CoreUtils.Destroy(m_StandardSkyboxMaterial);
            CoreUtils.Destroy(m_SkyboxCubemapRT);
            CoreUtils.Destroy(m_SkyboxGGXCubemapRT);
            CoreUtils.Destroy(m_SkyboxMarginalRowCdfRT);
            CoreUtils.Destroy(m_SkyboxConditionalCdfRT);

            if (m_Renderer != null)
                m_Renderer.Cleanup();
        }

        public bool IsSkyValid()
        {
            return m_Renderer != null && m_Renderer.IsSkyValid();
        }

        void RenderSkyToCubemap(BuiltinSkyParameters builtinParams, SkySettings skySettings, RenderTexture target)
        {
            for (int i = 0; i < 6; ++i)
            {
                builtinParams.pixelCoordToViewDirMatrix = m_facePixelCoordToViewDirMatrices[i];
                builtinParams.invViewProjMatrix = m_faceCameraInvViewProjectionMatrix[i];
                builtinParams.colorBuffer = target;
                builtinParams.depthBuffer = BuiltinSkyParameters.nullRT;

                CoreUtils.SetRenderTarget(builtinParams.commandBuffer, target, ClearFlag.None, 0, (CubemapFace)i);
                m_Renderer.RenderSky(builtinParams, true);
            }

            // Generate mipmap for our cubemap
            Debug.Assert(target.autoGenerateMips == false);
            builtinParams.commandBuffer.GenerateMips(target);
        }

        void BlitCubemap(CommandBuffer cmd, Cubemap source, RenderTexture dest)
        {
            var propertyBlock = new MaterialPropertyBlock();

            for (int i = 0; i < 6; ++i)
            {
                CoreUtils.SetRenderTarget(cmd, dest, ClearFlag.None, 0, (CubemapFace)i);
                propertyBlock.SetTexture("_MainTex", source);
                propertyBlock.SetFloat("_faceIndex", (float)i);
                cmd.DrawProcedural(Matrix4x4.identity, m_BlitCubemapMaterial, 0, MeshTopology.Triangles, 3, 1, propertyBlock);
            }

            // Generate mipmap for our cubemap
            Debug.Assert(dest.autoGenerateMips == false);
            cmd.GenerateMips(dest);
        }

        void RenderCubemapGGXConvolution(CommandBuffer cmd, Texture input, RenderTexture target)
        {
            using (new ProfilingSample(cmd, "Update Env: GGX Convolution"))
            {
                if (m_useMIS && m_iblFilterGgx.supportMis)
                    m_iblFilterGgx.FilterCubemapMIS(cmd, input, target, m_SkyboxConditionalCdfRT, m_SkyboxMarginalRowCdfRT);
                else
                    m_iblFilterGgx.FilterCubemap(cmd, input, target);
            }
        }

        public void RequestEnvironmentUpdate()
        {
            m_UpdatedFramesRequired = Math.Max(m_UpdatedFramesRequired, 1);
        }

        public void UpdateEnvironment(HDCamera camera, Light sunLight, CommandBuffer cmd)
        {
            if (m_LastFrameUpdated == Time.frameCount)
                return;

            m_LastFrameUpdated = Time.frameCount;

            // We need one frame delay for this update to work since DynamicGI.UpdateEnvironment is executed directly but the renderloop is not (so we need to wait for the sky texture to be rendered first)
            if (m_NeedLowLevelUpdateEnvironment)
            {
                using (new ProfilingSample(cmd, "DynamicGI.UpdateEnvironment"))
                {
                    // TODO: Properly send the cubemap to Enlighten. Currently workaround is to set the cubemap in a Skybox/cubemap material
                    float intensity = IsSkyValid() ? 1.0f : 0.0f; // Eliminate all diffuse if we don't have a skybox (meaning for now the background is black in HDRP)
                    m_StandardSkyboxMaterial.SetTexture("_Tex", m_SkyboxCubemapRT);
                    RenderSettings.skybox = m_StandardSkyboxMaterial; // Setup this material as the default to be use in RenderSettings
                    RenderSettings.ambientIntensity = intensity;
                    RenderSettings.ambientMode = AmbientMode.Skybox; // Force skybox for our HDRI
                    RenderSettings.reflectionIntensity = intensity;
                    RenderSettings.customReflection = null;
                    DynamicGI.UpdateEnvironment();

                    m_NeedLowLevelUpdateEnvironment = false;
                }
            }

            if (IsSkyValid())
            {
                m_CurrentUpdateTime += Time.deltaTime;

                m_BuiltinParameters.commandBuffer = cmd;
                m_BuiltinParameters.sunLight = sunLight;
                m_BuiltinParameters.screenSize = m_CubemapScreenSize;
                m_BuiltinParameters.cameraPosWS = camera.camera.transform.position;

                int sunHash = 0;
                if(sunLight != null)
                    sunHash = (sunLight.GetHashCode() * 23 + sunLight.transform.position.GetHashCode()) * 23 + sunLight.transform.rotation.GetHashCode();
                int skyHash = sunHash * 23 + skySettings.GetHashCode();

                if (m_UpdatedFramesRequired > 0 ||
                    (skySettings.updateMode == EnvironementUpdateMode.OnChanged && skyHash != m_SkyParametersHash) ||
                    (skySettings.updateMode == EnvironementUpdateMode.Realtime && m_CurrentUpdateTime > skySettings.updatePeriod))
                {
                    using (new ProfilingSample(cmd, "Sky Environment Pass"))
                    {
                        using (new ProfilingSample(cmd, "Update Env: Generate Lighting Cubemap"))
                        {
                            // Render sky into a cubemap - doesn't happen every frame, can be controlled
                            // Note that m_SkyboxCubemapRT is created with auto-generate mipmap, it mean that here we have also our mipmap correctly box filtered for importance sampling.
                            if(m_SkySettings.lightingOverride == null)
                                RenderSkyToCubemap(m_BuiltinParameters, skySettings, m_SkyboxCubemapRT);
                            // In case the user overrides the lighting, we already have a cubemap ready but we need to blit it anyway for potential resize and so that we can generate proper mipmaps for enlighten.
                            else
                                BlitCubemap(cmd, m_SkySettings.lightingOverride, m_SkyboxCubemapRT);
                        }

                        // Convolve downsampled cubemap
                        RenderCubemapGGXConvolution(cmd, m_SkyboxCubemapRT, m_SkyboxGGXCubemapRT);

                        m_NeedLowLevelUpdateEnvironment = true;
                        m_UpdatedFramesRequired--;
                        m_SkyParametersHash = skyHash;
                        m_CurrentUpdateTime = 0.0f;
                        #if UNITY_EDITOR
                        // In the editor when we change the sky we want to make the GI dirty so when baking again the new sky is taken into account.
                        // Changing the hash of the rendertarget allow to say that GI is dirty
                        m_SkyboxCubemapRT.imageContentsHash = new Hash128((uint)skySettings.GetHashCode(), 0, 0, 0);
                        #endif
                    }
                }
            }
            else
            {
                if (m_SkyParametersHash != 0)
                {
                    using (new ProfilingSample(cmd, "Reset Sky Environment"))
                    {
                        // Clear temp cubemap and redo GGX from black and then feed it to enlighten for default light probe.
                        CoreUtils.ClearCubemap(cmd, m_SkyboxCubemapRT, Color.black);
                        RenderCubemapGGXConvolution(cmd, m_SkyboxCubemapRT, m_SkyboxGGXCubemapRT);

                        m_SkyParametersHash = 0;
                        m_NeedLowLevelUpdateEnvironment = true;
                    }
                }
            }
        }

        public void RenderSky(HDCamera camera, Light sunLight, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, CommandBuffer cmd, DebugDisplaySettings debugSettings)
        {
            using (new ProfilingSample(cmd, "Sky Pass"))
            {
                if (IsSkyValid())
                {
                    // Rendering the sky is the first time in the frame where we need fog parameters so we push them here for the whole frame.
                    m_SkySettings.atmosphericScatteringSettings.PushShaderParameters(cmd, debugSettings.renderingDebugSettings);

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

        public void RenderOpaqueAtmosphericScattering(CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "Opaque Atmospheric Scattering"))
            {
                if(skySettings != null && skySettings.atmosphericScatteringSettings.NeedFogRendering())
                {
                    CoreUtils.DrawFullScreen(cmd, m_OpaqueAtmScatteringMaterial);
                }
            }
        }

        public Texture2D ExportSkyToTexture()
        {
            if(m_Renderer == null)
            {
                Debug.LogError("Cannot export sky to a texture, no SkyRenderer is setup.");
                return null;
            }

            if(m_SkySettings == null)
            {
                Debug.LogError("Cannot export sky to a texture, no Sky settings are setup.");
                return null;
            }

            int resolution = (int)m_SkySettings.resolution;

            var tempRT = new RenderTexture(resolution * 6, resolution, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)
            {
                dimension = TextureDimension.Tex2D,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Trilinear
            };
            tempRT.Create();

            var temp = new Texture2D(resolution * 6, resolution, TextureFormat.RGBAFloat, false);
            var result = new Texture2D(resolution * 6, resolution, TextureFormat.RGBAFloat, false);

            // Note: We need to invert in Y the cubemap faces because the current sky cubemap is inverted (because it's a RT)
            // So to invert it again so that it's a proper cubemap image we need to do it in several steps because ReadPixels does not have scale parameters:
            // - Convert the cubemap into a 2D texture
            // - Blit and invert it to a temporary target.
            // - Read this target again into the result texture.
            int offset = 0;
            for (int i = 0; i < 6; ++i)
            {
                UnityEngine.Graphics.SetRenderTarget(m_SkyboxCubemapRT, 0, (CubemapFace)i);
                temp.ReadPixels(new Rect(0, 0, resolution, resolution), offset, 0);
                temp.Apply();
                offset += resolution;
            }

            // Flip texture.
            // Temporarily disabled until proper API reaches trunk
            UnityEngine.Graphics.Blit(temp, tempRT, new Vector2(1.0f, -1.0f), new Vector2(0.0f, 0.0f));

            result.ReadPixels(new Rect(0, 0, resolution * 6, resolution), 0, 0);
            result.Apply();

            UnityEngine.Graphics.SetRenderTarget(null);
            Object.DestroyImmediate(temp);
            Object.DestroyImmediate(tempRT);

            return result;
        }
    }
}
