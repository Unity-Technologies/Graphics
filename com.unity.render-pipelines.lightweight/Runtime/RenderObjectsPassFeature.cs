using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;
using System.Linq;

namespace UnityEngine.Experimental.Rendering.LWRP 
{
    public enum RenderQueueType
    {
        Opaque,
        Transparent,
    }

    [CreateAssetMenu]
    public class RenderObjectsPassFeature : RenderPassFeature
    {
        public enum InjectionCallback
        {
            BeforeRenderPasses,
            AfterOpaqueRenderPasses,
            AfterOpaquePostProcessPasses,
            AfterSkyboxPasses,
            AfterTransparentPasses,
            AfterRenderPasses,
        };

        // TODO: expose opaque, transparent, all ranges as drop down
        public InjectionCallback callback;
        public RenderQueueType renderQueueType;
        public string layerName = "Default";
        public string[] passNames = {"LightweightForward"};

        RenderObjectsPass renderObjectsPass;

        void OnEnable()
        {
            Initialize();
        }

        void OnValidate()
        {
            Initialize();
        }

        void Initialize()
        {
            int layerMask = (layerName.Equals("Default")) ? -1 : LayerMask.GetMask(layerName);
            renderObjectsPass = new RenderObjectsPass(passNames, renderQueueType, layerMask);
        }

        public override InjectionPoint injectionPoints => (InjectionPoint)(1 << (int)callback);

        public override ScriptableRenderPass GetPassToEnqueue(InjectionPoint injection, RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorAttachmentHandle, RenderTargetHandle depthAttachmentHandle)
        {
            if (injection == (InjectionPoint)(1 << (int)callback))
                return renderObjectsPass;

            return null;
        }
    }

    public class RenderObjectsPass : ScriptableRenderPass
    {
        FilteringSettings filteringSettings;
        RenderQueueType renderQueueType;

        public RenderObjectsPass(string[] shaderTags, RenderQueueType renderQueueType, int layerMask)
        {
            this.renderQueueType = renderQueueType;
            RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
                ? RenderQueueRange.transparent
                : RenderQueueRange.opaque;
            filteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            foreach (var passName in shaderTags)
                RegisterShaderPassName(passName);
        }

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = (renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : renderingData.cameraData.defaultOpaqueSortFlags;

            Camera camera = renderingData.cameraData.camera;
            DrawingSettings drawingSettings = CreateDrawingSettings(camera, sortingCriteria, renderingData.perObjectData, renderingData.supportsDynamicBatching);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        }
    }
}

