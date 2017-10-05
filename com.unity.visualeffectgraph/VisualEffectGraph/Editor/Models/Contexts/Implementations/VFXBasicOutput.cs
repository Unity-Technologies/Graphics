using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXBasicOutput : VFXContext
    {
        public VFXBasicOutput() : base(VFXContextType.kOutput, VFXDataType.kParticle, VFXDataType.kNone) {}
        public override string name { get { return "Output"; } }
        public override string codeGeneratorTemplate { get { return "VFXShaders/VFXOutput"; } }
        public override bool codeGeneratorCompute { get { return false; } }
        public override VFXTaskType taskType { get { return VFXTaskType.kOutput; } }

        public enum BlendMode
        {
            Additive,
            Alpha,
            Masked,
            Dithered
        }

        [VFXSetting]
        [SerializeField]
        private bool useSoftParticle = false;
        [VFXSetting]
        [SerializeField]
        private BlendMode blendMode = BlendMode.Alpha;

        public class InputProperties
        {
            public float softParticlesFadeDistance = 1.0f;
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            if (target == VFXDeviceTarget.GPU)
            {
                var gpuMapper = VFXExpressionMapper.FromBlocks(activeChildrenWithImplicit);
                if (useSoftParticle)
                {
                    var softParticleFade = GetExpressionsFromSlots(this).First(o => o.name == "softParticlesFadeDistance");
                    var invSoftParticleFade = (VFXValue.Constant(1.0f) / softParticleFade.exp);
                    gpuMapper.AddExpression(invSoftParticleFade, "invSoftParticlesFadeDistance", -1);
                }
                return gpuMapper;
            }
            return new VFXExpressionMapper();
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                if (useSoftParticle)
                {
                    yield return "USE_SOFT_PARTICLE";
                }
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
                renderState.WriteLine("ZTest LEqual");
                if (blendMode == BlendMode.Masked || blendMode == BlendMode.Dithered)
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
