using System;
namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class Absolute : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Absolute"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionAbs(inputExpression[0]) };
        }
    }
}
