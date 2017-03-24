using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorFloat : VFXOperator
    {
        override public string name { get { return "Temp_Float"; } }
        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXValueFloat.Default };
        }
    }
}

