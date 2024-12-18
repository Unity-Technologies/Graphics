using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class VRS_Draw_RendererFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent;
    public int renderPassEventOffset = 1;

    class VRS_Draw_CustomRenderPass : ScriptableRenderPass
    {

        public VRS_Draw_CustomRenderPass()
        {
            profilingSampler = new ProfilingSampler("VRS_Draw_CustomRenderPass");
        }

        private class PassData
        {
            public RendererList rendererList;
            public RendererListHandle rendererListHandle;
        }

        static List<ShaderTagId> s_ShaderTagIdList = new List<ShaderTagId>()
        {
            new ShaderTagId("VRS")
        };
        private void InitRendererLists(UniversalCameraData cameraData, UniversalRenderingData renderingData, UniversalLightData lightData,
           RenderGraph renderGraph, ref PassData passData)
        {
            SortingCriteria sortingCriteria = cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(s_ShaderTagIdList, renderingData,
                cameraData, lightData, sortingCriteria);

            //int layerMask = 1 << 1;
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque,  ~0, uint.MaxValue, 0);
            RenderStateBlock renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            NativeArray<ShaderTagId> tagValues = new NativeArray<ShaderTagId>(1, Allocator.Temp);
            NativeArray<RenderStateBlock> stateBlocks = new NativeArray<RenderStateBlock>(1, Allocator.Temp);
            tagValues[0] = ShaderTagId.none;
            stateBlocks[0] = renderStateBlock;
            var param = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings)
            {
                tagValues = tagValues,
                stateBlocks = stateBlocks,
                isPassTagName = false
            };
            passData.rendererListHandle = renderGraph.CreateRendererList(param);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passName = "VRS Render Custom Pass";

            if (!Vrs.IsColorMaskTextureConversionSupported()) return;

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.historyManager == null)
                return;

            VRSHistory vrsHistory = cameraData.historyManager.GetHistoryForRead<VRSHistory>();
            if (vrsHistory == null)
                return;

            var sriTextureRTHandle = vrsHistory.GetSRITexture();
            if (sriTextureRTHandle.rt == null)
                return;

            // This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);

                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();

                InitRendererLists(cameraData, renderingData, lightData, renderGraph, ref passData);
                builder.UseRendererList(passData.rendererListHandle);

                builder.AllowGlobalStateModification(true);

                var sriTextureHandle = vrsHistory.importedSRITextureHandle;
                Debug.Assert(sriTextureHandle.IsValid(), "Draws imported sri is not valid.");
                //var sriTextureHandle = renderGraph.ImportTexture(sriTextureRTHandle);

                if (sriTextureHandle.IsValid())
                {
                    builder.SetShadingRateImageAttachment(sriTextureHandle);
                    builder.SetShadingRateCombiner(ShadingRateCombinerStage.Fragment, ShadingRateCombiner.Override);
                }

                // Assigns the ExecutePass function to the render pass delegate. This will be called by the render graph when executing the pass.
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }

        // This static method is passed as the RenderFunc delegate to the RenderGraph render pass.
        // It is used to execute draw commands.
        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            context.cmd.DrawRendererList(data.rendererListHandle);
        }
    }

    VRS_Draw_CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new VRS_Draw_CustomRenderPass();
        m_ScriptablePass.renderPassEvent = renderPassEvent + renderPassEventOffset;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        /*if (!GetMaterials())
        {
            Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added.", GetType().Name, name);
            return;
        }

        m_ScriptablePass.m_Material = m_Material;*/
        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        //CoreUtils.Destroy(m_Material);
    }

    /*private bool GetMaterials()
    {
        if (m_Material == null && vrsShader != null)
            m_Material = CoreUtils.CreateEngineMaterial(vrsShader);
        return m_Material != null;
    }*/
}
