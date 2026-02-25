
using UnityEngine;
using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    [System.Flags]
    enum ExpressionFlags // To be defined, currently just a copy of what is in VFX Graph 1
    {
        None = 0,
        Value = 1 << 0, // Expression is a value, get/set can be called on it
        Foldable = 1 << 1, // Expression is not a constant but can be folded anyway
        Constant = 1 << 2, // Expression is a constant, it can be folded
        InvalidOnGPU = 1 << 3, // Expression can be evaluated on GPU
        InvalidOnCPU = 1 << 4, // Expression can be evaluated on CPU
        InvalidConstant = 1 << 5, // Expression can be folded (for UI) but constant folding is forbidden
        PerElement = 1 << 6, // Expression is per element
        PerSpawn = 1 << 7, // Expression relies on event attribute or spawn context
        NotCompilableOnCPU = InvalidOnCPU | PerElement //Helper to filter out invalid expression on CPU
    }

    /// <summary>
    /// IExpression base class implementation to simplify code common to most expressions.
    /// </summary>
    /*public*/ abstract class Expression : IExpression
    {
        readonly List<IExpression> m_Parents;

        /// <inheritdoc cref="IExpression"/>
        public virtual System.Type ResultType => throw new System.NotImplementedException();

        internal ExpressionFlags Flags { get; set; }

        /// <inheritdoc cref="IExpression"/>
        public bool IsConstant => Flags.HasFlag(ExpressionFlags.Constant);

        /// <inheritdoc cref="IExpression"/>
        public ReadOnlyList<IExpression> Parents => m_Parents;

        static readonly Dictionary<System.Type, System.Func<IExpression, IExpression>> s_Converters = new();

        /// <summary>
        /// Creates a new expression that uses other expressions as parents.
        /// Currently there is a max limit of 4 parents.
        /// </summary>
        /// <param name="parents">List of parents to use as parameters.</param>
        public Expression(params IExpression[] parents)
        {
            Debug.Assert(parents.Length <= 4);
            m_Parents = new(parents);
        }

        static Expression()
        {
            Register<bool>((expression) => new ConvertToBoolExpression(expression));
            Register<float>((expression) => new ConvertToFloatExpression(expression));
            Register<double>((expression) => new ConvertToDoubleExpression(expression));
            Register<int>((expression) => new ConvertToIntExpression(expression));
            Register<uint>((expression) => new ConvertToUintExpression(expression));
            Register<Vector2>((expression) => new ConvertToVector2Expression(expression));
            Register<Vector3>((expression) => new ConvertToVector3Expression(expression));
            Register<Vector4>((expression) => new ConvertToVector4Expression(expression));
            Register<Vector2Int>((expression) => new ConvertToVector2IntExpression(expression));
            Register<Vector3Int>((expression) => new ConvertToVector3IntExpression(expression));
        }

        /// <summary>
        /// Tests if the expression is valid with the current parents.
        /// </summary>
        /// <returns>True if the expression is valid for the current parent expressions.</returns>
        public bool Validate() => Validate(m_Parents);

        /// <inheritdoc cref="IExpression"/>
        public virtual bool Validate(ReadOnlyList<IExpression> parents) => true;

        /// <inheritdoc cref="IExpression"/>
        public abstract IValueExpression Evaluate(ReadOnlyList<IValueExpression> parents);

        /// <inheritdoc cref="IExpression"/>
        public virtual IExpression Reduce(ReadOnlyList<IExpression> parents) => this;

        /// <inheritdoc cref="IExpression"/>
        public abstract string GetCodeString(ReadOnlyList<string> parents);

        /// <inheritdoc cref="IExpression"/>
        public IExpression Convert(System.Type type)
        {
            if (ResultType == type) return this;

            if (!s_Converters.TryGetValue(type, out var converter))
            {
                throw new System.InvalidCastException();
            }

            return converter(this);
        }

        /// <summary>
        /// Registers a new conversion function for expression types.
        /// </summary>
        /// <typeparam name="T">The result type of the expression to convert to.</typeparam>
        /// <param name="converter">Function that converts any expression to the desired type.</param>
        public static void Register<T>(System.Func<IExpression, IExpression> converter)
        {
            Debug.Assert(converter != null);
            Debug.Assert(!s_Converters.ContainsKey(typeof(T)));
            s_Converters.Add(typeof(T), converter);
        }

        /// <summary>
        /// Converts a boolean value into an expression.
        /// </summary>
        /// <param name="value">The value to be transformed into an expression.</param>
        /// <returns>A new expression containing the value.</returns>
        public static explicit operator Expression(bool value) => new LiteralExpression<bool>(value);

        /// <summary>
        /// Converts a float value into an expression.
        /// </summary>
        /// <param name="value">The value to be transformed into an expression.</param>
        /// <returns>A new expression containing the value.</returns>
        public static explicit operator Expression(float value) => new LiteralExpression<float>(value);

        /// <summary>
        /// Converts a double value into an expression.
        /// </summary>
        /// <param name="value">The value to be transformed into an expression.</param>
        /// <returns>A new expression containing the value.</returns>
        public static explicit operator Expression(double value) => new LiteralExpression<double>(value);

        /// <summary>
        /// Converts an int value into an expression.
        /// </summary>
        /// <param name="value">The value to be transformed into an expression.</param>
        /// <returns>A new expression containing the value.</returns>
        public static explicit operator Expression(int value) => new LiteralExpression<int>(value);

        /// <summary>
        /// Converts a uint value into an expression.
        /// </summary>
        /// <param name="value">The value to be transformed into an expression.</param>
        /// <returns>A new expression containing the value.</returns>
        public static explicit operator Expression(uint value) => new LiteralExpression<uint>(value);

        /// <summary>
        /// Converts a Vector2 value into an expression.
        /// </summary>
        /// <param name="value">The value to be transformed into an expression.</param>
        /// <returns>A new expression containing the value.</returns>
        public static explicit operator Expression(Vector2 value) => new LiteralExpression<Vector2>(value);

        /// <summary>
        /// Converts a Vector3 value into an expression.
        /// </summary>
        /// <param name="value">The value to be transformed into an expression.</param>
        /// <returns>A new expression containing the value.</returns>
        public static explicit operator Expression(Vector3 value) => new LiteralExpression<Vector3>(value);

        /// <summary>
        /// Converts a Vector4 value into an expression.
        /// </summary>
        /// <param name="value">The value to be transformed into an expression.</param>
        /// <returns>A new expression containing the value.</returns>
        public static explicit operator Expression(Vector4 value) => new LiteralExpression<Vector4>(value);

        /// <summary>
        /// Converts a Vector2Int value into an expression.
        /// </summary>
        /// <param name="value">The value to be transformed into an expression.</param>
        /// <returns>A new expression containing the value.</returns>
        public static explicit operator Expression(Vector2Int value) => new LiteralExpression<Vector2Int>(value);

        /// <summary>
        /// Converts a Vector3Int value into an expression.
        /// </summary>
        /// <param name="value">The value to be transformed into an expression.</param>
        /// <returns>A new expression containing the value.</returns>
        public static explicit operator Expression(Vector3Int value) => new LiteralExpression<Vector3Int>(value);

        /// <summary>
        /// Creates a new expression representing a literal value of a specific type.
        /// </summary>
        /// <param name="type">The type of the value contained in the expression.</param>
        /// <param name="value">The value to be transformed into an expression.</param>
        /// <returns>A new expression representing the literal value.</returns>
        public static Expression CreateLiteral(System.Type type, object value)
        {
            Debug.Assert(type.IsAssignableFrom(value.GetType()));
            var genericType = typeof(LiteralExpression<>).MakeGenericType(type);
            return (Expression)System.Activator.CreateInstance(genericType, value);
        }
    }

    /// <summary>
    /// IExpression base class implementation to simplify code common to most expressions, specialized for a specific result type.
    /// </summary>
    /// <typeparam name="T">The result type of the expression.</typeparam>
    /*public*/ abstract class Expression<T> : Expression, IExpression<T>
    {
        /// <inheritdoc cref="IExpression"/>
        public sealed override System.Type ResultType => typeof(T);

        /// <summary>
        /// Creates a new expression that uses other expressions as parents.
        /// Currently there is a max limit of 4 parents.
        /// </summary>
        /// <param name="parents">List of parents to use as parameters.</param>
        public Expression(params IExpression[] parents) : base (parents)
        {
        }

        /// <summary>
        /// Converts a generic value into an expression of the same type.
        /// </summary>
        /// <param name="value">The value to be transformed into an expression.</param>
        /// <returns>A new expression containing the value.</returns>
        public static implicit operator Expression<T>(T value) => new LiteralExpression<T>(value);
    }

    /// <summary>
    /// Expression containing a single value.
    /// </summary>
    /// <typeparam name="T">The result type of the expression.</typeparam>
    /*public*/ abstract class ValueExpression<T> : Expression<T>, IValueExpression<T>
    {
        /// <inheritdoc cref="IValueExpression{T}"/>
        public abstract T Value { get; }

        /// <inheritdoc cref="IExpression"/>
        public override IValueExpression Evaluate(ReadOnlyList<IValueExpression> parents) => this;

        /// <inheritdoc cref="IExpression"/>
        public override string GetCodeString(ReadOnlyList<string> parents) => Value.ToString();
    }

    /// <summary>
    /// Expression containing a delegate that evaluates to a single value.
    /// </summary>
    /// <typeparam name="T">The result type of the expression.</typeparam>
    /*public*/ sealed class DelegateValueExpression<T> : ValueExpression<T>
    {
        /// <inheritdoc cref="ValueExpression{T}"/>
        public override T Value => ValueFunction();

        /// <summary>
        /// Returns the delegate to compute the value of the expression
        /// </summary>
        public System.Func<T> ValueFunction { get; }

        /// <summary>
        /// Creates a new DelegateValueExpression containing the provided delegate.
        /// </summary>
        /// <param name="valueFunction">The delegate that returns the value of the expression.</param>
        public DelegateValueExpression(System.Func<T> valueFunction)
        {
            ValueFunction = valueFunction;
        }
    }

    /// <summary>
    /// Expression containing a single constant value.
    /// </summary>
    /// <typeparam name="T">The result type of the expression.</typeparam>
    /*public*/ sealed class LiteralExpression<T> : ValueExpression<T>
    {
        /// <inheritdoc cref="ValueExpression{T}"/>
        public override T Value { get; }

        /// <summary>
        /// Creates a new LiteralExpression containing the provided value.
        /// </summary>
        /// <param name="value">The value that will be stored in the expression.</param>
        public LiteralExpression(T value)
        {
            Value = value;
            Flags |= ExpressionFlags.Constant;
        }
    }
}
