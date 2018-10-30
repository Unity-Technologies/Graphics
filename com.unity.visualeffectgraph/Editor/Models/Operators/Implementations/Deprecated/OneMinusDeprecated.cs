using System;

namespace UnityEditor.VFX.Operator
{
    class OneMinusDeprecated : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "One Minus (1-x) (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var input = inputExpression[0];
            var one = VFXOperatorUtility.OneExpression[input.valueType];
            return new[] { one - input };
        }

        public sealed override void Sanitize(int version)
        {
            base.Sanitize(version);
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(OneMinus));
        }
    }
}
