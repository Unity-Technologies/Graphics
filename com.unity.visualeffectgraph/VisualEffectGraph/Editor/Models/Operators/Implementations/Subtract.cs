using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class Subtract : VFXOperatorBinaryFloatOperationZero
    {
        override public string name { get { return "Subtract"; } }

        override protected VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return a - b;
        }
    }
}
