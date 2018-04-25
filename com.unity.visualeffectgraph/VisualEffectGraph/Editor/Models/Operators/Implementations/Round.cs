using System;
using UnityEditor.VFX;
namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Clamp")]
    class Round : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Round"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Round(inputExpression[0]) };
        }
    }
}
