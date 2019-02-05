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
        public LayerMask layerMask = -1;
        public string[] passNames = {"LightweightForward"};
        public Material overrideMaterial;
        public int overrideMaterialPassIndex;

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
            renderObjectsPass = new RenderObjectsPass(passNames, renderQueueType, overrideMaterial, overrideMaterialPassIndex, layerMask);
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
        RenderQueueType renderQueueType;
        Material overrideMaterial;
        int overrideMaterialPassIndex;

        public RenderObjectsPass(string[] shaderTags, RenderQueueType renderQueueType, Material overrideMaterial, int overrideMaterialPassIndex, int layerMask)
        {
            this.renderQueueType = renderQueueType;
            this.overrideMaterial = overrideMaterial;
            this.overrideMaterialPassIndex = overrideMaterialPassIndex;
            RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
                ? RenderQueueRange.transparent
                : RenderQueueRange.opaque;
            filteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            foreach (var passName in shaderTags)
                RegisterShaderPassName(passName);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = (renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : renderingData.cameraData.defaultOpaqueSortFlags;

            Camera camera = renderingData.cameraData.camera;
            DrawingSettings drawingSettings = CreateDrawingSettings(camera, sortingCriteria, renderingData.perObjectData, renderingData.supportsDynamicBatching);
            drawingSettings.overrideMaterial = overrideMaterial;
            drawingSettings.overrideMaterialPassIndex = overrideMaterialPassIndex; 
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        }
    }
}

