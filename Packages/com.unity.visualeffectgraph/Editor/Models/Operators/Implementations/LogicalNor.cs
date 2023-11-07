using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-LogicNor")]
    [VFXInfo(name = "Nor", category = "Logic")]
    class LogicalNor : VFXOperator
    {
        override public string name { get { return "Nor"; } }

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
            [Tooltip("Outputs true if both operands are false. Otherwise, outputs false.")]
            public bool o = false;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionLogicalNot(new VFXExpressionLogicalOr(inputExpression[0], inputExpression[1])) };
        }
    }
}
