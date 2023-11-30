using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Length")]
    [VFXInfo(category = "Math/Vector", synonyms = new []{ "norm", "magnitude" })]
    class Length : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            [Tooltip("The vector to be used in the length calculation.")]
            public Vector3 x;
        }

        public class OutputProperties
        {
            [Tooltip("The length of x.")]
            public float l;
        }

        public override string name => "Length";

        protected override sealed ValidTypeRule typeFilter { get { return ValidTypeRule.allowEverythingExceptIntegerAndDirection; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Length(inputExpression[0]) };
        }
    }
}
