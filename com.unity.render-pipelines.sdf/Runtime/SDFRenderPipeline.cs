using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.SDFRP
{
    public static class Utilities
    {
        static Mesh s_FullscreenMesh = null;
        /// <summary>
        /// Returns a mesh that you can use with <see cref="CommandBuffer.DrawMesh(Mesh, Matrix4x4, Material)"/> to render full-screen effects.
        /// </summary>
        public static Mesh fullscreenMesh
        {
            get
            {
                if (s_FullscreenMesh != null)
                    return s_FullscreenMesh;

                float topV = 1.0f;
                float bottomV = 0.0f;

                s_FullscreenMesh = new Mesh { name = "Fullscreen Quad" };
                s_FullscreenMesh.SetVertices(new List<Vector3>
                {
                    new Vector3(-1.0f, -1.0f, 0.0f),
                    new Vector3(-1.0f,  1.0f, 0.0f),
                    new Vector3(1.0f, -1.0f, 0.0f),
                    new Vector3(1.0f,  1.0f, 0.0f)
                });

                s_FullscreenMesh.SetUVs(0, new List<Vector2>
                {
                    new Vector2(0.0f, bottomV),
                    new Vector2(0.0f, topV),
                    new Vector2(1.0f, bottomV),
                    new Vector2(1.0f, topV)
                });

                s_FullscreenMesh.SetIndices(new[] { 0, 1, 2, 2, 1, 3 }, MeshTopology.Triangles, 0, false);
                s_FullscreenMesh.UploadMeshData(true);
                return s_FullscreenMesh;
            }
        }
    }

    [ExecuteInEditMode]
    public class SDFRenderPipeline : RenderPipeline
    {
        internal static SDFRenderPipelineAsset currentAsset
                => GraphicsSettings.currentRenderPipeline is SDFRenderPipelineAsset sdfAsset ? sdfAsset : null;

        Material m_DepthOfFieldMaterial = null;

        bool m_isGIResourcesCreated = false;
        RenderTexture m_ProbeAtlasTexture = null;
        #region DEBUG_ONLY
        RenderTexture gi_mockRT = null;
        #endregion

        static int Frame = 0;

        private SDFSceneData m_SdfSceneData;
        private SDFRayMarch m_SdfRayMarch;

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            if (currentAsset.enableGI && !m_isGIResourcesCreated)// || m_needToRecreateGIResources))
                CreateGIResources(); // TODO - we should move this into a setup function

            ClearBackground(context, cameras);
            foreach (Camera camera in cameras)
            {
                SDFCameraData cameraData = new SDFCameraData();
                cameraData.InitializeCameraData(camera);

                CommandBuffer cmd = new CommandBuffer();
                cmd.name = "Camera Setup";
                cmd.SetViewport(camera.pixelRect);
                cmd.SetRenderTarget(camera.targetTexture);
                cmd.SetViewMatrix(camera.worldToCameraMatrix);
                cmd.SetProjectionMatrix(camera.projectionMatrix);
                cameraData.UpdateGlobalShaderVariables(cmd);
                context.ExecuteCommandBuffer(cmd);
                cmd.Release();

                SDFRenderer[] SDFObjects = GameObject.FindObjectsOfType<SDFRenderer>();
                if (SDFObjects.Length > 0)
                {
                    // GI Probe Update
                    // TODO - RSM should execute before this and for GI we need a full scene bounding box list
                    if (currentAsset.enableGI)
                    {
                        if (camera.cameraType == CameraType.Game && camera.enabled)
                        {
                            SDFGIProbeUpdateData giProbeUpdateData = new SDFGIProbeUpdateData();
                            giProbeUpdateData.InitializeGIProbeUpdateData(currentAsset, m_ProbeAtlasTexture);

                            CommandBuffer cmdGIProbeUpdate = new CommandBuffer();
                            cmdGIProbeUpdate.name = "GIProbeUpdate";
                            cmdGIProbeUpdate.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);

                            // TODO - giProbeUpdateData.SetupRSMInput(...);
                            giProbeUpdateData.UpdateComputeShaderVariables(cmdGIProbeUpdate, currentAsset.gatherIrradianceCS);                          

                            SDFRayMarch.RayMarchUpdateGIProbe(cmdGIProbeUpdate, currentAsset.gatherIrradianceCS, currentAsset.probeResolution);
                           
                            context.ExecuteCommandBufferAsync(cmdGIProbeUpdate, ComputeQueueType.Default);
                            cmdGIProbeUpdate.Release();
                        }
                    }

                    GetDataFromSceneGraph(SDFObjects, camera.pixelRect);
                    CreateObjectList(context, camera, SDFObjects.Length);

                    // SDF Rendering
                    {
                        if (camera.cameraType == CameraType.Game && camera.enabled)
                        {
                            CommandBuffer cmdRayMarch = new CommandBuffer();
                            cmdRayMarch.name = "RayMarch";
                            cameraData.UpdateComputeShaderVariables(cmdRayMarch, currentAsset.rayMarchingCS);

                            if (m_SdfRayMarch == null) // TODO: or if resolution has changed
                            {
                                m_SdfRayMarch = new SDFRayMarch(camera.pixelRect);
                            }
                            m_SdfRayMarch.RayMarch(cmdRayMarch, currentAsset.rayMarchingCS, m_SdfSceneData);

                            context.ExecuteCommandBuffer(cmdRayMarch);
                            cmdRayMarch.Release();
                        }
                    }
                    
                    // GI Shading
                    // TODO - reuse full scene object bounding box list
                    if (currentAsset.enableGI)
                    {
                        if (camera.cameraType == CameraType.Game && camera.enabled)
                        {
                            SDFGIShadingData giShadingData = new SDFGIShadingData();
                            giShadingData.InitializeGIShadingData(currentAsset, m_ProbeAtlasTexture);

                            CommandBuffer cmdGIShading = new CommandBuffer();
                            cmdGIShading.name = "GIShading";

                            #region DEBUG_ONLY
                            if (gi_mockRT == null)
                            {
                                gi_mockRT = new RenderTexture((int)camera.pixelRect.width, (int)camera.pixelRect.height, 1, RenderTextureFormat.ARGBHalf);
                                gi_mockRT.enableRandomWrite = true;
                                gi_mockRT.Create();
                            }
                            #endregion

                            // TODO - hook up screen space color, t-value and normal input
                            giShadingData.SetupScreenSpaceInput(null, null, gi_mockRT);
                            giShadingData.UpdateComputeShaderVariables(cmdGIShading, currentAsset.giShadingCS);

                            SDFRayMarch.RayMarchGIShading(cmdGIShading, currentAsset.giShadingCS, camera, gi_mockRT);

                            context.ExecuteCommandBuffer(cmdGIShading);
                            cmdGIShading.Release();
                        }
                    }
                }

                if (currentAsset.EnableDepthOfField)
                {
                    if (m_DepthOfFieldMaterial == null)
                    {
                        m_DepthOfFieldMaterial = new Material(Shader.Find("Hidden/SDFRP/DepthOfField"));
                    }
                    if (camera.cameraType == CameraType.Game && camera.enabled)
                    {

                        CommandBuffer cmdDOF = new CommandBuffer();
                        cmdDOF.name = "DepthOfField";

                        cmdDOF.SetGlobalColor("BackgroundColor", currentAsset.clearColor);
                        cmdDOF.SetGlobalInt("lensRes", currentAsset.lensRes);
                        cmdDOF.SetGlobalFloat("lensDis", camera.nearClipPlane);
                        cmdDOF.SetGlobalFloat("focalDis", currentAsset.focalDis);
                        cmdDOF.SetGlobalFloat("lensSiz", currentAsset.lensSiz);
                        cmdDOF.DrawMesh(Utilities.fullscreenMesh, Matrix4x4.identity, m_DepthOfFieldMaterial);
                        context.ExecuteCommandBuffer(cmdDOF);
                        cmdDOF.Release();
                    }
                }
            }
            context.Submit();
            Frame++;
        }

        private void ClearBackground(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (Camera camera in cameras)
            {
                CommandBuffer cmd1 = new CommandBuffer();
                if (camera.cameraType == CameraType.Preview)
                {
                    Debug.LogError(camera.pixelRect);
                }
                cmd1.SetViewport(camera.pixelRect);
                cmd1.SetRenderTarget(camera.targetTexture);
                cmd1.ClearRenderTarget(false, true, currentAsset.clearColor);
                context.ExecuteCommandBuffer(cmd1);
                cmd1.Release();
            }
        }

        private void GetDataFromSceneGraph(SDFRenderer[] SDFObjects, Rect pixelRect)
        {
            if (m_SdfSceneData == null) // TODO: assuming fixed numebr of objects in scene for now
            {
                int sdfDataSize = 0;
                foreach (SDFRenderer renderer in SDFObjects)
                {
                    sdfDataSize += renderer.SDFFilter.VoxelField.m_Field.Length;
                }

                m_SdfSceneData = new SDFSceneData(SDFObjects.Length, sdfDataSize, pixelRect);
            }

            // Fill out array of data and array of data-headers
            int offset = 0;
            for(int i = 0; i < SDFObjects.Length; i++)
            {
                VoxelField field = SDFObjects[i].SDFFilter.VoxelField;

                m_SdfSceneData.objectHeaders[i].worldToObjMatrix = SDFObjects[i].gameObject.transform.worldToLocalMatrix; // may not work with shader according to docs?
                m_SdfSceneData.objectHeaders[i].objID = i; // index into data. Change later?
                m_SdfSceneData.objectHeaders[i].numEntries = field.m_Field.Length;
                m_SdfSceneData.objectHeaders[i].startOffset = offset;
                m_SdfSceneData.objectHeaders[i].voxelSize = field.m_VoxelSize;
                Vector3 minExtent = field.MeshBounds.center - 0.5f * field.MeshBounds.size; // is this correct? Can we just pass the counts instead?
                m_SdfSceneData.objectHeaders[i].minExtentX = minExtent.x;
                m_SdfSceneData.objectHeaders[i].minExtentY = minExtent.y;
                m_SdfSceneData.objectHeaders[i].minExtentZ = minExtent.z;
                Vector3 maxExtent = field.MeshBounds.center + 0.5f * field.MeshBounds.size;
                m_SdfSceneData.objectHeaders[i].maxExtentX = maxExtent.x;
                m_SdfSceneData.objectHeaders[i].maxExtentY = maxExtent.y;
                m_SdfSceneData.objectHeaders[i].maxExtentZ = maxExtent.z;
                //m_SdfSceneData.objectHeaders[i].color = SDFObjects[i].SDFMaterial.color;

                Array.Copy(field.m_Field, 0, m_SdfSceneData.sdfData, offset, field.m_Field.Length);
                offset += field.m_Field.Length;
            }

            // Update compute buffers
            m_SdfSceneData.SetObjectHeaderData();
            m_SdfSceneData.SetSDFData();
        }

        private void CreateObjectList(ScriptableRenderContext context, Camera camera, int totalSDFs)
        {
            Material material;

            Vector3[] cubeVertices =
            {
                new Vector3(-0.5f,-0.5f,-0.5f),
                new Vector3(-0.5f,-0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f,-0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f,-0.5f,-0.5f),
                new Vector3(0.5f,-0.5f, 0.5f),
                new Vector3(0.5f, 0.5f,-0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
            };
            int[] cubeIndices =
            {
                0, 1, 3,
                6, 0, 2,
                5, 0, 4,
                6, 4, 0,
                0, 3, 2,
                5, 1, 0,
                3, 1, 5,
                7, 4, 6,
                4, 7, 5,
                7, 6, 2,
                7, 2, 3,
                7, 3, 5
            };

            Shader shader = Shader.Find("Hidden/Internal-Colored");
            material = new Material(shader);
            material.SetColor("_Color", Color.red);
            material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);

            // var buffer = new ...
            // material.SetBuffer(...)
            // cmd.SetRandomWriteTarget(...)

            var mesh = new Mesh();
            mesh.SetVertices(cubeVertices, 0, 8);
            mesh.SetIndices(cubeIndices, MeshTopology.Triangles, 0);

            CommandBuffer cmd1 = new CommandBuffer();
            if (camera.cameraType == CameraType.Preview)
            {
                Debug.LogError(camera.pixelRect);
            }
            cmd1.SetViewport(camera.pixelRect);

            SDFSceneData.ObjectHeader[] data = m_SdfSceneData.objectHeaders;
            for (int i = 0; i < data.Length; i++)
            {
                Vector3 minExtent = new Vector3(data[i].minExtentX, data[i].minExtentY, data[i].minExtentZ);
                Vector3 maxExtent = new Vector3(data[i].maxExtentX, data[i].maxExtentY, data[i].maxExtentZ);
                var extents = maxExtent - minExtent;
                Matrix4x4 scale = Matrix4x4.Scale(extents);
                Matrix4x4 finalTRS = data[i].worldToObjMatrix.inverse * scale;       // Check multiply scale correctness later

                cmd1.DrawMesh(mesh, finalTRS, material, 0, 0);
            }
            context.ExecuteCommandBuffer(cmd1);
            cmd1.Release();
        }

        private void CreateGIResources()
        {
            Debug.Assert(currentAsset.gridSize.x >= 0 && currentAsset.gridSize.y >= 0 && currentAsset.gridSize.z >= 0);
            Debug.Assert(currentAsset.probeDistance.x >= 0 && currentAsset.probeDistance.y >= 0 && currentAsset.probeDistance.z >= 0);

            Debug.Assert(m_ProbeAtlasTexture == null);

            RenderTextureDescriptor rtDesc = new RenderTextureDescriptor();
            rtDesc.autoGenerateMips = false;
            rtDesc.enableRandomWrite = true;
            rtDesc.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            rtDesc.width = currentAsset.probeAtlasTextureResolution;
            rtDesc.height = currentAsset.probeAtlasTextureResolution;
            rtDesc.volumeDepth = 1;
            rtDesc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
            rtDesc.msaaSamples = 1;

            m_ProbeAtlasTexture = new RenderTexture(rtDesc);
            m_ProbeAtlasTexture.Create();

            // Create other resources here

            m_isGIResourcesCreated = true;
        }
    }
}
