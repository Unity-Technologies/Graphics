using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Clamp")]
    class Floor : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Floor"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionFloor(inputExpression[0]) };
        }
    }
}
