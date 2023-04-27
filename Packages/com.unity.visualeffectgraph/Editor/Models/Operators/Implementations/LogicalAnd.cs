using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-LogicAnd")]
    [VFXInfo(category = "Logic")]
    class LogicalAnd : VFXOperator
    {
        override public string name { get { return "And"; } }

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
            [Tooltip("Outputs true if both operands are true. Otherwise, outputs false.")]
            public bool o = false;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionLogicalAnd(inputExpression[0], inputExpression[1]) };
        }
    }
}
