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

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var d in base.additionalDefines)
                    yield return d;

                yield return "USE_MOTION_VECTORS";
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

                // Motion vectors
                yield return new VFXAttributeInfo(VFXAttribute.OldPosition, VFXAttributeMode.Read);
            }
        }

        /* protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
         {
             foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                 yield return exp;

             // Motion vectors
             yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");
         }*/
    }
}
