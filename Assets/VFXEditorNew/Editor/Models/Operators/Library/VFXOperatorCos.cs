using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorCos : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Cos"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionCos(inputExpression[0]) };
        }
    }
}

