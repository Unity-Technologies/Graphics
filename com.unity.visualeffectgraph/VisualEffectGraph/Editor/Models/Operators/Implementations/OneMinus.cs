using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class OneMinus : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "One Minus (1-x)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var input = inputExpression[0];
            var one = VFXOperatorUtility.OneExpression[VFXExpression.TypeToSize(input.valueType)];
            return new[] { one - input };
        }
    }
}
