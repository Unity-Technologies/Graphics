using System;

namespace UnityEditor.VFX.Operator
{
    class SubtractDeprecated : VFXOperatorBinaryFloatOperationZero
    {
        override public string name { get { return "Subtract (deprecated)"; } }

        override protected VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return a - b;
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Subtract));
        }
    }
}
