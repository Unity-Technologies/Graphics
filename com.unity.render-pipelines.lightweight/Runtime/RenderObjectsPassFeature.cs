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
        public enum InjectionCallback
        {
            BeforeRenderPasses,
            AfterOpaqueRenderPasses,
            AfterOpaquePostProcessPasses,
            AfterSkyboxPasses,
            AfterTransparentPasses,
            AfterRenderPasses,
        };

        [System.Serializable]
        public class RenderObjectsSettings
        {
            // TODO: expose opaque, transparent, all ranges as drop down
            public InjectionCallback callback = InjectionCallback.AfterOpaqueRenderPasses;
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
            renderObjectsPass = new RenderObjectsPass(settings.passNames, settings.renderQueueType, settings.layerMask);
            renderObjectsPass.overrideMaterial = settings.overrideMaterial;
            renderObjectsPass.overrideMaterialPassIndex = settings.overrideMaterialPassIndex;

            if (settings.overrideDepthState)
                renderObjectsPass.SetDetphState(settings.enableWrite, settings.depthCompareFunction);

            if (settings.overrideStencilState)
                renderObjectsPass.SetStencilState(settings.stencilReference, settings.stencilCompareFunction, settings.passOperation, settings.failOperation, settings.zFailOperation);
        }

        public override InjectionPoint injectionPoints => (InjectionPoint)(1 << (int)settings.callback);

        public override ScriptableRenderPass GetPassToEnqueue(InjectionPoint injection, RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorAttachmentHandle, RenderTargetHandle depthAttachmentHandle)
        {
            if (injection == (InjectionPoint)(1 << (int)settings.callback))
                return renderObjectsPass;

            return null;
        }
    }
}

