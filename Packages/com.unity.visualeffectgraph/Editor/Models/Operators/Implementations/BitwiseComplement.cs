using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-BitwiseComplement")]
    [VFXInfo(category = "Bitwise")]
    class BitwiseComplement : VFXOperator
    {
        override public string name { get { return "Complement"; } }

        public class InputProperties
        {
            [Tooltip("Sets the operand")]
            public uint x = 0;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the result of the logical NOT operation for the specified operand. For example, 7 (0111 in binary) will return 8 (1000 in binary).")]
            public uint o = 0;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionBitwiseComplement(inputExpression[0]) };
        }
    }
}
