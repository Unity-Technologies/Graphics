using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorRound : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Round"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var half = VFXOperatorUtility.OneExpression[VFXExpression.TypeToSize(inputExpression[0].ValueType)];
            var sum = new VFXExpressionAdd(inputExpression[0], half);
            return new[] { new VFXExpressionFloor(sum) };
        }
    }
}

