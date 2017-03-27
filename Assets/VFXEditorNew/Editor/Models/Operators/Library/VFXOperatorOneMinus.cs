using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorOneMinus : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "OneMinus"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var input = inputExpression[0];
            var one = VFXOperatorUtility.OneExpression[VFXExpression.TypeToSize(input.ValueType)];
            return new[] { new VFXExpressionSubtract(one, input) };
        }
    }
}

