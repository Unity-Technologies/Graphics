using System.Collections.Generic;

namespace UnityEngine.Rendering.LWRP
{
    [CreateAssetMenu]
    public class BlitPassFeature : RenderPassFeature
    {
        [System.Serializable]
        public class BlitSettings
        {
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;
            
            public Material blitMaterial = null;
            public int blitMaterialPassIndex = -1;
        }

        public BlitSettings settings = new BlitSettings();

        BlitPass blitPass;

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
            settings.blitMaterialPassIndex = Mathf.Clamp(settings.blitMaterialPassIndex, -1, settings.blitMaterial.passCount - 1);
            blitPass = new BlitPass(settings.Event, settings.blitMaterial, settings.blitMaterialPassIndex, name);
        }

        public override void AddRenderPasses(List<ScriptableRenderPass> renderPasses,
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorAttachmentHandle,
            RenderTargetHandle depthAttachmentHandle)
        {
            blitPass.Setup(colorAttachmentHandle, colorAttachmentHandle);
            renderPasses.Add(blitPass);
        }
    }
}

