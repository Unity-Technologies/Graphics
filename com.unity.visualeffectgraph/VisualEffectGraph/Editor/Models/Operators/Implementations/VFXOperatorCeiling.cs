using System;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Math")]
    class VFXOperatorCeiling : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Ceiling"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Ceil(inputExpression[0]) };
        }
    }
}
