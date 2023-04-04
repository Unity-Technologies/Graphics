using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-BitwiseOr")]
    [VFXInfo(category = "Bitwise")]
    class BitwiseRightShift : VFXOperator
    {
        override public string name { get { return "Right Shift"; } }

        public class InputProperties
        {
            static public uint FallbackValue = 0;
            [Tooltip("Sets the first operand.")]
            public uint a = FallbackValue;
            [Tooltip("Sets the second operand.")]
            public uint b = FallbackValue;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the result of the logical >> operation between the two operands. For example, 9 (1001 in binary) right shifted by 1 returns 4 (0100 in binary).")]
            public uint o = 0;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionBitwiseRightShift(inputExpression[0], inputExpression[1]) };
        }
    }
}
