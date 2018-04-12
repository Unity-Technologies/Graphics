using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Trigonometry")]
    class Sine : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Sine"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSin(inputExpression[0]) };
        }
    }
}
