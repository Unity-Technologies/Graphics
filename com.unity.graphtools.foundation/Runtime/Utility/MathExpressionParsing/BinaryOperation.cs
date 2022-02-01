namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Holds information on a parsed binary operation.
    /// </summary>
    public readonly struct BinaryOperation : IOperation
    {
        public readonly OperationType Type;
        public readonly IExpression OperandA;
        public readonly IExpression OperandB;

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryOperation"/> class.
        /// </summary>
        /// <param name="type">Type of the binary operation.</param>
        /// <param name="operandA">First operand of the binary operation.</param>
        /// <param name="operandB">Second operand of the binary operation.</param>
        public BinaryOperation(OperationType type, IExpression operandA, IExpression operandB)
        {
            Type = type;
            OperandA = operandA;
            OperandB = operandB;
        }

        /// <summary>
        /// Returns a string that represents the parsed binary operation.
        /// </summary>
        /// <returns>A string that represents the parsed binary operation.</returns>
        public override string ToString() => $"({OperandA} {MathExpressionParser.Ops[Type].Str} {OperandB})";
    }
}
