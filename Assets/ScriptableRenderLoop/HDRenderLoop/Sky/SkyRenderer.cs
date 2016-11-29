using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System;


namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    [Serializable]
    public class SkyParameters
    {
        public Texture skyHDRI;
        public float rotation = 0.0f;
        public float exposure = 0.0f;
        public float multiplier = 1.0f;
    }

    public class SkyRenderer
    {
        const int kSkyCubemapSize = 256;

        RenderTexture m_SkyboxCubemapRT = null;
        RenderTexture m_SkyboxGGXCubemapRT = null;

        Material m_StandardSkyboxMaterial = null; // This is the Unity standard skybox material. Used to pass the correct cubemap to Enlighten.
        Material m_SkyHDRIMaterial = null; // Renders a cubemap into a render texture (can be cube or 2D)
        Material m_GGXConvolveMaterial = null; // Apply GGX convolution to cubemap

        MaterialPropertyBlock m_RenderSkyPropertyBlock = null;

        GameObject[] m_CubemapFaceCamera = new GameObject[6];

        Mesh BuildSkyMesh(Camera camera, bool forceUVBottom)
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

            // Get view vector vased on the frustrum, i.e (invert transform frustrum get position etc...)
            Vector3[] eyeVectorData = new Vector3[4];

            Matrix4x4 transformMatrix = camera.cameraToWorldMatrix * camera.projectionMatrix.inverse;

            Vector4 posWorldSpace0 = transformMatrix * vertData0;
            Vector4 posWorldSpace1 = transformMatrix * vertData1;
            Vector4 posWorldSpace2 = transformMatrix * vertData2;
            Vector4 posWorldSpace3 = transformMatrix * vertData3;

            Vector3 temp = camera.GetComponent<Transform>().position;
            Vector4 cameraPosition = new Vector4(temp.x, temp.y, temp.z, 0.0f);

            Vector4 direction0 = (posWorldSpace0 / posWorldSpace0.w - cameraPosition);
            Vector4 direction1 = (posWorldSpace1 / posWorldSpace1.w - cameraPosition);
            Vector4 direction2 = (posWorldSpace2 / posWorldSpace2.w - cameraPosition);
            Vector4 direction3 = (posWorldSpace3 / posWorldSpace3.w - cameraPosition);

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

        void RebuildTextures()
        {
            if(m_SkyboxCubemapRT == null)
            {
                m_SkyboxCubemapRT = new RenderTexture(kSkyCubemapSize, kSkyCubemapSize, 1, RenderTextureFormat.ARGBHalf);
                m_SkyboxCubemapRT.dimension = TextureDimension.Cube;
                m_SkyboxCubemapRT.useMipMap = true;
                m_SkyboxCubemapRT.autoGenerateMips = true;
                m_SkyboxCubemapRT.filterMode = FilterMode.Point;
                m_SkyboxCubemapRT.Create();
            }

            if(m_SkyboxGGXCubemapRT == null)
            {
                m_SkyboxGGXCubemapRT = new RenderTexture(kSkyCubemapSize, kSkyCubemapSize, 1, RenderTextureFormat.ARGBHalf);
                m_SkyboxGGXCubemapRT.dimension = TextureDimension.Cube;
                m_SkyboxGGXCubemapRT.useMipMap = true;
                m_SkyboxGGXCubemapRT.autoGenerateMips = false;
                m_SkyboxGGXCubemapRT.filterMode = FilterMode.Trilinear;
                m_SkyboxGGXCubemapRT.Create();
            }
        }

        // Sets the global "_SkyTexture" cubemap array in the shader.
        // The texture being set is a sky (environment) map pre-convolved with GGX.
        public void SetGlobalSkyTexture()
        {
            Shader.SetGlobalTexture("_SkyTexture", m_SkyboxGGXCubemapRT);
        }

        public void Rebuild()
        {
            // TODO: We need to have an API to send our sky information to Enlighten. For now use a workaround through skybox/cubemap material...
            m_StandardSkyboxMaterial = Utilities.CreateEngineMaterial("Skybox/Cubemap");

            m_SkyHDRIMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderLoop/SkyHDRI");
            m_GGXConvolveMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderLoop/GGXConvolve");

            m_RenderSkyPropertyBlock = new MaterialPropertyBlock();

            RebuildTextures();

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
                m_CubemapFaceCamera[i] = new GameObject();
                m_CubemapFaceCamera[i].hideFlags = HideFlags.HideAndDontSave;

                Camera camera = m_CubemapFaceCamera[i].AddComponent<Camera>();
                camera.projectionMatrix = cubeProj;
                Transform transform = camera.GetComponent<Transform>();
                transform.LookAt(lookAtList[i], UpVectorList[i]);
            }
        }

        public void OnDisable()
        {
            Utilities.Destroy(m_StandardSkyboxMaterial);
            Utilities.Destroy(m_SkyHDRIMaterial);
            Utilities.Destroy(m_GGXConvolveMaterial);
            Utilities.Destroy(m_SkyboxCubemapRT);
            Utilities.Destroy(m_SkyboxGGXCubemapRT);

            for(int i = 0 ; i < 6 ; ++i)
            {
                Utilities.Destroy(m_CubemapFaceCamera[i]);
            }

        }

        bool IsSkyValid(SkyParameters parameters)
        {
            // Later we will also test shader for procedural skies.
            return parameters.skyHDRI != null;
        }

        private void RenderSky(Camera camera, SkyParameters skyParameters, bool forceUVBottom, RenderLoop renderLoop)
        {
            Mesh skyMesh = BuildSkyMesh(camera, forceUVBottom);

            Shader.EnableKeyword("PERFORM_SKY_OCCLUSION_TEST");

            m_RenderSkyPropertyBlock.SetTexture("_Cubemap", skyParameters.skyHDRI);
            m_RenderSkyPropertyBlock.SetVector("_SkyParam", new Vector4(skyParameters.exposure, skyParameters.multiplier, skyParameters.rotation, 0.0f));
            m_RenderSkyPropertyBlock.SetMatrix("_InvViewProjMatrix", Utilities.GetViewProjectionMatrix(camera).inverse);

            var cmd = new CommandBuffer { name = "" };
            cmd.DrawMesh(skyMesh, Matrix4x4.identity, m_SkyHDRIMaterial, 0, 0, m_RenderSkyPropertyBlock);
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        private void RenderSkyToCubemap(SkyParameters skyParameters, RenderTexture target, RenderLoop renderLoop)
        {
            Shader.DisableKeyword("PERFORM_SKY_OCCLUSION_TEST");

            for (int i = 0; i < 6; ++i)
            {
                Utilities.SetRenderTarget(renderLoop, target, 0, (CubemapFace)i);
                Camera faceCamera = m_CubemapFaceCamera[i].GetComponent<Camera>();
                RenderSky(faceCamera, skyParameters, true, renderLoop);
            }
        }

        private void RenderCubemapGGXConvolution(Texture input, RenderTexture target, RenderLoop renderLoop)
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
                // All parameters are neutral because exposure/multiplier have already been applied in the first copy.
                SkyParameters skyParams = new SkyParameters();
                skyParams.exposure = 0.0f;
                skyParams.multiplier = 1.0f;
                skyParams.rotation = 0.0f;
                skyParams.skyHDRI = input;
                RenderSkyToCubemap(skyParams, target, renderLoop);

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
                        Utilities.SetRenderTarget(renderLoop, target, mip, (CubemapFace)face);
                        Camera faceCamera = m_CubemapFaceCamera[face].GetComponent<Camera>();

                        Mesh skyMesh = BuildSkyMesh(faceCamera, true);

                        var cmd = new CommandBuffer { name = "" };
                        cmd.DrawMesh(skyMesh, Matrix4x4.identity, m_GGXConvolveMaterial, 0, 0, propertyBlock);
                        renderLoop.ExecuteCommandBuffer(cmd);
                        cmd.Dispose();
                    }
                }

            }
        }

        public void RenderSky(Camera camera, SkyParameters skyParameters, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, RenderLoop renderLoop)
        {
            using (new Utilities.ProfilingSample("Sky Pass", renderLoop))
            {
                //using (new EditorGUI.DisabledScope(m_LookDevEnvLibrary.hdriList.Count <= 1))
                if (IsSkyValid(skyParameters))
                {
                    // When loading RenderDoc, RenderTextures will go null
                    RebuildTextures();

                    using (new Utilities.ProfilingSample("Sky Pass: Render Cubemap", renderLoop))
                    {
                        // Render sky into a cubemap - doesn't happen every frame, can be controlled
                        RenderSkyToCubemap(skyParameters, m_SkyboxCubemapRT, renderLoop);
                        // Convolve downsampled cubemap
                        RenderCubemapGGXConvolution(m_SkyboxCubemapRT, m_SkyboxGGXCubemapRT, renderLoop);

                        // TODO: Properly send the cubemap to Enlighten. Currently workaround is to set the cubemap in a Skybox/cubemap material
                        m_StandardSkyboxMaterial.SetTexture("_Tex", m_SkyboxCubemapRT);
                        RenderSettings.skybox = m_StandardSkyboxMaterial; // Setup this material as the default to be use in RenderSettings
                        RenderSettings.ambientIntensity = 1.0f; // fix this to 1, this parameter should not exist!
                        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox; // Force skybox for our HDRI
                        RenderSettings.reflectionIntensity = 1.0f;
                        RenderSettings.customReflection = null;
                        //DynamicGI.UpdateEnvironment();
                    }

                    // Render the sky itself
                    Utilities.SetRenderTarget(renderLoop, colorBuffer, depthBuffer);
                    RenderSky(camera, skyParameters, false, renderLoop);
                }
            }
        }
    }
}
