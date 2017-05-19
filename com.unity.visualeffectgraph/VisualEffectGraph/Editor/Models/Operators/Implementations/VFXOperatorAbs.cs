using System;
namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorAbs : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Abs"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionAbs(inputExpression[0]) };
        }
    }
}
