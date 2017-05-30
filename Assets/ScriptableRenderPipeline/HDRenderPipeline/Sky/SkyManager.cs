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

    [GenerateHLSL(PackingRules.Exact)]
    public enum LightSamplingParameters
    {
        TextureHeight = 256,
        TextureWidth  = 512
    }

    public enum EnvironementUpdateMode
    {
        OnChanged = 0,
        OnDemand,
        Realtime
    }

    public class BuiltinSkyParameters
    {
        public Matrix4x4                invViewProjMatrix;
        public Vector3                  cameraPosWS;
        public Vector4                  screenSize;
        public Mesh                     skyMesh;
        public ScriptableRenderContext  renderContext;
        public Light                    sunLight;
        public RenderTargetIdentifier   colorBuffer;
        public RenderTargetIdentifier   depthBuffer;

        public static RenderTargetIdentifier nullRT = -1;
    }

    public class SkyManager
    {
        RenderTexture           m_SkyboxCubemapRT = null;
        RenderTexture           m_SkyboxGGXCubemapRT = null;
        RenderTexture           m_SkyboxMarginalRowCdfRT = null;
        RenderTexture           m_SkyboxConditionalCdfRT = null;

        Material                m_StandardSkyboxMaterial = null; // This is the Unity standard skybox material. Used to pass the correct cubemap to Enlighten.
        Material                m_BlitCubemapMaterial = null;

        IBLFilterGGX            m_iblFilterGgx = null;

        Vector4                 m_CubemapScreenSize;
        Matrix4x4[]             m_faceCameraViewProjectionMatrix = new Matrix4x4[6];
        Matrix4x4[]             m_faceCameraInvViewProjectionMatrix = new Matrix4x4[6];
        Mesh[]                  m_CubemapFaceMesh = new Mesh[6];

        BuiltinSkyParameters    m_BuiltinParameters = new BuiltinSkyParameters();
        SkyRenderer             m_Renderer = null;
        int                     m_SkyParametersHash = -1;
        bool                    m_NeedLowLevelUpdateEnvironment = false;
        int                     m_UpdatedFramesRequired = 2; // The first frame after the scene load is currently not rendered correctly
        float                   m_CurrentUpdateTime = 0.0f;

        bool                    m_useMIS = false;


        private SkySettings m_SkySettings;
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

        protected Mesh BuildSkyMesh(Vector3 cameraPosition, Matrix4x4 cameraInvViewProjectionMatrix, bool forceUVBottom)
        {
            Vector4 vertData0 = new Vector4(-1.0f, -1.0f, 1.0f, 1.0f);
            Vector4 vertData1 = new Vector4(1.0f, -1.0f, 1.0f, 1.0f);
            Vector4 vertData2 = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            Vector4 vertData3 = new Vector4(-1.0f, 1.0f, 1.0f, 1.0f);

            Vector3[] vertData = new Vector3[4];
            vertData[0] = new Vector3(vertData0.x, vertData0.y, vertData0.z);
            vertData[1] = new Vector3(vertData1.x, vertData1.y, vertData1.z);
            vertData[2] = new Vector3(vertData2.x, vertData2.y, vertData2.z);
            vertData[3] = new Vector3(vertData3.x, vertData3.y, vertData3.z);

            // Get view vector based on the frustum, i.e (invert transform frustum get position etc...)
            Vector3[] eyeVectorData = new Vector3[4];

            Matrix4x4 transformMatrix = cameraInvViewProjectionMatrix;

            Vector4 posWorldSpace0 = transformMatrix * vertData0;
            Vector4 posWorldSpace1 = transformMatrix * vertData1;
            Vector4 posWorldSpace2 = transformMatrix * vertData2;
            Vector4 posWorldSpace3 = transformMatrix * vertData3;

            Vector4 cameraPos = new Vector4(cameraPosition.x, cameraPosition.y, cameraPosition.z, 0.0f);

            Vector4 direction0 = (posWorldSpace0 / posWorldSpace0.w - cameraPos);
            Vector4 direction1 = (posWorldSpace1 / posWorldSpace1.w - cameraPos);
            Vector4 direction2 = (posWorldSpace2 / posWorldSpace2.w - cameraPos);
            Vector4 direction3 = (posWorldSpace3 / posWorldSpace3.w - cameraPos);

            if (SystemInfo.graphicsUVStartsAtTop && !forceUVBottom)
            {
                eyeVectorData[3] = new Vector3(direction0.x, direction0.y, direction0.z).normalized;
                eyeVectorData[2] = new Vector3(direction1.x, direction1.y, direction1.z).normalized;
                eyeVectorData[1] = new Vector3(direction2.x, direction2.y, direction2.z).normalized;
                eyeVectorData[0] = new Vector3(direction3.x, direction3.y, direction3.z).normalized;
            }
            else
            {
                eyeVectorData[0] = new Vector3(direction0.x, direction0.y, direction0.z).normalized;
                eyeVectorData[1] = new Vector3(direction1.x, direction1.y, direction1.z).normalized;
                eyeVectorData[2] = new Vector3(direction2.x, direction2.y, direction2.z).normalized;
                eyeVectorData[3] = new Vector3(direction3.x, direction3.y, direction3.z).normalized;
            }

            // Write out the mesh
            var triangles = new int[6] { 0, 1, 2, 2, 3, 0 };

            return new Mesh
            {
                vertices = vertData,
                normals = eyeVectorData,
                triangles = triangles
            };
        }

        void RebuildTextures(SkySettings skySettings)
        {
            int resolution = 256;
            // Parameters not set yet. We need them for the resolution.
            if (skySettings != null)
                resolution = (int)skySettings.resolution;

            if ((m_SkyboxCubemapRT != null) && (m_SkyboxCubemapRT.width != resolution))
            {
                Utilities.Destroy(m_SkyboxCubemapRT);
                Utilities.Destroy(m_SkyboxGGXCubemapRT);
                Utilities.Destroy(m_SkyboxMarginalRowCdfRT);
                Utilities.Destroy(m_SkyboxConditionalCdfRT);

                m_SkyboxCubemapRT = null;
                m_SkyboxGGXCubemapRT = null;
                m_SkyboxMarginalRowCdfRT = null;
                m_SkyboxConditionalCdfRT = null;
            }

            if (m_SkyboxCubemapRT == null)
            {
                m_SkyboxCubemapRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                m_SkyboxCubemapRT.dimension = TextureDimension.Cube;
                m_SkyboxCubemapRT.useMipMap = true;
                m_SkyboxCubemapRT.autoGenerateMips = true; // Generate regular mipmap for filtered importance sampling
                m_SkyboxCubemapRT.filterMode = FilterMode.Trilinear;
                m_SkyboxCubemapRT.Create();

                m_SkyboxGGXCubemapRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                m_SkyboxGGXCubemapRT.dimension = TextureDimension.Cube;
                m_SkyboxGGXCubemapRT.useMipMap = true;
                m_SkyboxGGXCubemapRT.autoGenerateMips = false;
                m_SkyboxGGXCubemapRT.filterMode = FilterMode.Trilinear;
                m_SkyboxGGXCubemapRT.Create();

                if (m_useMIS)
                {
                    int width  = (int)LightSamplingParameters.TextureWidth;
                    int height = (int)LightSamplingParameters.TextureHeight;

                    // + 1 because we store the value of the integral of the cubemap at the end of the texture.
                    m_SkyboxMarginalRowCdfRT = new RenderTexture(height + 1, 1, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
                    m_SkyboxMarginalRowCdfRT.useMipMap = false;
                    m_SkyboxMarginalRowCdfRT.autoGenerateMips = false;
                    m_SkyboxMarginalRowCdfRT.enableRandomWrite = true;
                    m_SkyboxMarginalRowCdfRT.filterMode = FilterMode.Point;
                    m_SkyboxMarginalRowCdfRT.Create();

                    // TODO: switch the format to R16 (once it's available) to save some bandwidth.
                    m_SkyboxConditionalCdfRT = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
                    m_SkyboxConditionalCdfRT.useMipMap = false;
                    m_SkyboxConditionalCdfRT.autoGenerateMips = false;
                    m_SkyboxConditionalCdfRT.enableRandomWrite = true;
                    m_SkyboxConditionalCdfRT.filterMode = FilterMode.Point;
                    m_SkyboxConditionalCdfRT.Create();
                }

                m_UpdatedFramesRequired = 2; // Special case. Even if update mode is set to OnDemand, we need to regenerate the environment after destroying the texture.
            }

            m_CubemapScreenSize = new Vector4((float)resolution, (float)resolution, 1.0f / (float)resolution, 1.0f / (float)resolution);
        }

        void RebuildSkyMeshes(float nearPlane, float farPlane)
        {
            if (m_CubemapFaceMesh[0] == null)
            {
                Matrix4x4 cubeProj = Matrix4x4.Perspective(90.0f, 1.0f, nearPlane, farPlane);

                Vector3[] lookAtList =
                {
                    new Vector3(1.0f, 0.0f, 0.0f),
                    new Vector3(-1.0f, 0.0f, 0.0f),
                    new Vector3(0.0f, 1.0f, 0.0f),
                    new Vector3(0.0f, -1.0f, 0.0f),
                    new Vector3(0.0f, 0.0f, 1.0f),
                    new Vector3(0.0f, 0.0f, -1.0f),
                };

                Vector3[] UpVectorList =
                {
                    new Vector3(0.0f, 1.0f, 0.0f),
                    new Vector3(0.0f, 1.0f, 0.0f),
                    new Vector3(0.0f, 0.0f, -1.0f),
                    new Vector3(0.0f, 0.0f, 1.0f),
                    new Vector3(0.0f, 1.0f, 0.0f),
                    new Vector3(0.0f, 1.0f, 0.0f),
                };

                for (int i = 0; i < 6; ++i)
                {
                    Matrix4x4 lookAt = Matrix4x4.LookAt(Vector3.zero, lookAtList[i], UpVectorList[i]);
                    m_faceCameraViewProjectionMatrix[i] = Utilities.GetViewProjectionMatrix(lookAt, cubeProj);
                    m_faceCameraInvViewProjectionMatrix[i] = m_faceCameraViewProjectionMatrix[i].inverse;

                    m_CubemapFaceMesh[i] = BuildSkyMesh(Vector3.zero, m_faceCameraInvViewProjectionMatrix[i], true);
                }
            }
        }

        // Sets the global MIP-mapped cubemap '_SkyTexture' in the shader.
        // The texture being set is the sky (environment) map pre-convolved with GGX.
        public void SetGlobalSkyTexture()
        {
            Shader.SetGlobalTexture("_SkyTexture", m_SkyboxGGXCubemapRT);
        }

        public void Resize(float nearPlane, float farPlane)
        {
            // When loading RenderDoc, RenderTextures will go null
            RebuildTextures(skySettings);
            RebuildSkyMeshes(nearPlane, farPlane);
        }

        public void Build(RenderPipelineResources renderPipelinesResources)
        {
            // Create unititialized. Lazy initialization is performed later.
            m_iblFilterGgx = new IBLFilterGGX(renderPipelinesResources);

            // TODO: We need to have an API to send our sky information to Enlighten. For now use a workaround through skybox/cubemap material...
            m_StandardSkyboxMaterial = Utilities.CreateEngineMaterial(renderPipelinesResources.skyboxCubemap);

            m_BlitCubemapMaterial = Utilities.CreateEngineMaterial(renderPipelinesResources.blitCubemap);

            m_CurrentUpdateTime = 0.0f;
        }

        public void Cleanup()
        {
            Utilities.Destroy(m_StandardSkyboxMaterial);
            Utilities.Destroy(m_SkyboxCubemapRT);
            Utilities.Destroy(m_SkyboxGGXCubemapRT);
            Utilities.Destroy(m_SkyboxMarginalRowCdfRT);
            Utilities.Destroy(m_SkyboxConditionalCdfRT);

            if (m_Renderer != null)
                m_Renderer.Cleanup();
        }

        public bool IsSkyValid()
        {
            return m_Renderer != null && m_Renderer.IsSkyValid();
        }

        private void RenderSkyToCubemap(BuiltinSkyParameters builtinParams, SkySettings skySettings, RenderTexture target)
        {
            for (int i = 0; i < 6; ++i)
            {
                builtinParams.invViewProjMatrix = m_faceCameraInvViewProjectionMatrix[i];
                builtinParams.screenSize = m_CubemapScreenSize;
                builtinParams.skyMesh = m_CubemapFaceMesh[i];
                builtinParams.colorBuffer = target;
                builtinParams.depthBuffer = BuiltinSkyParameters.nullRT;

                Utilities.SetRenderTarget(builtinParams.renderContext, target, ClearFlag.ClearNone, 0, (CubemapFace)i);
                m_Renderer.RenderSky(builtinParams, skySettings, true);
            }
        }

        private void BlitCubemap(ScriptableRenderContext renderContext, Cubemap source, RenderTexture dest)
        {

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

            for (int i = 0; i < 6; ++i)
            {
                Utilities.SetRenderTarget(renderContext, dest, ClearFlag.ClearNone, 0, (CubemapFace)i);
                var cmd = new CommandBuffer { name = "" };
                propertyBlock.SetTexture("_MainTex", source);
                propertyBlock.SetFloat("_faceIndex", (float)i);
                cmd.DrawProcedural(Matrix4x4.identity, m_BlitCubemapMaterial, 0, MeshTopology.Triangles, 3, 1, propertyBlock);
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }

        }

        private void RenderCubemapGGXConvolution(ScriptableRenderContext renderContext, BuiltinSkyParameters builtinParams, SkySettings skyParams, Texture input, RenderTexture target)
        {
            using (new Utilities.ProfilingSample("Sky Pass: GGX Convolution", renderContext))
            {
                int mipCount = 1 + (int)Mathf.Log(input.width, 2.0f);
                if (mipCount < ((int)EnvConstants.SpecCubeLodStep + 1))
                {
                    Debug.LogWarning("RenderCubemapGGXConvolution: Cubemap size is too small for GGX convolution, needs at least " + ((int)EnvConstants.SpecCubeLodStep + 1) + " mip levels");
                    return;
                }

                if (!m_iblFilterGgx.IsInitialized())
                {
                    m_iblFilterGgx.Initialize(renderContext);
                }

                // Copy the first mip
                var cmd = new CommandBuffer { name = "" };
                for (int f = 0; f < 6; f++)
                {
                    cmd.CopyTexture(input, f, 0, target, f, 0);
                }
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Dispose();

                if (m_useMIS && m_iblFilterGgx.SupportMIS)
                {
                    m_iblFilterGgx.FilterCubemapMIS(renderContext, input, target, mipCount, m_SkyboxConditionalCdfRT, m_SkyboxMarginalRowCdfRT, m_CubemapFaceMesh);
                }
                else
                {
                    m_iblFilterGgx.FilterCubemap(renderContext, input, target, mipCount, m_CubemapFaceMesh);
                }
            }
        }

        public void RequestEnvironmentUpdate()
        {
            m_UpdatedFramesRequired = Math.Max(m_UpdatedFramesRequired, 1);
        }

        public void UpdateEnvironment(HDCamera camera, Light sunLight, ScriptableRenderContext renderContext)
        {
            using (new Utilities.ProfilingSample("Sky Environment Pass", renderContext))
            {

                // We need one frame delay for this update to work since DynamicGI.UpdateEnvironment is executed direclty but the renderloop is not (so we need to wait for the sky texture to be rendered first)
                if (m_NeedLowLevelUpdateEnvironment)
                {
                    // TODO: Properly send the cubemap to Enlighten. Currently workaround is to set the cubemap in a Skybox/cubemap material
                    m_StandardSkyboxMaterial.SetTexture("_Tex", m_SkyboxCubemapRT);
                    RenderSettings.skybox = m_StandardSkyboxMaterial; // Setup this material as the default to be use in RenderSettings
                    RenderSettings.ambientIntensity = 1.0f; // fix this to 1, this parameter should not exist!
                    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox; // Force skybox for our HDRI
                    RenderSettings.reflectionIntensity = 1.0f;
                    RenderSettings.customReflection = null;
                    DynamicGI.UpdateEnvironment();

                    m_NeedLowLevelUpdateEnvironment = false;
                }

                if (IsSkyValid())
                {
                    m_CurrentUpdateTime += Time.deltaTime;

                    m_BuiltinParameters.renderContext = renderContext;
                    m_BuiltinParameters.sunLight = sunLight;

                    if (
                        m_UpdatedFramesRequired > 0 ||
                        (skySettings.updateMode == EnvironementUpdateMode.OnChanged && skySettings.GetHash() != m_SkyParametersHash) ||
                        (skySettings.updateMode == EnvironementUpdateMode.Realtime && m_CurrentUpdateTime > skySettings.updatePeriod)
                        )
                    {
                        // Render sky into a cubemap - doesn't happen every frame, can be controlled
                        // Note that m_SkyboxCubemapRT is created with auto-generate mipmap, it mean that here we have also our mipmap correctly box filtered for importance sampling.
                        if(m_SkySettings.lightingOverride == null)
                            RenderSkyToCubemap(m_BuiltinParameters, skySettings, m_SkyboxCubemapRT);
                        // In case the user overrides the lighting, we already have a cubemap ready but we need to blit it anyway for potential resize and so that we can generate proper mipmaps for enlighten.
                        else
                            BlitCubemap(renderContext, m_SkySettings.lightingOverride, m_SkyboxCubemapRT);

                        // Convolve downsampled cubemap
                        RenderCubemapGGXConvolution(renderContext, m_BuiltinParameters, skySettings, m_SkyboxCubemapRT, m_SkyboxGGXCubemapRT);

                        m_NeedLowLevelUpdateEnvironment = true;
                        m_UpdatedFramesRequired--;
                        m_SkyParametersHash = skySettings.GetHash();
                        m_CurrentUpdateTime = 0.0f;
                    }
                }
                else
                {
                    if(m_SkyParametersHash != 0)
                    {
                        // Clear temp cubemap and redo GGX from black and then feed it to enlighten for default light probe.
                        Utilities.ClearCubemap(renderContext, m_SkyboxCubemapRT, Color.black);
                        RenderCubemapGGXConvolution(renderContext, m_BuiltinParameters, skySettings, m_SkyboxCubemapRT, m_SkyboxGGXCubemapRT);

                        m_SkyParametersHash = 0;
                        m_NeedLowLevelUpdateEnvironment = true;
                    }
                }
            }
        }

        public void RenderSky(HDCamera camera, Light sunLight, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, ScriptableRenderContext renderContext)
        {
            using (new Utilities.ProfilingSample("Sky Pass", renderContext))
            {
                if (IsSkyValid())
                {
                    m_BuiltinParameters.renderContext = renderContext;
                    m_BuiltinParameters.sunLight = sunLight;
                    m_BuiltinParameters.invViewProjMatrix = camera.invViewProjectionMatrix;
                    m_BuiltinParameters.cameraPosWS = camera.camera.transform.position;
                    m_BuiltinParameters.screenSize = camera.screenSize;
                    m_BuiltinParameters.skyMesh = BuildSkyMesh(camera.camera.GetComponent<Transform>().position, m_BuiltinParameters.invViewProjMatrix, false);
                    m_BuiltinParameters.colorBuffer = colorBuffer;
                    m_BuiltinParameters.depthBuffer = depthBuffer;

                    m_Renderer.SetRenderTargets(m_BuiltinParameters);
                    m_Renderer.RenderSky(m_BuiltinParameters, skySettings, false);
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

            RenderTexture tempRT = new RenderTexture(resolution * 6, resolution, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            tempRT.dimension = TextureDimension.Tex2D;
            tempRT.useMipMap = false;
            tempRT.autoGenerateMips = false;
            tempRT.filterMode = FilterMode.Trilinear;
            tempRT.Create();

            Texture2D temp = new Texture2D(resolution * 6, resolution, TextureFormat.RGBAFloat, false);
            Texture2D result = new Texture2D(resolution * 6, resolution, TextureFormat.RGBAFloat, false);

            // Note: We need to invert in Y the cubemap faces because the current sky cubemap is inverted (because it's a RT)
            // So to invert it again so that it's a proper cubemap image we need to do it in several steps because ReadPixels does not have scale parameters:
            // - Convert the cubemap into a 2D texture
            // - Blit and invert it to a temporary target.
            // - Read this target again into the result texture.
            int offset = 0;
            for (int i = 0; i < 6; ++i)
            {
                Graphics.SetRenderTarget(m_SkyboxCubemapRT, 0, (CubemapFace)i);
                temp.ReadPixels(new Rect(0, 0, resolution, resolution), offset, 0);
                temp.Apply();
                offset += resolution;
            }

            // Flip texture.
            // Temporarily disabled until proper API reaches trunk
            //Graphics.Blit(temp, tempRT, new Vector2(1.0f, -1.0f), new Vector2(0.0f, 0.0f));
            Graphics.Blit(temp, tempRT);

            result.ReadPixels(new Rect(0, 0, resolution * 6, resolution), 0, 0);
            result.Apply();

            Graphics.SetRenderTarget(null);
            Object.DestroyImmediate(temp);
            Object.DestroyImmediate(tempRT);

            return result;
        }
    }
}
