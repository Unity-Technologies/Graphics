using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
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

    public class BuiltinSkyParameters
    {
        public Matrix4x4    invViewProjMatrix;
        public Mesh         skyMesh;
        public RenderLoop   renderLoop;
        public Light        sunLight;
    }

    public class SkyManager
    {
        RenderTexture           m_SkyboxCubemapRT = null;
        RenderTexture           m_SkyboxGGXCubemapRT = null;

        Material                m_StandardSkyboxMaterial = null; // This is the Unity standard skybox material. Used to pass the correct cubemap to Enlighten.
        Material                m_GGXConvolveMaterial = null; // Apply GGX convolution to cubemap

        Matrix4x4[]             m_faceCameraInvViewProjectionMatrix = new Matrix4x4[6];
        Mesh[]                  m_CubemapFaceMesh = new Mesh[6];

        BuiltinSkyParameters    m_BuiltinParameters = new BuiltinSkyParameters();
        SkyRenderer             m_Renderer = null;
        int                     m_SkyParametersHash = 0;
        bool                    m_NeedUpdateEnvironment = false;

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
            }
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
                    m_faceCameraInvViewProjectionMatrix[i] = Utilities.GetViewProjectionMatrix(lookAt, cubeProj).inverse;

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
            m_StandardSkyboxMaterial = Utilities.CreateEngineMaterial("Skybox/Cubemap");
            m_GGXConvolveMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderLoop/GGXConvolve");
        }

        public void Cleanup()
        {
            Utilities.Destroy(m_StandardSkyboxMaterial);
            Utilities.Destroy(m_GGXConvolveMaterial);
            Utilities.Destroy(m_SkyboxCubemapRT);
            Utilities.Destroy(m_SkyboxGGXCubemapRT);

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
                Utilities.SetRenderTarget(builtinParams.renderLoop, target, ClearFlag.ClearNone, 0, (CubemapFace)i);

                builtinParams.invViewProjMatrix = m_faceCameraInvViewProjectionMatrix[i];
                builtinParams.skyMesh = m_CubemapFaceMesh[i];
                m_Renderer.RenderSky(builtinParams, skyParameters);
            }
        }

        private void RenderCubemapGGXConvolution(RenderLoop renderLoop, BuiltinSkyParameters builtinParams, SkyParameters skyParams, Texture input, RenderTexture target)
        {
            using (new Utilities.ProfilingSample("Sky Pass: GGX Convolution", renderLoop))
            {
                int mipCount = 1 + (int)Mathf.Log(input.width, 2.0f);
                if (mipCount < ((int)EnvConstants.SpecCubeLodStep + 1))
                {
                    Debug.LogWarning("RenderCubemapGGXConvolution: Cubemap size is too small for GGX convolution, needs at least " + ((int)EnvConstants.SpecCubeLodStep + 1) + " mip levels");
                    return;
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

                // Do the convolution on remaining mipmaps
                float invOmegaP = (6.0f * input.width * input.width) / (4.0f * Mathf.PI); // Solid angle associated to a pixel of the cubemap;

                m_GGXConvolveMaterial.SetTexture("_MainTex", input);
                m_GGXConvolveMaterial.SetFloat("_MipMapCount", mipCount);
                m_GGXConvolveMaterial.SetFloat("_InvOmegaP", invOmegaP);

                for (int mip = 1; mip < ((int)EnvConstants.SpecCubeLodStep + 1); ++mip)
                {
                    MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                    propertyBlock.SetFloat("_Level", mip);

                    for (int face = 0; face < 6; ++face)
                    {
                        Utilities.SetRenderTarget(renderLoop, target, ClearFlag.ClearNone, mip, (CubemapFace)face);

                        var cmd = new CommandBuffer { name = "" };
                        cmd.DrawMesh(m_CubemapFaceMesh[face], Matrix4x4.identity, m_GGXConvolveMaterial, 0, 0, propertyBlock);
                        renderLoop.ExecuteCommandBuffer(cmd);
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

        public void RenderSky(Camera camera, Light sunLight, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, RenderLoop renderLoop)
        {
            using (new Utilities.ProfilingSample("Sky Pass", renderLoop))
            {
                if (IsSkyValid())
                {
                    m_BuiltinParameters.renderLoop = renderLoop;
                    m_BuiltinParameters.sunLight = sunLight;

                    // We need one frame delay for this update to work since DynamicGI.UpdateEnvironment is executed direclty but the renderloop is not (so we need to wait for the sky texture to be rendered first)
                    if(m_NeedUpdateEnvironment)
                    {
                        // TODO: Properly send the cubemap to Enlighten. Currently workaround is to set the cubemap in a Skybox/cubemap material
                        m_StandardSkyboxMaterial.SetTexture("_Tex", m_SkyboxCubemapRT);
                        RenderSettings.skybox = m_StandardSkyboxMaterial; // Setup this material as the default to be use in RenderSettings
                        RenderSettings.ambientIntensity = 1.0f; // fix this to 1, this parameter should not exist!
                        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox; // Force skybox for our HDRI
                        RenderSettings.reflectionIntensity = 1.0f;
                        RenderSettings.customReflection = null;
                        DynamicGI.UpdateEnvironment();
                        
                        m_NeedUpdateEnvironment = false;
                    }

                    if (skyParameters.GetHash() != m_SkyParametersHash)
                    {
                        using (new Utilities.ProfilingSample("Sky Pass: Render Cubemap", renderLoop))
                        {
                            // Render sky into a cubemap - doesn't happen every frame, can be controlled
                            RenderSkyToCubemap(m_BuiltinParameters, skyParameters, m_SkyboxCubemapRT);
                            // Convolve downsampled cubemap
                            RenderCubemapGGXConvolution(renderLoop, m_BuiltinParameters, skyParameters, m_SkyboxCubemapRT, m_SkyboxGGXCubemapRT);

                            m_NeedUpdateEnvironment = true;
                        }

                        m_SkyParametersHash = skyParameters.GetHash();
                    }

                    // Render the sky itself
                    Utilities.SetRenderTarget(renderLoop, colorBuffer, depthBuffer);
                    m_BuiltinParameters.invViewProjMatrix = Utilities.GetViewProjectionMatrix(camera).inverse;
                    m_BuiltinParameters.skyMesh = BuildSkyMesh(camera.GetComponent<Transform>().position, m_BuiltinParameters.invViewProjMatrix, false);
                    m_Renderer.RenderSky(m_BuiltinParameters, skyParameters);
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
                    //    Utilities.SetRenderTarget(renderLoop, m_SkyboxCubemapRT, ClearFlag.ClearColor);
                    //    RenderCubemapGGXConvolution(renderLoop, m_BuiltinParameters, skyParameters, m_SkyboxCubemapRT, m_SkyboxGGXCubemapRT);

                    //    m_SkyParametersHash = 0;
                    //}
                }
            }
        }
    }
}
