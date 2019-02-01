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
        public int layerMask;
        public string[] passNames = {"LightweightForward", "SRPDefaultUnlit"};

        RenderObjectsPass renderObjectsPass;

        void OnEnable()
        {
            ShaderTagId[] shaderTags = passNames.Select(x => new ShaderTagId(x)).ToArray();
            renderObjectsPass = new RenderObjectsPass(shaderTags, renderQueueType, layerMask);
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
        ShaderTagId[] shaderTags;
        FilteringSettings filteringSettings;
        RenderQueueType renderQueueType;

        public RenderObjectsPass(ShaderTagId[] shaderTags, RenderQueueType renderQueueType, int layerMask)
        {
            this.shaderTags = shaderTags;
            this.renderQueueType = renderQueueType;
            RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
                ? RenderQueueRange.transparent
                : RenderQueueRange.opaque;
            filteringSettings = new FilteringSettings(renderQueueRange, layerMask);
        }

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = (renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : renderingData.cameraData.defaultOpaqueSortFlags;

            RenderObjects(context, ref renderingData, shaderTags, ref filteringSettings, sortingCriteria);
        }
    }
}

