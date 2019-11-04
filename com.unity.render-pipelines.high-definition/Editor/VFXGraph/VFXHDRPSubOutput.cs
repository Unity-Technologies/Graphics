using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;
using static UnityEditor.VFX.VFXAbstractRenderedOutput;
using static UnityEngine.Rendering.HighDefinition.HDRenderQueue;

namespace UnityEditor.VFX
{
    class VFXHDRPSubOutput : VFXSRPSubOutput
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Header("HDRP"), Tooltip("Specifies when in the render queue opaque particles are drawn. This is useful for drawing particles after post processing so they are not affected by effects such as Depth of Field.")]
        public OpaqueRenderQueue opaqueRenderQueue = OpaqueRenderQueue.Default;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Header("HDRP"), Tooltip("Specifies when in the render queue particles are drawn. This is useful for drawing particles behind refractive surfaces like frosted glass, for performance gains by rendering them in low resolution, or to draw particles after post processing so they are not affected by effects such as Depth of Field.")]
        public TransparentRenderQueue transparentRenderQueue = TransparentRenderQueue.Default;

        // Caps
        public override bool supportsExposure { get { return true; } } 
        public override bool supportsMotionVector
        {
            get
            {
                return transparentRenderQueue != TransparentRenderQueue.LowResolution
                    && transparentRenderQueue != TransparentRenderQueue.AfterPostProcessing;
            }
        }

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

            int renderQueue = HDRenderQueue.ChangeType(renderQueueType, 0, owner.hasAlphaClipping) - (int)(owner.isBlendModeOpaque ? Priority.Opaque : Priority.Transparent);
            return prefix + renderQueue.ToString("+#;-#;+0");
        }

        //TODO : extend & factorize this method
        public static void GetStencilStateForDepthOrMV(bool receiveDecals, bool receiveSSR, bool useObjectVelocity, out int stencilWriteMask, out int stencilRef)
        {
            stencilWriteMask = (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer;
            stencilRef = receiveDecals ? (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer : 0;

            stencilWriteMask |= (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR;
            stencilRef |= !receiveSSR ? (int)HDRenderPipeline.StencilBitMask.DoesntReceiveSSR : 0;

            stencilWriteMask |= useObjectVelocity ? (int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors : 0;
            stencilRef |= useObjectVelocity ? (int)HDRenderPipeline.StencilBitMask.ObjectMotionVectors : 0;
        }

        public override IEnumerable<KeyValuePair<string, VFXShaderWriter>> GetStencilStateOverridesStr()
        {
            //TODO : Add GBuffer & Distorsion stencil
            int stencilWriteMask, stencilRef;
            GetStencilStateForDepthOrMV(false, false, true, out stencilWriteMask, out stencilRef);
            var stencilForMV = new VFXShaderWriter();
            stencilForMV.WriteFormat("Stencil\n{{\n WriteMask {0}\n Ref {1}\n Comp Always\n Pass Replace\n}}", stencilWriteMask, stencilRef);
            yield return new KeyValuePair<string, VFXShaderWriter>("${VFXStencilMotionVector}", stencilForMV);
        }
    }
}
