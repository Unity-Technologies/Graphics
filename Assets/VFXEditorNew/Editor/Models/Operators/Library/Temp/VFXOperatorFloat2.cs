using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorFloat2 : VFXOperator
    {
        override public string name { get { return "Temp_Float2"; } }
        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXValueFloat2.Default };
        }
    }
}

