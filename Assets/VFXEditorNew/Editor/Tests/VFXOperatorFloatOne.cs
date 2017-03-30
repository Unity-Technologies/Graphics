using System;

namespace UnityEditor.VFX.Test
{
    class VFXOperatorFloatOne : VFXOperator
    {
        override public string name { get { return "Temp_Float_One"; } }
        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXValueFloat(1.0f, true) };
        }
    }
}


