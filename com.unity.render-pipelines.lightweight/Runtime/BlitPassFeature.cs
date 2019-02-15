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
            public Target source = Target.Color;
            public Target dest = Target.Color;
            public RenderTexture texture;
        }
        
        public enum Target
        {
            Color,
            Depth,
            Texture
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
            var src = colorAttachmentHandle;
            var dest = colorAttachmentHandle;

            switch (settings.source)
            {
                case Target.Color:
                    break;
                case Target.Depth:
                    src = depthAttachmentHandle;
                    break;
            }
            
            switch (settings.dest)
            {
                case Target.Color:
                    break;
                case Target.Depth:
                    dest = depthAttachmentHandle;
                    break;
            }
            
            blitPass.Setup(src, dest);
            renderPasses.Add(blitPass);
        }
    }
}

