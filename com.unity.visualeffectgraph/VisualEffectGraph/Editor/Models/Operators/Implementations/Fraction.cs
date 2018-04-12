using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class Fraction : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Fraction"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Frac(inputExpression[0]) };
        }
    }
}
