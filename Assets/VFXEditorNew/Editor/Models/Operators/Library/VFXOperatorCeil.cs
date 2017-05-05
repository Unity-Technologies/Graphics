using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorCeil : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Ceil"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var one = VFXOperatorUtility.OneExpression[VFXExpression.TypeToSize(inputExpression[0].ValueType)];
            var sum = new VFXExpressionAdd(inputExpression[0], one);
            return new[] { new VFXExpressionFloor(sum) };
        }
    }
}
