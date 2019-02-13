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
            // TODO: expose opaque, transparent, all ranges as drop down
            public RenderPassEvent callback = RenderPassEvent.AfterRenderingOpaques;
            public RenderQueueType renderQueueType = RenderQueueType.Opaque;
            public LayerMask layerMask = -1;
            public string[] passNames = {"LightweightForward"};
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
            renderObjectsPass = new RenderObjectsPass(settings.callback, settings.passNames, settings.renderQueueType, settings.layerMask);
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

