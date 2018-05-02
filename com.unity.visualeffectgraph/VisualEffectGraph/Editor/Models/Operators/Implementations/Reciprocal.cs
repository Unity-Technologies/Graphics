using System;
namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class Reciprocal : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Reciprocal (1/x)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var expression = inputExpression[0];
            return new[] { VFXOperatorUtility.OneExpression[expression.valueType] / expression };
        }
    }
}
