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
        public class Settings
        {
            public bool useSoftParticle = false;
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
