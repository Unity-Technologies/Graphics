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
                if (owner.isBlendModeOpaque)
                    return true;

                return transparentRenderQueue != TransparentRenderQueue.LowResolution
                    && transparentRenderQueue != TransparentRenderQueue.AfterPostProcessing;
            }
        }
        public override bool supportsExcludeFromTAA { get { return !owner.isBlendModeOpaque; } }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (!supportsQueueSelection)
                {
                    yield return "transparentRenderQueue";
                    yield return "opaqueRenderQueue";
                }
                else if (owner.isBlendModeOpaque)
                    yield return "transparentRenderQueue";
                else
                    yield return "opaqueRenderQueue";
            }
        }

        public override void OnSettingModified(VFXSetting setting)
        {
            base.OnSettingModified(setting);
            // Reset to default if render queue is invalid
            if (setting.name == "transparentRenderQueue")
            {               
                if (!supportsQueueSelection || (isLit && transparentRenderQueue == TransparentRenderQueue.AfterPostProcessing))
                    transparentRenderQueue = TransparentRenderQueue.Default;
            }
            else if (setting.name == "opaqueRenderQueue")
            {
                if (!supportsQueueSelection || (isLit && opaqueRenderQueue == OpaqueRenderQueue.AfterPostProcessing))
                    opaqueRenderQueue = OpaqueRenderQueue.Default;
            }
        }

        protected bool isLit => owner is VFXAbstractParticleHDRPLitOutput;
        protected bool supportsQueueSelection => !(owner is VFXAbstractDistortionOutput); // TODO Should be made in a more abstract way

        public override IEnumerable<int> GetFilteredOutEnumerators(string name)
        {
            if (isLit)
            {
                switch (name)
                {
                    case "opaqueRenderQueue":
                        yield return (int)OpaqueRenderQueue.AfterPostProcessing;
                        break;
                    case "transparentRenderQueue":
                        yield return (int)TransparentRenderQueue.AfterPostProcessing;
                        break;
                }
            }
        }
        
        public override string GetBlendModeStr()
        {
            bool isOffscreen = transparentRenderQueue == TransparentRenderQueue.LowResolution || transparentRenderQueue == TransparentRenderQueue.AfterPostProcessing;
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

        private void GetStencilStateCommon(out int stencilWriteMask, out int stencilRef)
        {
            stencilWriteMask = 0;
            stencilRef = 0;
        }

        private void GetStencilStateMotionVector(out int stencilWriteMask, out int stencilRef, bool receiveSSR, bool useObjectVelocity)
        {
            GetStencilStateCommon(out stencilWriteMask, out stencilRef);

            stencilWriteMask |= (int)StencilUsage.TraceReflectionRay;
            stencilRef |= receiveSSR ? (int)StencilUsage.TraceReflectionRay : 0;

            stencilWriteMask |= useObjectVelocity ? (int)StencilUsage.ObjectMotionVector : 0;
            stencilRef |= useObjectVelocity ? (int)StencilUsage.ObjectMotionVector : 0;
        }

        private void GetStencilStateDistortion(out int stencilWriteMask, out int stencilRef)
        {
            GetStencilStateCommon(out stencilWriteMask, out stencilRef);

            stencilWriteMask |= (int)StencilUsage.DistortionVectors;
            stencilRef |= (int)StencilUsage.DistortionVectors;
        }

        private void GetStencilStateGBuffer(out int stencilWriteMask, out int stencilRef, bool hasSubsurfaceScattering)
        {
            GetStencilStateCommon(out stencilWriteMask, out stencilRef);

            stencilWriteMask |= (int)StencilUsage.RequiresDeferredLighting;
            stencilRef |= (int)StencilUsage.RequiresDeferredLighting;

            stencilWriteMask |= (int)StencilUsage.SubsurfaceScattering;
            stencilRef |= hasSubsurfaceScattering ? (int)StencilUsage.SubsurfaceScattering : 0;
        }

        private void GetStencilStateForward(out int stencilWriteMask, out int stencilRef, bool excludeFromTAA)
        {
            GetStencilStateCommon(out stencilWriteMask, out stencilRef);

            stencilWriteMask |= excludeFromTAA ? (int)StencilUsage.ExcludeFromTAA : 0;
            stencilRef |= excludeFromTAA ? (int)StencilUsage.ExcludeFromTAA : 0;
        }

        public override IEnumerable<KeyValuePair<string, VFXShaderWriter>> GetStencilStateOverridesStr()
        {
            int stencilWriteMaskMV, stencilRefMV;
            GetStencilStateMotionVector(out stencilWriteMaskMV, out stencilRefMV, false, true);
            yield return CreateStencilStateOverrideStr("${VFXStencilMotionVector}", stencilWriteMaskMV, stencilRefMV);

            int stencilWriteMaskDistortion, stencilRefDistortion;
            GetStencilStateDistortion(out stencilWriteMaskDistortion, out stencilRefDistortion);
            yield return CreateStencilStateOverrideStr("${VFXStencilDistortionVectors}", stencilWriteMaskDistortion, stencilRefDistortion);

            int stencilWriteMaskGBuffer, stencilRefGBuffer;
            GetStencilStateGBuffer(out stencilWriteMaskGBuffer, out stencilRefGBuffer, false);
            yield return CreateStencilStateOverrideStr("${VFXStencilGBuffer}", stencilWriteMaskGBuffer, stencilRefGBuffer);

            int stencilWriteMaskForward, stencilRefForward;
            GetStencilStateForward(out stencilWriteMaskForward, out stencilRefForward, owner.hasExcludeFromTAA);
            yield return CreateStencilStateOverrideStr("${VFXStencilForward}", stencilWriteMaskForward, stencilRefForward);
        }

        private KeyValuePair<string, VFXShaderWriter> CreateStencilStateOverrideStr(string variable, int stencilWriteMask, int stencilRef)
        {
            var shaderWriter = new VFXShaderWriter();
            if (stencilWriteMask != 0)
            {
                shaderWriter.WriteFormat("Stencil\n{{\n WriteMask {0}\n Ref {1}\n Comp Always\n Pass Replace\n}}", stencilWriteMask, stencilRef);
            }
            return new KeyValuePair<string, VFXShaderWriter>(variable, shaderWriter);
        }
    }
}
