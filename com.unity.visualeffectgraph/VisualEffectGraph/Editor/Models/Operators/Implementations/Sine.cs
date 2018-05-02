using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Trigonometry")]
    class Sine : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Sine"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSin(inputExpression[0]) };
        }
    }
}
