namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Holds information on a parsed unary operation.
    /// </summary>
    public readonly struct UnaryOperation : IOperation
    {
        public readonly OperationType Type;
        public readonly IExpression Operand;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnaryOperation"/> class.
        /// </summary>
        /// <param name="type">Type of the unary operation.</param>
        /// <param name="operand">Operand of the unary operation.</param>
        public UnaryOperation(OperationType type, IExpression operand)
        {
            Type = type;
            Operand = operand;
        }

        /// <summary>
        /// Returns a string that represents the parsed unary operation.
        /// </summary>
        /// <returns>A string that represents the parsed unary operation.</returns>
        public override string ToString() => $"{MathExpressionParser.Ops[Type].Str}{Operand}";
    }
}
