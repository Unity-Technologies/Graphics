using System;
namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class Reciprocal : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Reciprocal (1/x)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var expression = inputExpression[0];
            return new[] { VFXOperatorUtility.OneExpression[expression.valueType] / expression };
        }
    }
}
