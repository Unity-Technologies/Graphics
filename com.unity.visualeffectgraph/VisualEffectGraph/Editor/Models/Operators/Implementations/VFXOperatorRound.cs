using System;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Math")]
    class VFXOperatorRound : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Round"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var half = VFXOperatorUtility.HalfExpression[VFXExpression.TypeToSize(inputExpression[0].valueType)];
            return new[] { new VFXExpressionFloor(inputExpression[0] + half) };
        }
    }
}
