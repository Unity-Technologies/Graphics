using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    [Serializable]
    public class SkyParameters
    {
        public Cubemap skyHDRI;
        public float rotation = 0.0f;
        public float exposure = 0.0f;
        public float multiplier = 1.0f;
    }

    public class SkyRenderer
    {
        const int kSkyCubemapSize = 256;

        RenderTexture m_SkyboxCubemapRT = null;

        Material m_StandardSkyboxMaterial = null; // This is the Unity standard skybox material. Used to pass the correct cubemap to Enlighten.
        Material m_SkyHDRIMaterial = null; // Renders a cubemap into a render texture (can be cube or 2D)

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

        public void Rebuild()
        {
            // TODO: We need to have an API to send our sky information to Enlighten. For now use a workaround through skybox/cubemap material...
            m_StandardSkyboxMaterial = Utilities.CreateEngineMaterial("Skybox/Cubemap");

            m_SkyHDRIMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderLoop/SkyHDRI");

            m_SkyboxCubemapRT = new RenderTexture(kSkyCubemapSize, kSkyCubemapSize, 1, RenderTextureFormat.ARGBHalf);
            m_SkyboxCubemapRT.dimension = TextureDimension.Cube;
            m_SkyboxCubemapRT.useMipMap = true;
            m_SkyboxCubemapRT.autoGenerateMips = true;
            m_SkyboxCubemapRT.Create();

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
            Utilities.Destroy(m_SkyboxCubemapRT);

            for(int i = 0 ; i < 6 ; ++i)
            {
                Utilities.Destroy(m_CubemapFaceCamera[i]);
            }

        }

        private void RenderSky(Camera camera, SkyParameters skyParameters, bool forceUVBottom, RenderLoop renderLoop)
        {
            Mesh skyMesh = BuildSkyMesh(camera, forceUVBottom);

            m_SkyHDRIMaterial.SetTexture("_Cubemap", skyParameters.skyHDRI);
            m_SkyHDRIMaterial.SetVector("_SkyParam", new Vector4(skyParameters.exposure, skyParameters.multiplier, skyParameters.rotation, 0.0f));

            var cmd = new CommandBuffer { name = "Skybox" };
            cmd.DrawMesh(skyMesh, Matrix4x4.identity, m_SkyHDRIMaterial);
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        public void RenderSky(Camera camera, SkyParameters skyParameters, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, RenderLoop renderLoop)
        {
            // Render sky into a cubemap - doesn't happen every frame, can be control
            for (int i = 0; i < 6; ++i)
            {
                Utilities.SetRenderTarget(renderLoop, m_SkyboxCubemapRT, "", 0, (CubemapFace)i);
                Camera faceCamera = m_CubemapFaceCamera[i].GetComponent<Camera>();
                RenderSky(faceCamera, skyParameters, true, renderLoop);
            }

            m_StandardSkyboxMaterial.SetTexture("_Tex", m_SkyboxCubemapRT);
            RenderSettings.skybox = m_StandardSkyboxMaterial; // Setup this material as the default to be use in RenderSettings
            RenderSettings.ambientIntensity = 1.0f; // fix this to 1, this parameter should not exist!
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox; // Force skybox for our HDRI
            RenderSettings.reflectionIntensity = 1.0f;
            DynamicGI.UpdateEnvironment();

            // TODO: do a render to texture here

            // Downsample the cubemap and provide it to Enlighten

            // TODO: currently workaround is to set the cubemap in a Skybox/cubemap material
            //m_SkyboxMaterial.SetTexture(cubemap);

            // Render the sky itself
            Utilities.SetRenderTarget(renderLoop, colorBuffer, depthBuffer, "Sky Pass");
            RenderSky(camera, skyParameters, false, renderLoop);
        }
    }
}
