using System;
using UnityEngine.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // NOTE: If we have to do a near plane, when recovering position we need to add back the near 


        // TODOs:
        //  - For each cell: 
        //  - Once cubemap is done store in pool.
        //  - Once pool is full process in a compute 
        //  - To retrieve data:
        //      * Undo the rotation done to skew away from corners on the diagonal axis
        //      * Sample in the 14 directions we want
        //      * Write to a buffer

        //  - Once the buffer is complete read back completely and reorganize to hit/misses like is done in old version.
        //  - Flag the data as reorganized on cell and store in cell (refVol.setExtraCellData(cellIdx, DATA) so we can blindly upload when we do not need a rebake.
        //      * NOTE: The cell index is just what is in the list and is the same obtained alongside the positions, a transient thing. 
        //  - When reorganized, re upload.

        // ON THE REFERENCE VOLUME:
        //   - Prepare extra data locally and just do a set of the data.
        //   - Move all the buffer processing to HDRP and here.
        //   - Move the irradiance cache stuff here indexed by cell index? or keep on cell? <<< LATER FOR CLEANUP.
        //   - Get list <cellIndex, LISTOFPOSITIONS> 

        // TMP MOVE FROM HERE TODO
        RenderTargetIdentifier[] rts = new RenderTargetIdentifier[3];

        const int kPoolSize = 64;
        const float kExtraDataCameraNearPlane = 0.0001f;
        
        HDCamera CreateCameraForExtraData(float searchDistance)
        {
            var go = new GameObject("Probe Extra Data Camera");
            var camera = go.AddComponent<Camera>();
            camera.enabled = false;
            camera.cameraType = CameraType.Reflection;

            camera.farClipPlane = searchDistance + kExtraDataCameraNearPlane;

            return HDCamera.GetOrCreate(camera);
        }

        void ModifyCameraForProbe(HDCamera camera, Vector3 position, float searchDistance)
        {
            camera.camera.transform.position = position;
            camera.camera.nearClipPlane = kExtraDataCameraNearPlane;
            camera.camera.farClipPlane = searchDistance + kExtraDataCameraNearPlane;

            var additionalCameraData = HDUtils.TryGetAdditionalCameraDataOrDefault(camera.camera);
            if(!additionalCameraData.hasCustomRender)
                additionalCameraData.customRender += RenderCubeMapForExtraData;
        }

        void ModifyCameraPostProbeRendering(HDCamera camera)
        {
            var additionalCameraData = HDUtils.TryGetAdditionalCameraDataOrDefault(camera.camera);
            if (additionalCameraData.hasCustomRender) 
            additionalCameraData.customRender -= RenderCubeMapForExtraData;
        }


        public void GetProbeVolumeExtraData()
        {
            var refVol = ProbeReferenceVolume.instance;
            var positionsArrays = refVol.GetProbeLocations();

            HDCamera cameraForGen = CreateCameraForExtraData(refVol.MinDistanceBetweenProbes() + kExtraDataCameraNearPlane);

            // dummy, not really needed
            RTHandle dummyRT = RTHandles.Alloc(ProbeDynamicGIExtraDataManager.instance.GetCubeMapSize(), ProbeDynamicGIExtraDataManager.instance.GetCubeMapSize(), dimension: TextureDimension.Cube, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite: true, name: "Dummy output");

            var buffers = refVol.GetExtraDataBuffers();
            for (int i=0; i< buffers.Count; ++i)
            {
                var bufferSet = buffers[i];
                int probeCount = bufferSet.probeCount;

                int currentBatchIndex = 0;
                for (int probe = 0; probe < probeCount; ++probe)
                {
                    // TODO: This will change if the min distance changes per probe area.
                    float diagDist = Mathf.Sqrt(3.0f);
                    float distanceToSearch = refVol.MinDistanceBetweenProbes() * diagDist + kExtraDataCameraNearPlane;
                    Vector3 position = bufferSet.GetPosition(probe);
                    ModifyCameraForProbe(cameraForGen, position, distanceToSearch);
                    HDRenderUtilities.Render(cameraForGen, dummyRT.rt);
                    ModifyCameraPostProbeRendering(cameraForGen);

                    ProbeDynamicGIExtraDataManager.instance.MoveToNextExtraData();
                    currentBatchIndex++;
                    if (currentBatchIndex > 63)
                    {
                        // TODO: EXTRACT.
                    }
                }
            }

            CoreUtils.Destroy(cameraForGen.camera.gameObject);
            RTHandles.Release(dummyRT);
        }

        private void RenderCubeMapForExtraData(ScriptableRenderContext renderContext, HDCamera camera)
        {
            InitializeGlobalResources(renderContext);

            // TODO: CULLING?! Maybe cull once outside of the loop with the width of a cell and do culling once per cell?
            // Could be too expensive here...

            var cmd = CommandBufferPool.Get("");

            var hdCamera = camera;
            m_CurrentHDCamera = hdCamera;


            hdCamera.BeginRender(cmd);
            Resize(hdCamera);

            var mat = Matrix4x4.Rotate(Quaternion.Euler(40.0f, 40.0f, 40.0f));
            hdCamera.camera.worldToCameraMatrix *= mat;
            hdCamera.UpdateAllViewConstants(false);


            hdCamera.UpdateShaderVariablesGlobalCB(ref m_ShaderVariablesGlobalCB);
            ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesGlobalCB, HDShaderIDs._ShaderVariablesGlobal);

            CullingResults cullingResults;
            ScriptableCullingParameters cullingParams = default;
            hdCamera.camera.TryGetCullingParameters(false, out cullingParams);
            cullingParams.cullingOptions |= CullingOptions.DisablePerObjectCulling;

            cullingResults = renderContext.Cull(ref cullingParams);

            RendererListDesc renderListDesc = new RendererListDesc(HDShaderPassNames.s_DynamicGIDataGenName, cullingResults, hdCamera.camera)
            {
                rendererConfiguration = 0,
                renderQueueRange = HDRenderQueue.k_RenderQueue_AllOpaque,
                sortingCriteria = SortingCriteria.CommonOpaque,
                stateBlock = null,
                overrideMaterial = null,
                excludeObjectMotionVectors = false
            };
            
            var extraDataRTs = ProbeDynamicGIExtraDataManager.instance.GetCurrentExtraDataDestination();

            if (extraDataRTs.albedo == null)
            {
                extraDataRTs.Allocate();
            }

            int face = ProbeDynamicGIExtraDataManager.instance.GetCurrentSlice();
            rts[0] = extraDataRTs.albedo;
            rts[1] = extraDataRTs.normal;
            rts[2] = extraDataRTs.depth;

            var depthBuffer = ProbeDynamicGIExtraDataManager.instance.GetDepthBuffer();

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderForwardError)))
            {
                cmd.SetRenderTarget(rts, depthBuffer, 0, (CubemapFace)face, 0);
                CoreUtils.ClearRenderTarget(cmd, ClearFlag.All, Color.clear);

                CoreUtils.DrawRendererList(renderContext, cmd, RendererList.Create(renderListDesc));
            }
            renderContext.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }


        // ---------------------------------------------------------------------
        // -------------------------- Extract Data -----------------------------
        // ---------------------------------------------------------------------
        // This pass extracts data from cubemap to the format we expect them and pack 
        // directly into the output buffer used at runtime.


        void ExtractDataFromExtraDataPool(ProbeExtraDataBuffers bufferSet, int bufferOffsetStart, int batchSize)
        { }


    }
}
