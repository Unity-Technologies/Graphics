using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class Add : VFXOperatorBinaryFloatOperationZero
    {
        override public string name { get { return "Add (deprecated)"; } }

        override protected VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return a + b;
        }

        public override sealed void Sanitize()
        {
            SanitizeHelper.SanitizeToOperatorNew(this, typeof(AddNew));
        }
    }
}
