using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Represents a named parameter associated with an expression, which can be used in tasks.
    /// </summary>
    /*public*/ class TaskParameter
    {
        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the expression associated with the parameter.
        /// </summary>
        public IExpression Expression { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskParameter"/> class with the specified name and expression.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="expression">The expression associated with the parameter.</param>
        public TaskParameter(string name, IExpression expression)
        {
            Name = name;
            Expression = expression;
        }
    }
}
