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

        // TODO: expose opaque, transparent, all ranges as drop down
        public InjectionCallback callback;
        public RenderQueueType renderQueueType;
        public LayerMask layerMask = -1;
        public string[] passNames = {"LightweightForward"};
        public Material overrideMaterial;
        public int overrideMaterialPassIndex;

        public bool overrideDepthState = false;
        public CompareFunction depthCompareFunction = CompareFunction.Less;
        public bool enableWrite = true;

        public bool overrideStencilState = false;
        public int stencilReference = 1;
        public CompareFunction stencilCompareFunction = CompareFunction.Always;
        public StencilOp passOperation = StencilOp.Keep;
        public StencilOp failOperation = StencilOp.Keep;
        public StencilOp zFailOperation = StencilOp.Keep;

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
            renderObjectsPass = new RenderObjectsPass(passNames, renderQueueType, layerMask);
            renderObjectsPass.overrideMaterial = overrideMaterial;
            renderObjectsPass.overrideMaterialPassIndex = overrideMaterialPassIndex;

            if (overrideDepthState)
                renderObjectsPass.SetDetphState(enableWrite, depthCompareFunction);

            if (overrideStencilState)
                renderObjectsPass.SetStencilState(stencilReference, stencilCompareFunction, passOperation, failOperation, zFailOperation);
        }

        public override InjectionPoint injectionPoints => (InjectionPoint)(1 << (int)callback);

        public override ScriptableRenderPass GetPassToEnqueue(InjectionPoint injection, RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorAttachmentHandle, RenderTargetHandle depthAttachmentHandle)
        {
            if (injection == (InjectionPoint)(1 << (int)callback))
                return renderObjectsPass;

            return null;
        }
    }
}

