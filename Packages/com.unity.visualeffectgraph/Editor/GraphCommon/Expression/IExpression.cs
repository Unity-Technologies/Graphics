
namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Interface describing an expression to be evaluated by the graph.
    /// Expressions represent individual operations that need to be performed by the graph. They can receive other expressions as parameters, forming an expression tree.
    /// Expressions can be evaluated or simplified during compilation, if they have constant parameters. Otherwise, they will be evaluated on the CPU or GPU, depending on the task.
    /// </summary>
    /*public*/ interface IExpression
    {
        /// <summary>
        /// Type of the resulting value of the expression.
        /// </summary>
        System.Type ResultType { get; }

        /// <summary>
        /// Returns true if the expression is always constant.
        /// </summary>
        bool IsConstant { get; }

        /// <summary>
        /// List of parent expressions used as arguments.
        /// </summary>
        ReadOnlyList<IExpression> Parents { get; }

        /// <summary>
        /// Tests if the expression is valid for the provided parents.
        /// </summary>
        /// <param name="parents">List of parents to use as parameters.</param>
        /// <returns>True if the expression is valid for the parent expressions.</returns>
        bool Validate(ReadOnlyList<IExpression> parents) => true;

        /// <summary>
        /// Creates a new value expression that is equivalent to applying the expression to the provided parents.
        /// If the evaluation is not possible, it will throw an exception.
        /// </summary>
        /// <param name="parents">List of parents to use as parameters.</param>
        /// <returns>A new value expression equivalent to applying the expression to the provided parents.</returns>
        IValueExpression Evaluate(ReadOnlyList<IValueExpression> parents);

        /// <summary>
        /// Creates a new expression of the same type with the provided parents, or a reduced equivalent expression when it is possible.
        /// </summary>
        /// <param name="parents">List of parents to use as parameters.</param>
        /// <returns>A new expression of the same type with the provided parents, or a reduced equivalent expression.</returns>
        IExpression Reduce(ReadOnlyList<IExpression> parents) => (IExpression)System.Activator.CreateInstance(GetType(), parents);

        /// <summary>
        /// Obtains the HLSL code that evaluates this expression tree.
        /// </summary>
        /// <param name="parents">List of code strings produced by each parent expression.</param>
        /// <returns>The HLSL code that evaluates this expression tree.</returns>
        string GetCodeString(ReadOnlyList<string> parents);

        /// <summary>
        /// Converts this expression to an equivalent expression with result type T.
        /// </summary>
        /// <typeparam name="T">The result type of the returned expression.</typeparam>
        /// <returns>An equivalent expression with result type T, or null if the conversion is not possible.</returns>
        IExpression<T> Convert<T>() => Convert(typeof(T)) as IExpression<T>;

        /// <summary>
        /// Converts this expression to an equivalent expression with provided result type.
        /// </summary>
        /// <param name="type">Result type of the expression to be converted to.</param>
        /// <returns>An equivalent expression with provided result type, or null if the conversion is not possible.</returns>
        IExpression Convert(System.Type type);
    }

    /// <summary>
    /// Interface describing an expression to be evaluated by the graph, specialized for a specific result type.
    /// Expressions represent individual operations that need to be performed by the graph. They can receive other expressions as parameters, forming an expression tree.
    /// Expressions can be evaluated or simplified during compilation, if they have constant parameters. Otherwise, they will be evaluated on the CPU or GPU, depending on the task.
    /// </summary>
    /// <typeparam name="T">The result type of the expression.</typeparam>
    /*public*/ interface IExpression<T> : IExpression
    {
        /// <inheritdoc cref="IExpression"/>
        System.Type IExpression.ResultType => typeof(T);
    }

    /// <summary>
    /// Interface describing an expression which holds a fixed value.
    /// </summary>
    /*public*/ interface IValueExpression : IExpression
    {
        /// <summary>
        /// Fixed value of the expression as an object.
        /// </summary>
        object Value { get; }

        /// <summary>
        /// Tries to obtain the value of the expression as a specific type TValue.
        /// </summary>
        /// <typeparam name="TValue">The desired value type.</typeparam>
        /// <param name="value">The value of the expression if the TValue is compatible.</param>
        /// <returns>True if it is possible to convert the expression value to TValue, false otherwise.</returns>
        bool TryGetValue<TValue>(out TValue value);
    }

    /// <summary>
    /// Interface describing an expression which holds a fixed value.
    /// </summary>
    /// <typeparam name="T">The result type of the expression.</typeparam>
    /*public*/ interface IValueExpression<T> : IValueExpression, IExpression<T>
    {
        /// <inheritdoc cref="IValueExpression"/>
        bool IValueExpression.TryGetValue<TValue>(out TValue value)
        {
            if (Value is TValue convertedValue)
            {
                value = convertedValue;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Fixed value of the expression.
        /// </summary>
        new T Value { get; }
        object IValueExpression.Value => Value;
    }
}
