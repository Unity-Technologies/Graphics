using System.Collections.Generic;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionStripTangent : VFXExpression
    {
        public VFXExpressionStripTangent() : base(VFXExpression.Flags.PerElement | VFXExpression.Flags.InvalidOnCPU) {}

        public override VFXValueType valueType => VFXValueType.Float3;
        public override VFXExpressionOperation operation => VFXExpressionOperation.None;

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GetStripTangent(attributes.position, relativeIndexInStrip, stripData)");
        }

        public override IEnumerable<VFXAttributeInfo> GetNeededAttributes()
        {
            yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
        }
    }
}
