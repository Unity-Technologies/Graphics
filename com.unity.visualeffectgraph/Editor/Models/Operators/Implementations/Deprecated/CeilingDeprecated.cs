using System;

namespace UnityEditor.VFX.Operator
{
    class CeilingDeprecated : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Ceiling (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Ceil(inputExpression[0]) };
        }

        public override sealed void Sanitize(int version)
        {
            base.Sanitize(version);
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Ceiling));
        }
    }
}
