using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-RatioOverStrip")]
    [VFXInfo(name = "Get Ratio Over Strip [0..1]", category = "Attribute")]
    class RatioOverStrip : VFXOperator
    {
        public class OutputProperties
        {
            public float t = 0;
        }

        public override string name
        {
            get
            {
                return "Get Ratio Over Strip [0..1]";
            }
        }
        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression[] output = new VFXExpression[] { new VFXExpressionCastUintToFloat(new VFXAttributeExpression(VFXAttribute.ParticleIndexInStrip)) /
                (new VFXExpressionCastUintToFloat(new VFXAttributeExpression(VFXAttribute.ParticleCountInStrip)) - VFXOperatorUtility.OneExpression[VFXValueType.Float]) };
            return output;
        }
    }
}
