using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class Sign : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Sign"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSign(inputExpression[0]) };
        }
    }
}
