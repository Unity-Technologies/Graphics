using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorFloor : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Floor"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionFloor(inputExpression[0]) };
        }
    }

}

