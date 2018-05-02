using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Trigonometry")]
    class Tangent : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Tangent"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionTan(inputExpression[0]) };
        }
    }
}
