using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    abstract class VFXAbstractParticleOutput : VFXContext
    {
        private readonly static bool HDRP = false;

        public enum BlendMode
        {
            Additive,
            Alpha,
            Masked,
            AlphaPremultiplied,
        }

        public enum FlipbookMode
        {
            Off,
            Flipbook,
            FlipbookBlend,
        }

        [VFXSetting, SerializeField]
        protected BlendMode blendMode = BlendMode.Alpha;

        [VFXSetting, SerializeField]
        protected FlipbookMode flipbookMode;

        [VFXSetting, SerializeField]
        protected bool useSoftParticle = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected int sortPriority = 0;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool indirectDraw = false;

        public bool HasIndirectDraw() { return indirectDraw; }

        protected VFXAbstractParticleOutput() : base(VFXContextType.kOutput, VFXDataType.kParticle, VFXDataType.kNone) {}

        public override bool codeGeneratorCompute { get { return false; } }

        public virtual bool supportsFlipbooks { get { return false; } }

        protected virtual IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            if (blendMode == BlendMode.Masked)
                yield return slotExpressions.First(o => o.name == "alphaThreshold");

            if (useSoftParticle)
            {
                var softParticleFade = slotExpressions.First(o => o.name == "softParticlesFadeDistance");
                var invSoftParticleFade = (VFXValue.Constant(1.0f) / softParticleFade.exp);
                yield return new VFXNamedExpression(invSoftParticleFade, "invSoftParticlesFadeDistance");
            }

            if (flipbookMode != FlipbookMode.Off)
            {
                var flipBookSizeExp = slotExpressions.First(o => o.name == "flipBookSize");
                yield return flipBookSizeExp;
                yield return new VFXNamedExpression(VFXValue.Constant(Vector2.one) / flipBookSizeExp.exp, "invFlipBookSize");
            }
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            if (target == VFXDeviceTarget.GPU)
            {
                var gpuMapper = VFXExpressionMapper.FromBlocks(activeChildrenWithImplicit);
                gpuMapper.AddExpressions(CollectGPUExpressions(GetExpressionsFromSlots(this)), -1);
                return gpuMapper;
            }
            return new VFXExpressionMapper();
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                string inputPropertiesType = "InputProperties";
                if (flipbookMode != FlipbookMode.Off) inputPropertiesType = "InputPropertiesFlipbook";

                foreach (var property in PropertiesFromType(inputPropertiesType))
                    yield return property;

                if (blendMode == BlendMode.Masked)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "alphaThreshold"), 0.5f);
                if (useSoftParticle)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "softParticlesFadeDistance"), 1.0f);
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                if (blendMode == BlendMode.Masked)
                    yield return "USE_ALPHA_TEST";
                if (useSoftParticle)
                    yield return "USE_SOFT_PARTICLE";

                VFXAsset asset = GetAsset();
                if (asset != null)
                {
                    var settings = asset.rendererSettings;
                    if (settings.motionVectorGenerationMode == MotionVectorGenerationMode.Object)
                        yield return "USE_MOTION_VECTORS_PASS";
                    if (settings.shadowCastingMode != ShadowCastingMode.Off)
                        yield return "USE_CAST_SHADOWS_PASS";
                }

                if (HasIndirectDraw())
                    yield return "VFX_HAS_INDIRECT_DRAW";

                if (flipbookMode != FlipbookMode.Off)
                {
                    yield return "USE_FLIPBOOK";
                    if (flipbookMode == FlipbookMode.FlipbookBlend)
                        yield return "USE_FLIPBOOK_INTERPOLATION";
                }
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (!supportsFlipbooks)
                    yield return "flipbookMode";
                if (flipbookMode == FlipbookMode.Off)
                    yield return "frameInterpolationMode";
            }
        }

        public override IEnumerable<KeyValuePair<string, VFXShaderWriter>> additionnalReplacements
        {
            get
            {
                var renderState = new VFXShaderWriter();

                if (blendMode == BlendMode.Additive)
                    renderState.WriteLine("Blend SrcAlpha One");
                else if (blendMode == BlendMode.Alpha)
                    renderState.WriteLine("Blend SrcAlpha OneMinusSrcAlpha");
                else if (blendMode == BlendMode.AlphaPremultiplied)
                    renderState.WriteLine("Blend One OneMinusSrcAlpha");

                renderState.WriteLine("ZTest LEqual");

                if (blendMode == BlendMode.Masked)
                    renderState.WriteLine("ZWrite On");
                else
                    renderState.WriteLine("ZWrite Off");

                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXOutputRenderState}", renderState);

                var shaderTags = new VFXShaderWriter();
                if (blendMode == BlendMode.Masked)
                    shaderTags.Write("Tags { \"Queue\"=\"AlphaTest\" \"IgnoreProjector\"=\"False\" \"RenderType\"=\"Opaque\" }");
                else
                    shaderTags.Write("Tags { \"Queue\"=\"Transparent\" \"IgnoreProjector\"=\"True\" \"RenderType\"=\"Transparent\" }");

                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXShaderTags}", shaderTags);
            }
        }

        public override IEnumerable<VFXMapping> additionalMappings
        {
            get
            {
                yield return new VFXMapping(sortPriority, "sortPriority");
                if (HasIndirectDraw())
                    yield return new VFXMapping(1, "indirectDraw");
            }
        }
    }
}
