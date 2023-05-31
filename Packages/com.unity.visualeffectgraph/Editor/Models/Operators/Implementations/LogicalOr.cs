using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-LogicOr")]
    [VFXInfo(category = "Logic")]
    class LogicalOr : VFXOperator
    {
        override public string name { get { return "Or"; } }

        public class InputProperties
        {
            static public bool FallbackValue = false;
            [Tooltip("Sets the first operand.")]
            public bool a = FallbackValue;
            [Tooltip("Sets the second operand.")]
            public bool b = FallbackValue;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs true if at least one operand is true. Otherwise, outputs false.")]
            public bool o = false;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionLogicalOr(inputExpression[0], inputExpression[1]) };
        }
    }
}
