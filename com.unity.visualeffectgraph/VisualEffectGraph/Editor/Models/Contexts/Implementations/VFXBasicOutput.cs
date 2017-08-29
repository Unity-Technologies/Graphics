using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXBasicOutput : VFXContext
    {
        public VFXBasicOutput() : base(VFXContextType.kOutput, VFXDataType.kParticle, VFXDataType.kNone) {}
        public override string name { get { return "Output"; } }
        public override string codeGeneratorTemplate { get { return "VFXOutput"; } }
        public override bool codeGeneratorCompute { get { return false; } }

        public enum BlendMode
        {
            Additive,
            Alpha,
            Masked,
            Dithered
        }

        public class Settings
        {
            public bool useSoftParticle = false;
            public BlendMode blendMode = BlendMode.Alpha;
        }

        public class InputProperties
        {
            public float softParticlesFadeDistance = 1.0f;
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            var settings = GetSettings<Settings>();
            var gpuMapper = VFXExpressionMapper.FromBlocks(childrenWithImplicit);
            if (target == VFXDeviceTarget.GPU && settings.useSoftParticle)
            {
                var softParticleFade = GetExpressionsFromSlots(this).First(o => o.name == "softParticlesFadeDistance");
                var invSoftParticleFade = new VFXExpressionDivide(VFXValue.Constant(1.0f), softParticleFade.exp);
                gpuMapper.AddExpression(invSoftParticleFade, "invSoftParticlesFadeDistance", -1);
                return gpuMapper;
            }
            return gpuMapper;
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                var settings = GetSettings<Settings>();
                if (settings.useSoftParticle)
                {
                    yield return "USE_SOFT_PARTICLE";
                }
            }
        }

        public override IEnumerable<KeyValuePair<string, VFXShaderWriter>> additionnalReplacements
        {
            get
            {
                var setting = GetSettings<Settings>();
                var renderState = new VFXShaderWriter();

                if (setting.blendMode == BlendMode.Additive)
                    renderState.WriteLine("Blend SrcAlpha One");
                else if (setting.blendMode == BlendMode.Alpha)
                    renderState.WriteLine("Blend SrcAlpha OneMinusSrcAlpha");
                renderState.WriteLine("ZTest LEqual");
                if (setting.blendMode == BlendMode.Masked || setting.blendMode == BlendMode.Dithered)
                    renderState.WriteLine("ZWrite On");
                else
                    renderState.WriteLine("ZWrite Off");

                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXOutputRenderState}", renderState);
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
            }
        }
    }
}
