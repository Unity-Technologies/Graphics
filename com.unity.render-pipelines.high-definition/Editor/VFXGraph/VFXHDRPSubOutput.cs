using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

using static UnityEditor.VFX.VFXAbstractRenderedOutput;
using static UnityEngine.Experimental.Rendering.HDPipeline.HDRenderQueue;

namespace UnityEditor.VFX
{
    class VFXHDRPSubOutput : VFXSRPSubOutput
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Header("HDRP")]
        public OpaqueRenderQueue opaqueRenderQueue = OpaqueRenderQueue.Default;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Header("HDRP")]
        public TransparentRenderQueue transparentRenderQueue = TransparentRenderQueue.Default;

        // Caps
        public override bool supportsExposure { get { return true; } }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (owner.isBlendModeOpaque)
                    yield return "transparentRenderQueue";
                else
                    yield return "opaqueRenderQueue";
            }
        }

        public override string GetBlendModeStr()
        {
            bool isOffscreen = transparentRenderQueue == TransparentRenderQueue.LowResolution || transparentRenderQueue == TransparentRenderQueue.AfterPostProcessing;
            bool isLit = owner is VFXAbstractParticleHDRPLitOutput;
            switch (owner.blendMode)
            {
                case BlendMode.Additive:
                    return string.Format("Blend {0} One {1}", isLit ? "One" : "SrcAlpha", isOffscreen ? ", Zero One" : "");
                case BlendMode.Alpha:
                    return string.Format("Blend {0} OneMinusSrcAlpha {1}", isLit ? "One" : "SrcAlpha", isOffscreen ? ", Zero OneMinusSrcAlpha" : "");
                case BlendMode.AlphaPremultiplied:
                    return string.Format("Blend One OneMinusSrcAlpha {0}", isOffscreen ? ", Zero OneMinusSrcAlpha" : "");
                case BlendMode.Opaque:
                case BlendMode.Masked:
                    return opaqueRenderQueue == OpaqueRenderQueue.AfterPostProcessing ? "Blend One Zero, Zero Zero" : string.Empty; // Blend on for opaque in after post-process for correct compositing TODO Handle that in shader templates directly
                default:
                    return string.Empty;
            }
        }

        public override string GetRenderQueueStr()
        {
            RenderQueueType renderQueueType;
            string prefix = string.Empty;
            if (owner.isBlendModeOpaque)
            {
                prefix = "Geometry";
                renderQueueType = HDRenderQueue.ConvertFromOpaqueRenderQueue(opaqueRenderQueue);
            }
            else
            {
                prefix = "Transparent";
                renderQueueType = HDRenderQueue.ConvertFromTransparentRenderQueue(transparentRenderQueue);
            }

            int renderQueue = HDRenderQueue.ChangeType(renderQueueType, 0, owner.blendMode == BlendMode.Masked) - (int)(owner.isBlendModeOpaque ? Priority.Opaque : Priority.Transparent);
            return prefix + renderQueue.ToString("+#;-#;+0");
        }
    }
}
