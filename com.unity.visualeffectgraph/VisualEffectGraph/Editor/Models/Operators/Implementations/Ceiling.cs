using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Clamp")]
    class Ceiling : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Ceiling"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Ceil(inputExpression[0]) };
        }
    }
}
