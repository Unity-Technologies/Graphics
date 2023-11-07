namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Reciprocal")]
    [VFXInfo(name = "Reciprocal (1/x)", category = "Math/Arithmetic", synonyms = new []{ "inverse" })]
    class Reciprocal : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            public float x = 1.0f;
        }

        protected override sealed string operatorName { get { return "Reciprocal (1/x)"; } }

        protected override double defaultValueDouble
        {
            get
            {
                return 1.0;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var expression = inputExpression[0];
            return new[] { VFXOperatorUtility.Reciprocal(expression) };
        }

        protected sealed override ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowEverythingExceptIntegerAndDirection;
            }
        }
    }
}
