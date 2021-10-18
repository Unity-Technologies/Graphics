using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using Unity.Collections;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        HDBRGCallbacks m_HDBRGCallbacks;
        internal Material m_VisibilityMaterial;
        internal GeometryPool m_GlobalGeoPool;

        struct VBufferOutput
        {
            public TextureHandle vbuffer;
        }

        internal class HDBRGCallbacks : IBRGCallbacks
        {
            private HDRenderPipeline m_HDRenderPipeline;

            public HDBRGCallbacks(HDRenderPipeline pipeline)
            {
                m_HDRenderPipeline = pipeline;
                m_HDRenderPipeline.InitializeVisibilityPass();
            }

            public BRGInternalSRPConfig GetSRPConfig() => m_HDRenderPipeline.GetBRGSRPConfig();

            public void OnAddRenderers(AddRendererParameters parameters) => m_HDRenderPipeline.OnAddRenderersForVBuffer(parameters);
            public void OnRemoveRenderers(List<MeshRenderer> renderers) => m_HDRenderPipeline.OnRemoveRenderersForVBuffer(renderers);
            public int OnSubmeshIndexForOverrides(SubmeshIndexForOverridesParams parameters) => m_HDRenderPipeline.OnSubmeshIndexForOverridesVBuffer(parameters);
            public void Dispose() => m_HDRenderPipeline.ShutdownVisibilityPass();
        }

        internal void InitializeVisibilityPass()
        {
            m_VisibilityMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.visibilityPS);
            m_GlobalGeoPool = new GeometryPool(GeometryPoolDesc.NewDefault());
        }

        internal void ShutdownVisibilityPass()
        {
            CoreUtils.Destroy(m_VisibilityMaterial);
            m_GlobalGeoPool.Dispose();
            m_GlobalGeoPool = null;
            m_VisibilityMaterial = null;
        }

        internal bool IsVisibilityPassEnabled()
        {
            return m_VisibilityMaterial != null;
        }

        internal BRGInternalSRPConfig GetBRGSRPConfig()
        {
            var metadata = new NativeArray<AddedMetadataDesc>(1, Allocator.Temp);
            metadata[0] = new AddedMetadataDesc()
            {
                name = HDShaderIDs._VisBufferInstanceData,
                sizeInVec4s = 1
            };

            return new BRGInternalSRPConfig()
            {
                metadatas = metadata,
                overrideMesh = m_GlobalGeoPool.globalMesh,
                overrideMaterial = m_VisibilityMaterial
            };
        }

        class VBufferPassData
        {
            public FrameSettings frameSettings;
            public RendererListHandle rendererList;
        }

        internal void OnAddRenderersForVBuffer(AddRendererParameters parameters)
        {
            for (int i = 0; i < parameters.addedRenderers.Count; ++i)
            {
                AddedRendererInformation rendererInfo = parameters.addedRenderersInfo[i];
                var meshFilter = rendererInfo.meshFilter;

                //TODO handle out of memory case later.
                m_GlobalGeoPool.Register(meshFilter.sharedMesh, out GeometryPoolHandle geoHandle);

                if (!geoHandle.valid)
                {
                    Debug.LogError("Could not register mesh to geometry pool" + meshFilter.sharedMesh.name);
                    continue;
                }

                parameters.instanceBuffer[parameters.instanceBufferOffset + rendererInfo.instanceIndex] =
                    new Vector4(geoHandle.index, 0, 0, 0);
            }

            //Send to gpu immediately. Its possible we could defer these copy commands
            //but for now we do it immediately to avoid accessing possible data erased
            m_GlobalGeoPool.SendGpuCommands();
        }

        internal void OnRemoveRenderersForVBuffer(List<MeshRenderer> renderers)
        {
            if (m_GlobalGeoPool == null)
                return;

            foreach (var renderer in renderers)
            {
                var meshFilter = renderer.gameObject.GetComponent<MeshFilter>();
                if (meshFilter == null)
                    continue;

                //TODO handle out of memory case later.
                m_GlobalGeoPool.Unregister(meshFilter.sharedMesh);
            }

            //Send to gpu immediately. Its possible we could defer these copy commands
            //but for now we do it immediately to avoid accessing possible data erased
            m_GlobalGeoPool.SendGpuCommands();
        }

        internal int OnSubmeshIndexForOverridesVBuffer(SubmeshIndexForOverridesParams parameters)
        {
            return (int)parameters.instanceBuffer[parameters.instanceBufferOffset + parameters.rendererInfo.instanceIndex].x;
        }

        void RenderVBuffer(RenderGraph renderGraph, HDCamera hdCamera, CullingResults cull, ref PrepassOutput output)
        {
            output.vbuffer = new VBufferOutput();

            if (!IsVisibilityPassEnabled())
            {
                output.vbuffer.vbuffer = renderGraph.defaultResources.blackUIntTextureXR;
                return;
            }

            var visFormat = GraphicsFormat.R32_UInt;
            using (var builder = renderGraph.AddRenderPass<VBufferPassData>("VBuffer", out var passData, ProfilingSampler.Get(HDProfileId.VBuffer)))
            {
                builder.AllowRendererListCulling(false);

                FrameSettings frameSettings = hdCamera.frameSettings;

                passData.frameSettings = frameSettings;

                output.depthBuffer = builder.UseDepthBuffer(output.depthBuffer, DepthAccess.ReadWrite);
                output.vbuffer.vbuffer = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true)
                    {
                        colorFormat = visFormat,
                        clearBuffer = true,//TODO: for now clear
                        clearColor = Color.clear,
                        name = "VisibilityBuffer"
                    }), 0);

                passData.rendererList = builder.UseRendererList(
                   renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(
                        cull, hdCamera.camera,
                        HDShaderPassNames.s_VBufferName, m_CurrentRendererConfigurationBakedLighting, null, null, m_VisibilityMaterial, excludeObjectMotionVectors: false)));

                m_GlobalGeoPool.BindResources(m_VisibilityMaterial);

                builder.SetRenderFunc(
                    (VBufferPassData data, RenderGraphContext context) =>
                    {
                        DrawOpaqueRendererList(context, data.frameSettings, data.rendererList);
                    });
            }
        }
    }
}
