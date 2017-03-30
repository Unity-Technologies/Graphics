using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorFrac : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Frac"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Frac(inputExpression[0]) };
        }
    }
}

