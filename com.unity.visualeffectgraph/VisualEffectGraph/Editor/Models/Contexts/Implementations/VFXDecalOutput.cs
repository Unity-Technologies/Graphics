using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXDecalOutput : VFXAbstractParticleOutput
    {
        public override string name { get { return "Decal Output"; } }
        public override string codeGeneratorTemplate { get { return "VFXShaders/VFXParticleDecal"; } }
        public override VFXTaskType taskType { get { return VFXTaskType.kParticleHexahedronOutput; } }

        public class InputProperties
        {
            public Texture2D texture;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                return base.inputProperties.Concat(PropertiesFromType(GetInputPropertiesTypeName()));
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            yield return slotExpressions.First(o => o.name == "texture");
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Pivot, VFXAttributeMode.Read);
                foreach (var size in VFXBlockUtility.GetReadableSizeAttributes(GetData()))
                    yield return size;
            }
        }
    }
}
