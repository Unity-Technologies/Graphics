using System;

namespace UnityEditor.VFX.Operator
{
    class AddDeprecated : VFXOperatorBinaryFloatOperationZero
    {
        override public string name { get { return "Add (deprecated)"; } }

        override protected VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return a + b;
        }

        public override sealed void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Add));
        }
    }
}
