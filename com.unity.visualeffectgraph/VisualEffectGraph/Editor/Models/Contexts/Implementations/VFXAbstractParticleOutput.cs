using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.VFX;

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
            Opaque,
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

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool preRefraction = false;

        public bool HasIndirectDraw() { return indirectDraw; }

        protected VFXAbstractParticleOutput() : base(VFXContextType.kOutput, VFXDataType.kParticle, VFXDataType.kNone) {}

        public override bool codeGeneratorCompute { get { return false; } }

        public virtual bool supportsFlipbooks { get { return false; } }

        public virtual bool supportSoftParticles { get { return useSoftParticle && (blendMode != BlendMode.Opaque && blendMode != BlendMode.Masked); } }

        protected virtual IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            if (blendMode == BlendMode.Masked)
                yield return slotExpressions.First(o => o.name == "alphaThreshold");

            if (supportSoftParticles)
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
                if (supportSoftParticles)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "softParticlesFadeDistance"), 1.0f);
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                if (blendMode == BlendMode.Masked || blendMode == BlendMode.Masked)
                    yield return "IS_OPAQUE_PARTICLE";
                if (blendMode == BlendMode.Masked)
                    yield return "USE_ALPHA_TEST";
                if (supportSoftParticles)
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

                if (blendMode == BlendMode.Masked || blendMode == BlendMode.Opaque)
                    yield return "preRefraction";

                if (blendMode == BlendMode.Opaque || blendMode == BlendMode.Masked)
                    yield return "useSoftParticle";
            }
        }

        public override IEnumerable<KeyValuePair<string, VFXShaderWriter>> additionalReplacements
        {
            get
            {
                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXOutputRenderState}", renderState);

                var shaderTags = new VFXShaderWriter();
                if (blendMode == BlendMode.Opaque)
                    shaderTags.Write("Tags { \"Queue\"=\"Geometry\" \"IgnoreProjector\"=\"False\" \"RenderType\"=\"Opaque\" }");
                else if (blendMode == BlendMode.Masked)
                    shaderTags.Write("Tags { \"Queue\"=\"AlphaTest\" \"IgnoreProjector\"=\"False\" \"RenderType\"=\"Opaque\" }");
                else
                {
                    string queueName = preRefraction ? "Geometry+750" : "Transparent"; // TODO Geometry + 750 is currently hardcoded value from HDRP...
                    shaderTags.Write(string.Format("Tags {{ \"Queue\"=\"{0}\" \"IgnoreProjector\"=\"True\" \"RenderType\"=\"Transparent\" }}", queueName));
                }

                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXShaderTags}", shaderTags);
            }
        }

        protected virtual VFXShaderWriter renderState
        {
            get
            {
                var rs = new VFXShaderWriter();

                if (blendMode == BlendMode.Additive)
                    rs.WriteLine("Blend SrcAlpha One");
                else if (blendMode == BlendMode.Alpha)
                    rs.WriteLine("Blend SrcAlpha OneMinusSrcAlpha");
                else if (blendMode == BlendMode.AlphaPremultiplied)
                    rs.WriteLine("Blend One OneMinusSrcAlpha");

                rs.WriteLine("ZTest LEqual");

                if (blendMode == BlendMode.Masked || blendMode == BlendMode.Opaque)
                    rs.WriteLine("ZWrite On");
                else
                    rs.WriteLine("ZWrite Off");

                return rs;
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
