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

        // TODO - move these into a separate class like SDFSceneData
        private bool m_isGIResourcesCreated = false;
        private RenderTexture m_ProbeAtlasTexture = null;
        private ComputeBuffer m_RSMSamplePointsBuffer = null;
        #region DEBUG_ONLY
        public static RenderTexture gi_mockRT = null;
        #endregion

        private SDFGenerateRSMData m_SdfGenerateRSMData;

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
                    if (true)//(camera.cameraType == CameraType.Game && camera.enabled)
                    {
                        if (m_SdfRayMarch == null) // TODO: or if resolution has changed
                        {
                            m_SdfRayMarch = new SDFRayMarch(camera.pixelRect);
                        }

                        GetDataFromSceneGraph(SDFObjects, camera.pixelRect);
                        CreateObjectList(context, camera, SDFObjects.Length);
                        
                        // Get sun light properties to pass to RSM and use for direct lighting
                        Light directionalLight = RenderSettings.sun;
                        // For direct lighting, can use transform.forward
                        // Vector3 lightDir = directionalLight.transform.forward;
                         
                        // Generate RSM for main directional light
                        if (currentAsset.enableGI)
                        {
                            CommandBuffer cmdGenerateRSM = new CommandBuffer();
                            cmdGenerateRSM.name = "GenerateRSM";

                            if (m_SdfGenerateRSMData == null)
                            {
                                m_SdfGenerateRSMData = new SDFGenerateRSMData();
                            }
                            m_SdfGenerateRSMData.SetupGenerateRSMData(camera, directionalLight);
                            m_SdfGenerateRSMData.UpdateComputeShaderVariables(cmdGenerateRSM, currentAsset.generateRSMCS);                          

                            m_SdfRayMarch.RayMarchRSM(cmdGenerateRSM, currentAsset.generateRSMCS, m_SdfSceneData, m_SdfGenerateRSMData);

                            context.ExecuteCommandBuffer(cmdGenerateRSM);
                            cmdGenerateRSM.Release();
                        }

                        // GI Probe Update
                        // TODO - for GI we need a full scene bounding box list
                        if (currentAsset.enableGI)
                        {
                            SDFGIProbeUpdateData giProbeUpdateData = new SDFGIProbeUpdateData();
                            giProbeUpdateData.InitializeGIProbeUpdateData(currentAsset, m_ProbeAtlasTexture, m_RSMSamplePointsBuffer);

                            CommandBuffer cmdGIProbeUpdate = new CommandBuffer();
                            cmdGIProbeUpdate.name = "GIProbeUpdate";
                            cmdGIProbeUpdate.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);

                            giProbeUpdateData.SetupRSMInput(m_SdfGenerateRSMData.m_RSMFluxTexture, m_SdfGenerateRSMData.m_RSMNormalTexture, m_SdfGenerateRSMData.m_RSMDistanceTexture, m_SdfGenerateRSMData.m_RSMProjectionMatrix);
                            giProbeUpdateData.UpdateComputeShaderVariables(cmdGIProbeUpdate, currentAsset.gatherIrradianceCS);

                            m_SdfRayMarch.RayMarchUpdateGIProbe(cmdGIProbeUpdate, currentAsset.gatherIrradianceCS, currentAsset.probeResolution);
                           
                            context.ExecuteCommandBufferAsync(cmdGIProbeUpdate, ComputeQueueType.Default);
                            cmdGIProbeUpdate.Release();
                        }


                        // SDF Rendering
                        {
                            CommandBuffer cmdRayMarch = new CommandBuffer();
                            cmdRayMarch.name = "RayMarch";
                            cameraData.UpdateComputeShaderVariables(cmdRayMarch, currentAsset.rayMarchingCS);
                            SDFLightData lightData = new SDFLightData();
                            lightData.InitializeLightData(m_SdfSceneData.directionalLight);
                            lightData.UpdateComputeShaderVariables(cmdRayMarch, currentAsset.rayMarchingCS);
                            m_SdfRayMarch.RayMarch(cmdRayMarch, currentAsset.rayMarchingCS, m_SdfSceneData, currentAsset.DebugOutputValue);

                            context.ExecuteCommandBuffer(cmdRayMarch);
                            cmdRayMarch.Release();
                        }

                        // GI Shading
                        // TODO - reuse full scene object bounding box list
                        if (currentAsset.enableGI)
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
                            giShadingData.SetupScreenSpaceInput(m_SdfRayMarch.outSdfData, null, gi_mockRT);
                            giShadingData.UpdateComputeShaderVariables(cmdGIShading, currentAsset.giShadingCS);

                            m_SdfRayMarch.RayMarchGIShading(cmdGIShading, currentAsset.giShadingCS, camera);

                            context.ExecuteCommandBuffer(cmdGIShading);
                            cmdGIShading.Release();
                        }

                        if (currentAsset.EnableDepthOfField)
                        {
                            if (m_DepthOfFieldMaterial == null)
                            {
                                m_DepthOfFieldMaterial = new Material(Shader.Find("Hidden/SDFRP/DepthOfField"));
                            }
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
                    //else
                    {
                       // GetDataFromSceneGraph(SDFObjects, camera.pixelRect);
                        //CreateObjectList(context, camera, SDFObjects.Length);
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

        private bool NeedToCreateSdfSceneData(SDFRenderer[] SDFObjects, Light light, out int[] SDFObjectIDs)
        {
            SDFObjectIDs = new int[SDFObjects.Length];
            for (int i = 0; i < SDFObjects.Length; ++i)
            {
                SDFObjectIDs[i] = SDFObjects[i].gameObject.GetInstanceID();
            }

            if (m_SdfSceneData == null)
                return true;

            if (SDFObjects.Length != m_SdfSceneData.SDFObjectIDs.Length)
                return true;

            // Sadly the ordering of 'FindObjectsOfType' is not guaranteed
            Array.Sort(SDFObjectIDs);

            for (int i = 0; i < SDFObjects.Length; ++i)
            {
                if (SDFObjectIDs[i] != m_SdfSceneData.SDFObjectIDs[i])
                    return true;
            }
            // TODO wouldn't pick up changes if light properties are changed
            if (light.gameObject.GetInstanceID() != m_SdfSceneData.directionalLight.gameObject.GetInstanceID())
                return true;

            return false;
        }

        private void GetDataFromSceneGraph(SDFRenderer[] SDFObjects, Rect pixelRect)
        {
            Light light = GameObject.FindObjectOfType<Light>();
            Debug.Assert(light.type == LightType.Directional);

            bool sdfSceneDataChanged = NeedToCreateSdfSceneData(SDFObjects, light, out int[] SDFObjectIDs);
            if (sdfSceneDataChanged)
            {
                m_SdfSceneData?.Dispose();

                int sdfDataSize = 0;
                int normalsSize = 0;
                foreach (SDFRenderer renderer in SDFObjects)
                {
                    sdfDataSize += renderer.SDFFilter.VoxelField.m_Field.Length;
                    normalsSize += renderer.SDFFilter.VoxelField.m_Normals.Length;
                }

                m_SdfSceneData = new SDFSceneData(SDFObjectIDs, sdfDataSize, normalsSize, pixelRect, light);
            }

            // Fill out array of data and array of data-headers
            int offset = 0;
            int normalsOffset = 0;
            for(int i = 0; i < SDFObjects.Length; i++)
            {
                VoxelField field = SDFObjects[i].SDFFilter.VoxelField;

                m_SdfSceneData.objectHeaders[i].worldToObjMatrix = SDFObjects[i].gameObject.transform.worldToLocalMatrix; // may not work with shader according to docs?
                m_SdfSceneData.objectHeaders[i].objToWorldMatrix = SDFObjects[i].gameObject.transform.localToWorldMatrix;
                m_SdfSceneData.objectHeaders[i].color = SDFObjects[i].SDFMaterial.color;
                m_SdfSceneData.objectHeaders[i].objID = i; // index into data. Change later?
                m_SdfSceneData.objectHeaders[i].numEntries = field.m_Field.Length;
                m_SdfSceneData.objectHeaders[i].startOffset = offset;
                m_SdfSceneData.objectHeaders[i].normalsOffset = normalsOffset;
                Vector3 minExtent = field.MeshBounds.center - 0.5f * field.MeshBounds.size; // is this correct? Can we just pass the counts instead?
                m_SdfSceneData.objectHeaders[i].minExtentX = minExtent.x;
                m_SdfSceneData.objectHeaders[i].minExtentY = minExtent.y;
                m_SdfSceneData.objectHeaders[i].minExtentZ = minExtent.z;
                Vector3 maxExtent = field.MeshBounds.center + 0.5f * field.MeshBounds.size;
                m_SdfSceneData.objectHeaders[i].maxExtentX = maxExtent.x;
                m_SdfSceneData.objectHeaders[i].maxExtentY = maxExtent.y;
                m_SdfSceneData.objectHeaders[i].maxExtentZ = maxExtent.z;
                m_SdfSceneData.objectHeaders[i].voxelDimensionsX = field.m_VoxelCountX;
                m_SdfSceneData.objectHeaders[i].voxelDimensionsY = field.m_VoxelCountY;
                m_SdfSceneData.objectHeaders[i].voxelDimensionsZ = field.m_VoxelCountZ;
                m_SdfSceneData.objectHeaders[i].voxelSize = field.m_VoxelSize;

                if (sdfSceneDataChanged)
                {
                    Array.Copy(field.m_Field, 0, m_SdfSceneData.sdfData, offset, field.m_Field.Length);
                    Array.Copy(field.m_Normals, 0, m_SdfSceneData.normals, normalsOffset, field.m_Normals.Length);
                }

                offset += field.m_Field.Length;
                normalsOffset += field.m_Normals.Length;
            }

            // Update compute buffers
            m_SdfSceneData.UpdateObjectHeaderComputeBuffer();
            if (sdfSceneDataChanged)
            {
                m_SdfSceneData.UpdateSDFComputeBuffer();
                m_SdfSceneData.UpdateNormalsComputeBuffer();
            }
        }

        private void CreateObjectList(ScriptableRenderContext context, Camera camera, int totalSDFs)
        {
            // TO DO: Don't always clear if no change?
            Array.Clear(m_SdfSceneData.tileDataOffsetIntoObjHeaderValues, 0, m_SdfSceneData.tileDataOffsetIntoObjHeaderValues.Length);
            Array.Clear(m_SdfSceneData.tileHeaders, 0, m_SdfSceneData.tileHeaders.Length);

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

            var mesh = new Mesh();
            mesh.SetVertices(cubeVertices, 0, 8);
            mesh.SetIndices(cubeIndices, MeshTopology.Triangles, 0);

            CommandBuffer cmd1 = new CommandBuffer();
            Shader shader = currentAsset.tileCullingShader;
            Material material = new Material(shader);
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            // material.SetColor("_Color", Color.red);

            material.SetBuffer("_TileFlagsData", m_SdfSceneData.tileFlagsComputeBuffer);
            cmd1.SetRandomWriteTarget(1, m_SdfSceneData.tileFlagsComputeBuffer);

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
                propertyBlock.SetInt("_SdfID", i);
                // Debug.Log("--------------DrawMesh with sdf id " + i);
                cmd1.DrawMesh(mesh, finalTRS, material, 0, 0, propertyBlock);
            }

            // cmd1.ClearRandomWriteTargets();
            // int compressTileDataKernel = currentAsset.tileDataCompressionShader.FindKernel("CompressTileDataKernel");
            // cmd1.SetComputeBufferParam(currentAsset.tileDataCompressionShader, compressTileDataKernel, Shader.PropertyToID("_TileFlagsData"), m_SdfSceneData.tileFlagsComputeBuffer);
            // cmd1.SetComputeBufferParam(currentAsset.tileDataCompressionShader, compressTileDataKernel, Shader.PropertyToID("_TileDataHeader"), m_SdfSceneData.tileHeaderComputeBuffer);
            // cmd1.SetComputeBufferParam(currentAsset.tileDataCompressionShader, compressTileDataKernel, Shader.PropertyToID("_TileDataOffsetIntoObjHeader"), m_SdfSceneData.tileOffsetsComputeBuffer);
            // cmd1.DispatchCompute(currentAsset.tileDataCompressionShader, compressTileDataKernel, m_SdfSceneData.numTilesX, m_SdfSceneData.numTilesY, 1);

            // edit Tile data and headers
            int[] tileFlags = new int[SDFRayMarch.MAX_OBJECTS_IN_SCENE * m_SdfSceneData.numTilesX * m_SdfSceneData.numTilesY];
            m_SdfSceneData.tileFlagsComputeBuffer.GetData(tileFlags);

            int curOffset = 0;
            for (int tile = 0; tile < m_SdfSceneData.numTilesX * m_SdfSceneData.numTilesY; tile++)
            {
                m_SdfSceneData.tileHeaders[tile].offset = curOffset;
                int offset = SDFRayMarch.MAX_OBJECTS_IN_SCENE * tile;
                for (int id = 0; id < SDFRayMarch.MAX_OBJECTS_IN_SCENE; id++)
                {
                    if (tileFlags[offset + id] == 1)
                    {
                        int newOffset = m_SdfSceneData.tileHeaders[tile].offset + m_SdfSceneData.tileHeaders[tile].numObjects;
                        m_SdfSceneData.tileDataOffsetIntoObjHeaderValues[newOffset] = id;
                        m_SdfSceneData.tileHeaders[tile].numObjects += 1;
                        // Debug.Log("Tile " + tile + " now has " + m_SdfSceneData.tileHeaders[tile].numObjects + " objects, newest is id " + id);
                        curOffset++;
                    }
                }
            }
            m_SdfSceneData.UpdateTileHeaderComputeBuffer();
            m_SdfSceneData.UpdateTileOffsetIntoObjHeaderComputeBuffer();
            Array.Clear(m_SdfSceneData.tileDataOffsetIntoObjHeaderValues, 0, m_SdfSceneData.tileDataOffsetIntoObjHeaderValues.Length);
            Array.Clear(m_SdfSceneData.tileHeaders, 0, m_SdfSceneData.tileHeaders.Length);
            m_SdfSceneData.tileFlagsComputeBuffer.SetData(m_SdfSceneData.tileDataOffsetIntoObjHeaderValues);

            context.ExecuteCommandBuffer(cmd1);
            cmd1.Release();

            // DEBUG TILE DATA PRINT OUT
            
            // m_SdfSceneData.tileHeaderComputeBuffer.GetData(m_SdfSceneData.tileHeaders);
            // m_SdfSceneData.tileOffsetsComputeBuffer.GetData(m_SdfSceneData.tileDataOffsetIntoObjHeaderValues);
            // for (int i = 0; i < m_SdfSceneData.numTilesX * m_SdfSceneData.numTilesY; i++)
            // {
            //     SDFSceneData.TileDataHeader tile = m_SdfSceneData.tileHeaders[i];
            //     string ids = "";
            //     for (int j = 0; j < tile.numObjects; j++)
            //     {
            //         ids += m_SdfSceneData.tileDataOffsetIntoObjHeaderValues[tile.offset + j] + ", ";
            //     }
            //     if (tile.numObjects > 0)
            //         Debug.Log("Tile " + i + " has " + tile.numObjects + " objects, ids: " + ids);
            // }

        }

        private void CreateGIResources()
        {
            Debug.Assert(currentAsset.gridSize.x >= 0 && currentAsset.gridSize.y >= 0 && currentAsset.gridSize.z >= 0);
            Debug.Assert(currentAsset.probeDistance.x >= 0 && currentAsset.probeDistance.y >= 0 && currentAsset.probeDistance.z >= 0);
            if (currentAsset.gridSize.x * currentAsset.gridSize.y * currentAsset.gridSize.z > 256)
                Debug.LogError("Currently you cannot have more than 256 GI probes!");

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


            Debug.Assert(m_RSMSamplePointsBuffer == null);

            int sampleCount = 192; // This affects quality & performance
            m_RSMSamplePointsBuffer = new ComputeBuffer(sampleCount * 2, sizeof(float), ComputeBufferType.Default); // Every sample point is a UV offset
            SDFGIProbeUpdateData.GenerateRSMSamplePoints(sampleCount, 0.49f, m_RSMSamplePointsBuffer);


            // Create other resources here

            m_isGIResourcesCreated = true;
        }
    }
}
