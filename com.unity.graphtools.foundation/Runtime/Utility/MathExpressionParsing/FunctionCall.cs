using System.Collections.Generic;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Holds information on a parsed function call.
    /// </summary>
    public readonly struct FunctionCall : IOperation
    {
        public readonly string Id;
        public readonly List<IExpression> Arguments;

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionCall"/> class.
        /// </summary>
        /// <param name="id">The name of the function call.</param>
        /// <param name="arguments">The arguments provided to the function call.</param>
        public FunctionCall(string id, List<IExpression> arguments)
        {
            Id = id;
            Arguments = arguments;
        }

        /// <summary>
        /// Returns a string that represents the parsed function call.
        /// </summary>
        /// <returns>A string that represents the parsed function call.</returns>
        public override string ToString() => $"#{Id}({string.Join(", ", Arguments)})";
    }
}
