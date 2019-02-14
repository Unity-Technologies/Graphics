using System.Collections.Generic;

namespace UnityEngine.Rendering.LWRP
{
    public enum RenderQueueType
    {
        Opaque,
        Transparent,
    }

    [CreateAssetMenu]
    public class RenderObjectsPassFeature : RenderPassFeature
    {
        [System.Serializable]
        public class RenderObjectsSettings
        {
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

            public FilterSettings filterSettings = new FilterSettings();
            
            public Material overrideMaterial = null;
            public int overrideMaterialPassIndex = 0;

            public bool overrideDepthState = false;
            public CompareFunction depthCompareFunction = CompareFunction.Less;
            public bool enableWrite = true;

            public bool overrideStencilState = false;
            public int stencilReference = 1;
            public CompareFunction stencilCompareFunction = CompareFunction.Always;
            public StencilOp passOperation = StencilOp.Keep;
            public StencilOp failOperation = StencilOp.Keep;
            public StencilOp zFailOperation = StencilOp.Keep;
        }
        
        [System.Serializable]
        public class FilterSettings
        {
            // TODO: expose opaque, transparent, all ranges as drop down
            public RenderQueueType RenderQueueType;
            public LayerMask LayerMask;
            public string[] PassNames;

            public FilterSettings()
            {
                RenderQueueType = RenderQueueType.Opaque;
                LayerMask = -1;
                PassNames = new []{"LightweightForward"};
            }
        }

        public RenderObjectsSettings settings = new RenderObjectsSettings();

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
            FilterSettings filter = settings.filterSettings;
            renderObjectsPass = new RenderObjectsPass(settings.Event, filter.PassNames, filter.RenderQueueType, filter.LayerMask);
            renderObjectsPass.overrideMaterial = settings.overrideMaterial;
            renderObjectsPass.overrideMaterialPassIndex = settings.overrideMaterialPassIndex;

            if (settings.overrideDepthState)
                renderObjectsPass.SetDetphState(settings.enableWrite, settings.depthCompareFunction);

            if (settings.overrideStencilState)
                renderObjectsPass.SetStencilState(settings.stencilReference, settings.stencilCompareFunction, settings.passOperation, settings.failOperation, settings.zFailOperation);
        }

        public override void AddRenderPasses(List<ScriptableRenderPass> renderPasses,
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorAttachmentHandle,
            RenderTargetHandle depthAttachmentHandle)
        {
            renderPasses.Add(renderObjectsPass);
        }
    }
}

