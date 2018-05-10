using System;

namespace UnityEditor.VFX.Operator
{
    class Floor : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Floor (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionFloor(inputExpression[0]) };
        }

        public sealed override void Sanitize()
        {
            SanitizeHelper.SanitizeToOperatorNew(this, typeof(FloorNew));
        }
    }
}
