using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Trigonometry")]
    class Cosine : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Cosine"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionCos(inputExpression[0]) };
        }
    }
}
