using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXQuadOutput : VFXAbstractParticleOutput
    {
        public override string name { get { return "Quad Output"; } }
        public override string codeGeneratorTemplate { get { return "VFXShaders/VFXParticleQuad"; } }
        public override VFXTaskType taskType { get { return VFXTaskType.kParticleQuadOutput; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Front, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Side, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Up, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Angle, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Pivot, VFXAttributeMode.Read);
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            return base.CollectGPUExpressions(slotExpressions).Concat(slotExpressions.Where(o => o.name == "texture"));
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                return base.inputProperties.Concat(PropertiesFromType(GetInputPropertiesTypeName()));
            }
        }

        public class InputProperties
        {
            public Texture2D texture;
        }
    }
}
