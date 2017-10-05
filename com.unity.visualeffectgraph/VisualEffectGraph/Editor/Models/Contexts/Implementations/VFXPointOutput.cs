using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXPointOutput : VFXContext
    {
        public VFXPointOutput() : base(VFXContextType.kOutput, VFXDataType.kParticle, VFXDataType.kNone) {}
        public override string name { get { return "Point Output"; } }
        public override string codeGeneratorTemplate { get { return "VFXShaders/VFXParticlePoints"; } }
        public override bool codeGeneratorCompute { get { return false; } }
        public override VFXTaskType taskType { get { return VFXTaskType.kParticlePointOutput; } }

        public override IEnumerable<KeyValuePair<string, VFXShaderWriter>> additionnalReplacements
        {
            get
            {
                var renderState = new VFXShaderWriter();
                renderState.WriteLine("ZWrite On");
                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXOutputRenderState}", renderState);
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
            }
        }
    }
}
