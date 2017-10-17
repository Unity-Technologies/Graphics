using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXPointOutput : VFXAbstractParticleOutput
    {
        public override string name { get { return "Point Output"; } }
        public override string codeGeneratorTemplate { get { return "VFXShaders/VFXParticlePoints"; } }
        public override VFXTaskType taskType { get { return VFXTaskType.kParticlePointOutput; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadCurrent);
                yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.ReadCurrent);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.ReadCurrent);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.ReadCurrent);
            }
        }
    }
}
