using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System;

namespace UnityEngine.Experimental.ScriptableRenderLoop
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
        public Matrix4x4                viewProjMatrix;
        public Matrix4x4                invViewProjMatrix;
        public Vector3                  cameraPosWS;
        public Vector4                  screenSize;
        public Mesh                     skyMesh;
        public ScriptableRenderContext  renderContext;
        public Light                    sunLight;
        public RenderTargetIdentifier   colorBuffer;
        public RenderTargetIdentifier   depthBuffer;

        public static RenderTargetIdentifier invalidRTI = -1;
    }

    public class SkyManager
    {
        RenderTexture           m_SkyboxCubemapRT = null;
        RenderTexture           m_SkyboxGGXCubemapRT = null;
        RenderTexture           m_SkyboxMarginalRowCdfRT = null;
        RenderTexture           m_SkyboxConditionalCdfRT = null;

        Material                m_StandardSkyboxMaterial = null; // This is the Unity standard skybox material. Used to pass the correct cubemap to Enlighten.
        Material                m_GGXConvolveMaterial = null; // Apply GGX convolution to cubemap

        ComputeShader           m_BuildProbabilityTablesCS = null;
        int                     m_ConditionalDensitiesKernel = -1;
        int                     m_MarginalRowDensitiesKernel = -1;

        Vector4                 m_CubemapScreenSize;
        Matrix4x4[]             m_faceCameraViewProjectionMatrix = new Matrix4x4[6];
        Matrix4x4[]             m_faceCameraInvViewProjectionMatrix = new Matrix4x4[6];
        Mesh[]                  m_CubemapFaceMesh = new Mesh[6];

        BuiltinSkyParameters    m_BuiltinParameters = new BuiltinSkyParameters();
        SkyRenderer             m_Renderer = null;
        int                     m_SkyParametersHash = 0;
        bool                    m_NeedLowLevelUpdateEnvironment = false;
        bool                    m_UpdateRequired = true;
        float                   m_CurrentUpdateTime = 0.0f;

        bool                    m_useMIS = false;

        SkyParameters           m_SkyParameters = null;

        public SkyParameters skyParameters
        {
            set
            {
                if(m_Renderer != null)
                {
                    if (value == null || IsSkyParameterValid(value))
                    {
                        m_SkyParametersHash = 0;
                        m_SkyParameters = value;
                        m_UpdateRequired = true;
                    }
                    else
                    {
                        Debug.LogWarning("Sky renderer needs an instance of " + GetSkyParameterType().ToString() + " to be able to render.");
                    }
                }
            }
            get { return m_SkyParameters; }
        }

        public void InstantiateSkyRenderer(Type skyRendererType)
        {
            if(skyRendererType == null)
            {
                m_Renderer = null;
            }
            else if (m_Renderer == null || m_Renderer.GetType() != skyRendererType)
            {
                m_Renderer = Activator.CreateInstance(skyRendererType) as SkyRenderer;
                m_Renderer.Build();
            }
        }

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

        void RebuildTextures(SkyParameters skyParameters)
        {
            int resolution = 256;
            // Parameters not set yet. We need them for the resolution.
            if (skyParameters != null)
                resolution = (int)skyParameters.resolution;

            if ((m_SkyboxCubemapRT != null) && (m_SkyboxCubemapRT.width != resolution))
            {
                Utilities.Destroy(m_SkyboxCubemapRT);
                Utilities.Destroy(m_SkyboxGGXCubemapRT);
                Utilities.Destroy(m_SkyboxMarginalRowCdfRT);
                Utilities.Destroy(m_SkyboxConditionalCdfRT);

                m_UpdateRequired = true; // Special case. Even if update mode is set to OnDemand, we need to regenerate the environment after destroying the texture.
            }

            if (m_SkyboxCubemapRT == null)
            {
                m_SkyboxCubemapRT = new RenderTexture(resolution, resolution, 1, RenderTextureFormat.ARGBHalf);
                m_SkyboxCubemapRT.dimension = TextureDimension.Cube;
                m_SkyboxCubemapRT.useMipMap = true;
                m_SkyboxCubemapRT.autoGenerateMips = true; // Generate regular mipmap for filtered importance sampling
                m_SkyboxCubemapRT.filterMode = FilterMode.Trilinear;
                m_SkyboxCubemapRT.Create();

                m_SkyboxGGXCubemapRT = new RenderTexture(resolution, resolution, 1, RenderTextureFormat.ARGBHalf);
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
                    m_SkyboxMarginalRowCdfRT = new RenderTexture(height + 1, 1, 1, RenderTextureFormat.RFloat);
                    m_SkyboxMarginalRowCdfRT.dimension = TextureDimension.Tex2D;
                    m_SkyboxMarginalRowCdfRT.useMipMap = false;
                    m_SkyboxMarginalRowCdfRT.autoGenerateMips = false;
                    m_SkyboxMarginalRowCdfRT.enableRandomWrite = true;
                    m_SkyboxMarginalRowCdfRT.filterMode = FilterMode.Point;
                    m_SkyboxMarginalRowCdfRT.Create();

                    // TODO: switch the format to R16 (once it's available) to save some bandwidth.
                    m_SkyboxConditionalCdfRT = new RenderTexture(width, height, 1, RenderTextureFormat.RFloat);
                    m_SkyboxConditionalCdfRT.dimension = TextureDimension.Tex2D;
                    m_SkyboxConditionalCdfRT.useMipMap = false;
                    m_SkyboxConditionalCdfRT.autoGenerateMips = false;
                    m_SkyboxConditionalCdfRT.enableRandomWrite = true;
                    m_SkyboxConditionalCdfRT.filterMode = FilterMode.Point;
                    m_SkyboxConditionalCdfRT.Create();
                }
            }

            m_CubemapScreenSize = new Vector4((float)resolution, (float)resolution, 1.0f / (float)resolution, 1.0f / (float)resolution);
        }

        void RebuildSkyMeshes()
        {
            if(m_CubemapFaceMesh[0] == null)
            {
                Matrix4x4 cubeProj = Matrix4x4.Perspective(90.0f, 1.0f, 0.1f, 1.0f);

                Vector3[] lookAtList = {
                            new Vector3(1.0f, 0.0f, 0.0f),
                            new Vector3(-1.0f, 0.0f, 0.0f),
                            new Vector3(0.0f, 1.0f, 0.0f),
                            new Vector3(0.0f, -1.0f, 0.0f),
                            new Vector3(0.0f, 0.0f, 1.0f),
                            new Vector3(0.0f, 0.0f, -1.0f),
                        };

                Vector3[] UpVectorList = {
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

                    // When rendering into a texture the render will be flip (due to legacy unity openGL behavior), so we need to flip UV here...
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

        public void Resize()
        {
            // When loading RenderDoc, RenderTextures will go null
            RebuildTextures(skyParameters);
            RebuildSkyMeshes();
        }

        public void Build()
        {
            if (m_Renderer != null)
                m_Renderer.Build();

            // TODO: We need to have an API to send our sky information to Enlighten. For now use a workaround through skybox/cubemap material...
            m_StandardSkyboxMaterial   = Utilities.CreateEngineMaterial("Skybox/Cubemap");
            m_GGXConvolveMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/GGXConvolve");
            m_BuildProbabilityTablesCS = Resources.Load<ComputeShader>("BuildProbabilityTables");

            m_ConditionalDensitiesKernel = m_BuildProbabilityTablesCS.FindKernel("ComputeConditionalDensities");
            m_MarginalRowDensitiesKernel = m_BuildProbabilityTablesCS.FindKernel("ComputeMarginalRowDensities");

            m_CurrentUpdateTime = 0.0f;
        }

        public void Cleanup()
        {
            Utilities.Destroy(m_StandardSkyboxMaterial);
            Utilities.Destroy(m_GGXConvolveMaterial);
            Utilities.Destroy(m_SkyboxCubemapRT);
            Utilities.Destroy(m_SkyboxGGXCubemapRT);
            Utilities.Destroy(m_SkyboxMarginalRowCdfRT);
            Utilities.Destroy(m_SkyboxConditionalCdfRT);

            if(m_Renderer != null)
                m_Renderer.Cleanup();
        }

        public bool IsSkyValid()
        {
            return m_Renderer != null && m_Renderer.IsParameterValid(skyParameters) && m_Renderer.IsSkyValid(skyParameters);
        }

        private void RenderSkyToCubemap(BuiltinSkyParameters builtinParams, SkyParameters skyParameters, RenderTexture target)
        {
            for (int i = 0; i < 6; ++i)
            {
                Utilities.SetRenderTarget(builtinParams.renderContext, target, ClearFlag.ClearNone, 0, (CubemapFace)i);

                builtinParams.invViewProjMatrix = m_faceCameraInvViewProjectionMatrix[i];
                builtinParams.viewProjMatrix = m_faceCameraViewProjectionMatrix[i];
                builtinParams.screenSize = m_CubemapScreenSize;
                builtinParams.skyMesh = m_CubemapFaceMesh[i];
                builtinParams.colorBuffer = target;
                builtinParams.depthBuffer = BuiltinSkyParameters.invalidRTI;
                m_Renderer.RenderSky(builtinParams, skyParameters);
            }
        }

        private void BuildProbabilityTables(ScriptableRenderContext renderContext)
        {
            // Bind the input cubemap.
            m_BuildProbabilityTablesCS.SetTexture(m_ConditionalDensitiesKernel, "envMap", m_SkyboxCubemapRT);

            // Bind the outputs.
            m_BuildProbabilityTablesCS.SetTexture(m_ConditionalDensitiesKernel, "marginalRowDensities", m_SkyboxMarginalRowCdfRT);
            m_BuildProbabilityTablesCS.SetTexture(m_ConditionalDensitiesKernel, "conditionalDensities", m_SkyboxConditionalCdfRT);
            m_BuildProbabilityTablesCS.SetTexture(m_MarginalRowDensitiesKernel, "marginalRowDensities", m_SkyboxMarginalRowCdfRT);

            var cmd = new CommandBuffer() { name = "" };
            cmd.DispatchCompute(m_BuildProbabilityTablesCS, m_ConditionalDensitiesKernel, (int)LightSamplingParameters.TextureHeight, 1, 1);
            cmd.DispatchCompute(m_BuildProbabilityTablesCS, m_MarginalRowDensitiesKernel, 1, 1, 1);
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        private void RenderCubemapGGXConvolution(ScriptableRenderContext renderContext, BuiltinSkyParameters builtinParams, SkyParameters skyParams, Texture input, RenderTexture target)
        {
            using (new Utilities.ProfilingSample("Sky Pass: GGX Convolution", renderContext))
            {
                int mipCount = 1 + (int)Mathf.Log(input.width, 2.0f);
                if (mipCount < ((int)EnvConstants.SpecCubeLodStep + 1))
                {
                    Debug.LogWarning("RenderCubemapGGXConvolution: Cubemap size is too small for GGX convolution, needs at least " + ((int)EnvConstants.SpecCubeLodStep + 1) + " mip levels");
                    return;
                }

                if (m_useMIS)
                {
                    BuildProbabilityTables(renderContext);
                }

                // Copy the first mip.

                // WARNING:
                // Since we can't instanciate the parameters anymore (we don't know the final type here)
                // we can't make sure that exposure/multiplier etc are at neutral values
                // This will be solved with proper CopyTexture

                // TEMP code until CopyTexture is implemented for command buffer
                // All parameters are neutral because exposure/multiplier have already been applied in the first copy.
                //SkyParameters skyParams = new SkyParameters();
                //skyParams.exposure = 0.0f;
                //skyParams.multiplier = 1.0f;
                //skyParams.rotation = 0.0f;
                //skyParams.skyHDRI = input;
                RenderSkyToCubemap(builtinParams, skyParams, target);
                // End temp

                //for (int f = 0; f < 6; f++)
                //    Graphics.CopyTexture(input, f, 0, target, f, 0);

                if (m_useMIS)
                {
                    m_GGXConvolveMaterial.EnableKeyword("USE_MIS");
                    m_GGXConvolveMaterial.SetTexture("_MarginalRowDensities", m_SkyboxMarginalRowCdfRT);
                    m_GGXConvolveMaterial.SetTexture("_ConditionalDensities", m_SkyboxConditionalCdfRT);
                }
                else
                {
                    m_GGXConvolveMaterial.DisableKeyword("USE_MIS");
                }

                // Do the convolution on remaining mipmaps
                float invOmegaP = (6.0f * input.width * input.width) / (4.0f * Mathf.PI); // Solid angle associated to a pixel of the cubemap;

                m_GGXConvolveMaterial.SetTexture("_MainTex", input);
                m_GGXConvolveMaterial.SetFloat("_MaxLevel", mipCount - 1);
                m_GGXConvolveMaterial.SetFloat("_InvOmegaP", invOmegaP);

                for (int mip = 1; mip < ((int)EnvConstants.SpecCubeLodStep + 1); ++mip)
                {
                    MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                    propertyBlock.SetFloat("_Level", mip);

                    for (int face = 0; face < 6; ++face)
                    {
                        Utilities.SetRenderTarget(renderContext, target, ClearFlag.ClearNone, mip, (CubemapFace)face);

                        var cmd = new CommandBuffer { name = "" };
                        cmd.DrawMesh(m_CubemapFaceMesh[face], Matrix4x4.identity, m_GGXConvolveMaterial, 0, 0, propertyBlock);
                        renderContext.ExecuteCommandBuffer(cmd);
                        cmd.Dispose();
                    }
                }

            }
        }

        public bool IsSkyParameterValid(SkyParameters parameters)
        {
            return m_Renderer != null && m_Renderer.IsParameterValid(parameters);
        }

        public Type GetSkyParameterType()
        {
            return (m_Renderer == null) ? null : m_Renderer.GetSkyParameterType();
        }

        public void RequestEnvironmentUpdate()
        {
            m_UpdateRequired = true;
        }

        public void UpdateEnvironment(HDRenderPipeline.HDCamera camera, Light sunLight, ScriptableRenderContext renderContext)
        {
            {
                using (new Utilities.ProfilingSample("Sky Environment Pass", renderContext))
                {
                    if (IsSkyValid())
                    {
                        m_CurrentUpdateTime += Time.deltaTime;

                        m_BuiltinParameters.renderContext = renderContext;
                        m_BuiltinParameters.sunLight = sunLight;

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

                        if (
                                (skyParameters.updateMode == EnvironementUpdateMode.OnDemand && m_UpdateRequired) ||
                                (skyParameters.updateMode == EnvironementUpdateMode.OnChanged && skyParameters.GetHash() != m_SkyParametersHash) ||
                                (skyParameters.updateMode == EnvironementUpdateMode.Realtime && m_CurrentUpdateTime > skyParameters.updatePeriod)
                           )
                        {
                            // Render sky into a cubemap - doesn't happen every frame, can be controlled
                            RenderSkyToCubemap(m_BuiltinParameters, skyParameters, m_SkyboxCubemapRT);
                            // Convolve downsampled cubemap
                            RenderCubemapGGXConvolution(renderContext, m_BuiltinParameters, skyParameters, m_SkyboxCubemapRT, m_SkyboxGGXCubemapRT);

                            m_NeedLowLevelUpdateEnvironment = true;
                            m_UpdateRequired = false;
                            m_SkyParametersHash = skyParameters.GetHash();
                            m_CurrentUpdateTime = 0.0f;
                        }
                    }
                    else
                    {
                        // Disabled for now.
                        // We need to remove RenderSkyToCubemap from the RenderCubemapGGXConvolution first as it needs the skyparameter to be valid.
                        //if(m_SkyParametersHash != 0)
                        //{
                        //    // Clear sky light probe
                        //    RenderSettings.skybox = null;
                        //    RenderSettings.ambientIntensity = 1.0f; // fix this to 1, this parameter should not exist!
                        //    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox; // Force skybox for our HDRI
                        //    RenderSettings.reflectionIntensity = 1.0f;
                        //    RenderSettings.customReflection = null;
                        //    DynamicGI.UpdateEnvironment();

                        //    // Clear temp cubemap and redo GGX from black
                        //    Utilities.SetRenderTarget(renderContext, m_SkyboxCubemapRT, ClearFlag.ClearColor);
                        //    RenderCubemapGGXConvolution(renderContext, m_BuiltinParameters, skyParameters, m_SkyboxCubemapRT, m_SkyboxGGXCubemapRT);

                        //    m_SkyParametersHash = 0;
                        //}
                    }
                }
            }
        }

        public void RenderSky(HDRenderPipeline.HDCamera camera, Light sunLight, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, ScriptableRenderContext renderContext)
        {
            using (new Utilities.ProfilingSample("Sky Pass", renderContext))
            {
                if (IsSkyValid())
                {
                    m_BuiltinParameters.renderContext = renderContext;
                    m_BuiltinParameters.sunLight = sunLight;
                    m_BuiltinParameters.invViewProjMatrix = camera.invViewProjectionMatrix;
                    m_BuiltinParameters.viewProjMatrix = camera.viewProjectionMatrix;
                    m_BuiltinParameters.cameraPosWS = camera.camera.transform.position;
                    m_BuiltinParameters.screenSize = camera.screenSize;
                    m_BuiltinParameters.skyMesh = BuildSkyMesh(camera.camera.GetComponent<Transform>().position, m_BuiltinParameters.invViewProjMatrix, false);
                    m_BuiltinParameters.colorBuffer = colorBuffer;
                    m_BuiltinParameters.depthBuffer = depthBuffer;

                    Utilities.SetRenderTarget(renderContext, colorBuffer, depthBuffer);
                    m_Renderer.RenderSky(m_BuiltinParameters, skyParameters);
                }
            }
        }
    }
}
