using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class Multiply : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Multiply"; } }

        override protected VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return a * b;
        }
    }
}
