using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorSqrt : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Sqrt"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Sqrt(inputExpression[0]) };
        }
    }
}
