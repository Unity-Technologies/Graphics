using System;

namespace UnityEditor.VFX.Operator
{
    class DivideDeprecated : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Divide (deprecated)"; } }

        override protected VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return a / b;
        }

        public sealed override void Sanitize(int version)
        {
            base.Sanitize(version);
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Divide));
        }
    }
}
