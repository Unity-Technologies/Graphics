using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorSaturate : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Saturate"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var size = VFXExpression.TypeToSize(inputExpression[0].ValueType);
            return new[] { VFXOperatorUtility.Clamp(inputExpression[0], VFXOperatorUtility.ZeroExpression[size], VFXOperatorUtility.OneExpression[size]) };
        }
    }
}

