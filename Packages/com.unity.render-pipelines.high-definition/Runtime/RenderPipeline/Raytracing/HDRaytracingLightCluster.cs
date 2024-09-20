using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDRaytracingLightCluster
    {
        // External data
        HDRenderPipeline m_RenderPipeline = null;

        // Culling result
        GraphicsBuffer m_LightCullResult = null;

        // Output cluster data
        GraphicsBuffer m_LightCluster = null;

        // World light subset used for ray tracing
        WorldLightSubSet m_WorldLightSubSet = new WorldLightSubSet();

        // Light cluster debug material
        Material m_DebugMaterial = null;

        // String values
        const string m_LightClusterKernelName = "RaytracingLightCluster";
        const string m_LightCullKernelName = "RaytracingLightCull";

        public static readonly int _ClusterCellSize = Shader.PropertyToID("_ClusterCellSize");
        public static readonly int _LightVolumes = Shader.PropertyToID("_LightVolumes");
        public static readonly int _LightSubSet = Shader.PropertyToID("_LightSubSet");
        public static readonly int _LightVolumeCount = Shader.PropertyToID("_LightVolumeCount");
        public static readonly int _LightSubSetCount = Shader.PropertyToID("_LightSubSetCount");
        public static readonly int _DebugColorGradientTexture = Shader.PropertyToID("_DebugColorGradientTexture");
        public static readonly int _DebutLightClusterTexture = Shader.PropertyToID("_DebutLightClusterTexture");
        public static readonly int _RaytracingLightCullResult = Shader.PropertyToID("_RaytracingLightCullResult");
        public static readonly int _ClusterCenterPosition = Shader.PropertyToID("_ClusterCenterPosition");
        public static readonly int _ClusterDimension = Shader.PropertyToID("_ClusterDimension");
        public static readonly int _ClusterLightCategoryDebug = Shader.PropertyToID("_ClusterLightCategoryDebug");

        // Temporary variables
        // This value is now fixed for every HDRP asset
        int m_NumLightsPerCell = 0;

        // These values are overriden for every light cluster that is built
        Vector3 minClusterPos = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 maxClusterPos = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 clusterCellSize = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 clusterCenter = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 clusterDimension = new Vector3(0.0f, 0.0f, 0.0f);

        public void Initialize(HDRenderPipeline renderPipeline)
        {
            // Keep track of the render pipeline
            m_RenderPipeline = renderPipeline;

            // Allocate the light cluster buffer at the right size
            m_NumLightsPerCell = renderPipeline.asset.currentPlatformRenderPipelineSettings.lightLoopSettings.maxLightsPerClusterCell;
            int bufferSize = HDLightClusterDefinitions.s_ClusterCellCount * (m_NumLightsPerCell + HDLightClusterDefinitions.s_CellMetaDataSize);
            ResizeClusterBuffer(bufferSize);

            // Create the material required for debug
            m_DebugMaterial = CoreUtils.CreateEngineMaterial(renderPipeline.rayTracingResources.lightClusterDebugS);
        }

        public void ReleaseResources()
        {
            CoreUtils.SafeRelease(m_LightCluster);
            m_LightCluster = null;

            CoreUtils.SafeRelease(m_LightCullResult);
            m_LightCullResult = null;

            CoreUtils.Destroy(m_DebugMaterial);
            m_DebugMaterial = null;

            m_WorldLightSubSet.Release();
        }

        void ResizeClusterBuffer(int bufferSize)
        {
            // Release the previous buffer
            if (m_LightCluster != null)
            {
                // If it is not null and it has already the right size, we are pretty much done
                if (m_LightCluster.count == bufferSize)
                    return;

                CoreUtils.SafeRelease(m_LightCluster);
                m_LightCluster = null;
            }

            // Allocate the next buffer buffer
            if (bufferSize > 0)
            {
                m_LightCluster = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bufferSize, sizeof(uint));
            }
        }

        void ResizeCullResultBuffer(int numLights)
        {
            // Release the previous buffer
            if (m_LightCullResult != null)
            {
                // If it is not null and it has already the right size, we are pretty much done
                if (m_LightCullResult.count == numLights)
                    return;

                CoreUtils.SafeRelease(m_LightCullResult);
                m_LightCullResult = null;
            }

            // Allocate the next buffer buffer
            if (numLights > 0)
            {
                m_LightCullResult = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numLights, sizeof(uint));
            }
        }

        static internal Bounds GetLightClusterBounds(HDCamera hdCamera)
        {
            var settings = hdCamera.volumeStack.GetComponent<LightCluster>();

            Vector3 camPosWS = Vector3.zero;
            if (ShaderConfig.s_CameraRelativeRendering == 0)
                camPosWS = hdCamera.mainViewConstants.worldSpaceCameraPos;

            float range = settings.cameraClusterRange.value;
            if (hdCamera.IsPathTracingEnabled())
            {
                // For path tracing we use the max extent of the extended culling frustum as the light cluster size
                Vector3 extendedFrustumExtent = (hdCamera.camera.transform.up + hdCamera.camera.transform.right + hdCamera.camera.transform.forward) * hdCamera.camera.farClipPlane;
                range = Mathf.Max(Mathf.Max(Mathf.Abs(extendedFrustumExtent.x), Mathf.Abs(extendedFrustumExtent.y)), Mathf.Abs(extendedFrustumExtent.z));
            }

            return new Bounds(camPosWS, 2.0f * new Vector3(range, range, range));
        }

        void EvaluateClusterVolume(HDCamera hdCamera, in WorldLightSubSet subset)
        {
            var cluster = GetLightClusterBounds(hdCamera);
            
            minClusterPos = Vector3.Max(subset.bounds.min, cluster.min);
            maxClusterPos = Vector3.Min(subset.bounds.max, cluster.max);

            // Compute the cell size per dimension
            clusterCellSize = (maxClusterPos - minClusterPos);
            clusterCellSize.x /= HDLightClusterDefinitions.s_ClusterSize.x;
            clusterCellSize.y /= HDLightClusterDefinitions.s_ClusterSize.y;
            clusterCellSize.z /= HDLightClusterDefinitions.s_ClusterSize.z;

            // Compute the bounds of the cluster volume3
            clusterCenter = (maxClusterPos + minClusterPos) / 2.0f;
            clusterDimension = (maxClusterPos - minClusterPos);
        }

        void CullLights(CommandBuffer cmd, WorldLightsVolumes worldLightsVolumes)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingCullLights)))
            {
                int totalLightCount = worldLightsVolumes.GetCount();

                // Make sure the culling buffer has the right size
                if (m_LightCullResult == null || m_LightCullResult.count != totalLightCount)
                {
                    ResizeCullResultBuffer(totalLightCount);
                }

                ComputeShader lightClusterCS = m_RenderPipeline.rayTracingResources.lightClusterBuildCS;

                // Grab the kernel
                int lightClusterCullKernel = lightClusterCS.FindKernel(m_LightCullKernelName);

                // Inject all the parameters
                cmd.SetComputeVectorParam(lightClusterCS, _ClusterCenterPosition, clusterCenter);
                cmd.SetComputeVectorParam(lightClusterCS, _ClusterDimension, clusterDimension);
                cmd.SetComputeIntParam(lightClusterCS, _LightVolumeCount, totalLightCount);

                cmd.SetComputeBufferParam(lightClusterCS, lightClusterCullKernel, _LightVolumes, worldLightsVolumes.GetBuffer());
                cmd.SetComputeBufferParam(lightClusterCS, lightClusterCullKernel, _RaytracingLightCullResult, m_LightCullResult);

                // Dispatch a compute
                int numLightGroups = (totalLightCount / 16 + 1);
                cmd.DispatchCompute(lightClusterCS, lightClusterCullKernel, numLightGroups, 1, 1);
            }
        }

        void BuildLightCluster(HDCamera hdCamera, CommandBuffer cmd, WorldLightsVolumes worldLightsVolumes, WorldLightSubSet worldLightSubSet)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingBuildCluster)))
            {
                // Grab the kernel
                ComputeShader lightClusterCS = m_RenderPipeline.rayTracingResources.lightClusterBuildCS;
                int lightClusterKernel = lightClusterCS.FindKernel(m_LightClusterKernelName);

                // Inject all the parameters
                cmd.SetComputeBufferParam(lightClusterCS, lightClusterKernel, HDShaderIDs._RaytracingLightClusterRW, m_LightCluster);
                cmd.SetComputeVectorParam(lightClusterCS, _ClusterCellSize, clusterCellSize);

                cmd.SetComputeBufferParam(lightClusterCS, lightClusterKernel, _LightVolumes, worldLightsVolumes.GetBuffer());
                cmd.SetComputeBufferParam(lightClusterCS, lightClusterKernel, _LightSubSet, worldLightSubSet.GetBuffer());
                cmd.SetComputeIntParam(lightClusterCS, _LightVolumeCount, worldLightsVolumes.GetCount());
                cmd.SetComputeIntParam(lightClusterCS, _LightSubSetCount, worldLightSubSet.GetCount());
                cmd.SetComputeBufferParam(lightClusterCS, lightClusterKernel, _RaytracingLightCullResult, m_LightCullResult);

                // Dispatch a compute
                int numGroupsX = CoreUtils.DivRoundUp(HDLightClusterDefinitions.s_ClusterSize.x, 8);
                int numGroupsY = CoreUtils.DivRoundUp(HDLightClusterDefinitions.s_ClusterSize.y, 8);
                int numGroupsZ = CoreUtils.DivRoundUp(HDLightClusterDefinitions.s_ClusterSize.z, 8);
                cmd.DispatchCompute(lightClusterCS, lightClusterKernel, numGroupsX, numGroupsY, numGroupsZ);
            }
        }

        class LightClusterDebugPassData
        {
            public int texWidth;
            public int texHeight;
            public int lightClusterDebugKernel;
            public int clusterLightCategory;
            public Vector3 clusterCellSize;
            public Material debugMaterial;
            public BufferHandle lightCluster;
            public ComputeShader lightClusterDebugCS;

            public TextureHandle depthStencilBuffer;
            public TextureHandle depthPyramid;
            public TextureHandle outputBuffer;
        }

        public void EvaluateClusterDebugView(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthStencilBuffer, TextureHandle depthPyramid)
        {
            // TODO: Investigate why this behavior causes a leak in player mode only.
            if (FullScreenDebugMode.LightCluster != m_RenderPipeline.m_CurrentDebugDisplaySettings.data.fullScreenDebugMode)
                return;

            TextureHandle debugTexture;
            using (var builder = renderGraph.AddRenderPass<LightClusterDebugPassData>("Debug Texture for the Light Cluster", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingDebugCluster)))
            {
                builder.EnableAsyncCompute(false);

                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.clusterCellSize = clusterCellSize;
                passData.lightCluster = builder.ReadBuffer(renderGraph.ImportBuffer(m_LightCluster));
                passData.lightClusterDebugCS = m_RenderPipeline.rayTracingResources.lightClusterDebugCS;
                passData.lightClusterDebugKernel = passData.lightClusterDebugCS.FindKernel("DebugLightCluster");
                passData.debugMaterial = m_DebugMaterial;
                passData.depthStencilBuffer = builder.UseDepthBuffer(depthStencilBuffer, DepthAccess.Read);
                passData.depthPyramid = builder.ReadTexture(depthStencilBuffer);
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Light Cluster Debug Texture" }));

                passData.clusterLightCategory = m_RenderPipeline.m_CurrentDebugDisplaySettings.data.lightClusterCategoryDebug;

                builder.SetRenderFunc(
                    (LightClusterDebugPassData data, RenderGraphContext ctx) =>
                    {
                        var debugMaterialProperties = ctx.renderGraphPool.GetTempMaterialPropertyBlock();

                        // Bind the output texture
                        CoreUtils.SetRenderTarget(ctx.cmd, data.outputBuffer, data.depthStencilBuffer, clearFlag: ClearFlag.Color, clearColor: Color.black);

                        // Inject all the parameters to the debug compute
                        ctx.cmd.SetComputeBufferParam(data.lightClusterDebugCS, data.lightClusterDebugKernel, HDShaderIDs._RaytracingLightCluster, data.lightCluster);
                        ctx.cmd.SetComputeVectorParam(data.lightClusterDebugCS, _ClusterCellSize, data.clusterCellSize);
                        ctx.cmd.SetComputeIntParam(data.lightClusterDebugCS, _ClusterLightCategoryDebug, data.clusterLightCategory);
                        ctx.cmd.SetComputeTextureParam(data.lightClusterDebugCS, data.lightClusterDebugKernel, HDShaderIDs._CameraDepthTexture, data.depthStencilBuffer);

                        // Target output texture
                        ctx.cmd.SetComputeTextureParam(data.lightClusterDebugCS, data.lightClusterDebugKernel, _DebutLightClusterTexture, data.outputBuffer);

                        // Dispatch the compute
                        int lightVolumesTileSize = 8;
                        int numTilesX = (data.texWidth + (lightVolumesTileSize - 1)) / lightVolumesTileSize;
                        int numTilesY = (data.texHeight + (lightVolumesTileSize - 1)) / lightVolumesTileSize;

                        ctx.cmd.DispatchCompute(data.lightClusterDebugCS, data.lightClusterDebugKernel, numTilesX, numTilesY, 1);

                        // Bind the parameters
                        debugMaterialProperties.SetBuffer(HDShaderIDs._RaytracingLightCluster, data.lightCluster);
                        debugMaterialProperties.SetVector(_ClusterCellSize, data.clusterCellSize);
                        debugMaterialProperties.SetTexture(HDShaderIDs._CameraDepthTexture, data.depthPyramid);

                        // Draw the faces
                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.debugMaterial, 1, MeshTopology.Lines, 48, HDLightClusterDefinitions.s_ClusterCellCount, debugMaterialProperties);
                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.debugMaterial, 0, MeshTopology.Triangles, 36, HDLightClusterDefinitions.s_ClusterCellCount, debugMaterialProperties);
                    });

                debugTexture = passData.outputBuffer;
            }

            m_RenderPipeline.PushFullScreenDebugTexture(renderGraph, debugTexture, FullScreenDebugMode.LightCluster);
        }

        public GraphicsBuffer GetCluster()
        {
            return m_LightCluster;
        }

        public Vector3 GetMinClusterPos()
        {
            return minClusterPos;
        }

        public Vector3 GetMaxClusterPos()
        {
            return maxClusterPos;
        }

        public Vector3 GetClusterCellSize()
        {
            return clusterCellSize;
        }

        public int GetLightPerCellCount()
        {
            return m_NumLightsPerCell;
        }

        void InvalidateCluster()
        {
            // Invalidate the cluster's bounds so that we never access the buffer (the buffer's access in hlsl is surrounded by position testing)
            minClusterPos.Set(float.MaxValue, float.MaxValue, float.MaxValue);
            maxClusterPos.Set(-float.MaxValue, -float.MaxValue, -float.MaxValue);
        }

        public void CullForRayTracing(HDCamera hdCamera, WorldLights worldLights, WorldLightsVolumes worldLightsVolumes)
        {
            uint filter = hdCamera.IsPathTracingEnabled() ? (uint)WorldLightFlags.ActivePathtracing : (uint)WorldLightFlags.ActiveRaytracing;

            // Filter the world lights
            WorldLightCulling.GetLightSubSetUsingFlags(worldLightsVolumes, filter, m_WorldLightSubSet);

            // If there is no lights to process or no environment not the shader is missing
            if (worldLights.totalLightCount == 0 || !m_RenderPipeline.GetRayTracingState())
            {
                InvalidateCluster();
                return;
            }

            // If no valid light were found, invalidate the cluster and leave
            if (m_WorldLightSubSet.GetCount() == 0)
            {
                InvalidateCluster();
                return;
            }

            // Evaluate the volume of the cluster
            EvaluateClusterVolume(hdCamera, m_WorldLightSubSet);
        }

        public void BuildLightClusterBuffer(CommandBuffer cmd, HDCamera hdCamera, WorldLightsVolumes worldLightsVolumes)
        {
            // If there is no lights to process or no environment not the shader is missing
            if (m_WorldLightSubSet.GetCount() == 0 || !m_RenderPipeline.GetRayTracingState())
                return;

            // Cull the lights within the evaluated cluster range
            CullLights(cmd, worldLightsVolumes);

            // Build the light Cluster
            BuildLightCluster(hdCamera, cmd, worldLightsVolumes, m_WorldLightSubSet);
        }

        public void ReserveCookieAtlasSlots(WorldLights rayTracingLights)
        {
            HDLightRenderDatabase lightEntities = HDLightRenderDatabase.instance;
            for (int lightIdx = 0; lightIdx < rayTracingLights.hdLightEntityArray.Length; ++lightIdx)
            {
                int dataIndex = lightEntities.GetEntityDataIndex(rayTracingLights.hdLightEntityArray[lightIdx].light);
                HDAdditionalLightData additionalLightData = lightEntities.hdAdditionalLightData[dataIndex];
                // Grab the additional light data to process
                // Fetch the light component for this light
                additionalLightData.gameObject.TryGetComponent(out Light lightComponent);

                // Reserve the cookie resolution in the 2D atlas
                m_RenderPipeline.ReserveCookieAtlasTexture(additionalLightData, lightComponent, additionalLightData.legacyLight.type);
            }
        }

        public void BindLightClusterData(CommandBuffer cmd)
        {
            cmd.SetGlobalBuffer(HDShaderIDs._RaytracingLightCluster, GetCluster());
        }
    }
}
