using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Base expression for generic math operations.
    /// Splits the Evaluate function in different functions depending on the result type.
    /// </summary>
    /// <typeparam name="T">The result type of the expression.</typeparam>
    /*public*/ abstract class MathExpression<T> : Expression<T>
    {
        /// <summary>
        /// True if the result type is a floating point type (scalar or vector).
        /// </summary>
        public bool IsFloatingPoint => default(T) is float or double or Vector2 or Vector3 or Vector4;

        /// <summary>
        /// True if the result type is a vector.
        /// </summary>
        public bool IsVector => default(T) is Vector2 or Vector3 or Vector4 or Vector2Int or Vector3Int;

        /// <summary>
        /// Creates a new math expression with the parameter expressions as parents.
        /// </summary>
        /// <param name="parents">List of parents to use as parameters.</param>
        protected MathExpression(params IExpression[] parents) : base(parents)
        {
            // TODO: Convert parents to T?
        }

        /// <inheritdoc cref="IExpression"/>
        public override IValueExpression Evaluate(ReadOnlyList<IValueExpression> parents)
        {
            var values = new T[parents.Count]; // TODO: Could remove the alloc with some extra work
            for (int i = 0; i < parents.Count; ++i)
            {
                if (!parents[i].TryGetValue(out values[i]))
                {
                    throw new System.InvalidCastException();
                }
            }
            return values switch
            {
                bool[] v => (Expression)EvaluateOperation(v) as IValueExpression,
                float[] v => (Expression)EvaluateOperation(v) as IValueExpression,
                double[] v => (Expression)EvaluateOperation(v) as IValueExpression,
                int[] v => (Expression)EvaluateOperation(v) as IValueExpression,
                uint[] v => (Expression)EvaluateOperation(v) as IValueExpression,
                Vector2[] v => (Expression)EvaluateOperation(v) as IValueExpression,
                Vector3[] v => (Expression)EvaluateOperation(v) as IValueExpression,
                Vector4[] v => (Expression)EvaluateOperation(v) as IValueExpression,
                Vector2Int[] v => (Expression)EvaluateOperation(v) as IValueExpression,
                Vector3Int[] v => (Expression)EvaluateOperation(v) as IValueExpression,
                _ => default
            };
        }

        /// <summary>
        /// Evaluates the operation using the values from the parent expressions converted to bool.
        /// </summary>
        /// <param name="parents">List of values taken from the parent expressions.</param>
        /// <returns>The value of the operation applied to the parents.</returns>
        protected virtual bool EvaluateOperation(bool[] parents) => throw new System.NotImplementedException();

        /// <summary>
        /// Evaluates the operation using the values from the parent expressions converted to float.
        /// </summary>
        /// <param name="parents">List of values taken from the parent expressions.</param>
        /// <returns>The value of the operation applied to the parents.</returns>
        protected virtual float EvaluateOperation(float[] parents) => throw new System.NotImplementedException();

        /// <summary>
        /// Evaluates the operation using the values from the parent expressions converted to double.
        /// </summary>
        /// <param name="parents">List of values taken from the parent expressions.</param>
        /// <returns>The value of the operation applied to the parents.</returns>
        protected virtual double EvaluateOperation(double[] parents) => throw new System.NotImplementedException();

        /// <summary>
        /// Evaluates the operation using the values from the parent expressions converted to int.
        /// </summary>
        /// <param name="parents">List of values taken from the parent expressions.</param>
        /// <returns>The value of the operation applied to the parents.</returns>
        protected virtual int EvaluateOperation(int[] parents) => throw new System.NotImplementedException();

        /// <summary>
        /// Evaluates the operation using the values from the parent expressions converted to uint.
        /// </summary>
        /// <param name="parents">List of values taken from the parent expressions.</param>
        /// <returns>The value of the operation applied to the parents.</returns>
        protected virtual uint EvaluateOperation(uint[] parents) => throw new System.NotImplementedException();

        /// <summary>
        /// Evaluates the operation using the values from the parent expressions converted to Vector2.
        /// </summary>
        /// <param name="parents">List of values taken from the parent expressions.</param>
        /// <returns>The value of the operation applied to the parents.</returns>
        protected virtual Vector2 EvaluateOperation(Vector2[] parents)
        {
            Vector2 result = new Vector2();
            var values = new float[parents.Length];
            for (int j = 0; j < 2; ++j)
            {
                for (int i = 0; i < parents.Length; ++i)
                {
                    values[i] = parents[i][j];
                }
                result[j] = EvaluateOperation(values);
            }
            return result;
        }

        /// <summary>
        /// Evaluates the operation using the values from the parent expressions converted to Vector3.
        /// </summary>
        /// <param name="parents">List of values taken from the parent expressions.</param>
        /// <returns>The value of the operation applied to the parents.</returns>
        protected virtual Vector3 EvaluateOperation(Vector3[] parents)
        {
            Vector3 result = new Vector3();
            var values = new float[parents.Length];
            for (int j = 0; j < 3; ++j)
            {
                for (int i = 0; i < parents.Length; ++i)
                {
                    values[i] = parents[i][j];
                }
                result[j] = EvaluateOperation(values);
            }
            return result;
        }

        /// <summary>
        /// Evaluates the operation using the values from the parent expressions converted to Vector4.
        /// </summary>
        /// <param name="parents">List of values taken from the parent expressions.</param>
        /// <returns>The value of the operation applied to the parents.</returns>
        protected virtual Vector4 EvaluateOperation(Vector4[] parents)
        {
            Vector4 result = new Vector4();
            var values = new float[parents.Length];
            for (int j = 0; j < 4; ++j)
            {
                for (int i = 0; i < parents.Length; ++i)
                {
                    values[i] = parents[i][j];
                }
                result[j] = EvaluateOperation(values);
            }
            return result;
        }

        /// <summary>
        /// Evaluates the operation using the values from the parent expressions converted to Vector2Int.
        /// </summary>
        /// <param name="parents">List of values taken from the parent expressions.</param>
        /// <returns>The value of the operation applied to the parents.</returns>
        protected virtual Vector2Int EvaluateOperation(Vector2Int[] parents)
        {
            Vector2Int result = new Vector2Int();
            var values = new int[parents.Length];
            for (int j = 0; j < 2; ++j)
            {
                for (int i = 0; i < parents.Length; ++i)
                {
                    values[i] = parents[i][j];
                }
                result[j] = EvaluateOperation(values);
            }
            return result;
        }

        /// <summary>
        /// Evaluates the operation using the values from the parent expressions converted to Vector3sInt.
        /// </summary>
        /// <param name="parents">List of values taken from the parent expressions.</param>
        /// <returns>The value of the operation applied to the parents.</returns>
        protected virtual Vector3Int EvaluateOperation(Vector3Int[] parents)
        {
            Vector3Int result = new Vector3Int();
            var values = new int[parents.Length];
            for (int j = 0; j < 3; ++j)
            {
                for (int i = 0; i < parents.Length; ++i)
                {
                    values[i] = parents[i][j];
                }
                result[j] = EvaluateOperation(values);
            }
            return result;
        }
    }

    /// <summary>
    /// Expression that adds the result of two parent expressions.
    /// </summary>
    /// <typeparam name="T">The result type of the expression.</typeparam>
    /*public*/ class AddExpression<T> : MathExpression<T>
    {
        /// <summary>
        /// Creates a new expression with the two parent expressions.
        /// </summary>
        /// <param name="expression0">The first parent expression.</param>
        /// <param name="expression1">The second parent expression.</param>
        public AddExpression(IExpression expression0, IExpression expression1) : base(expression0.Convert<T>(), expression1.Convert<T>())
        {
        }

        /// <inheritdoc cref="IExpression"/>
        public override bool Validate(ReadOnlyList<IExpression> parents) => parents.Count == 2;

        /// <inheritdoc cref="IExpression"/>
        public override IExpression Reduce(ReadOnlyList<IExpression> parents)
        {
            Expression<T> zero = default(T);

            if (parents[0] == zero)
                return parents[1];

            if (parents[1] == zero)
                return parents[0];

            return base.Reduce(parents);
        }

        /// <inheritdoc cref="MathExpression{T}"/>
        protected override float EvaluateOperation(float[] parents) => parents[0] + parents[1];

        /// <inheritdoc cref="MathExpression{T}"/>
        protected override double EvaluateOperation(double[] parents) => parents[0] + parents[1];

        /// <inheritdoc cref="MathExpression{T}"/>
        protected override int EvaluateOperation(int[] parents) => parents[0] + parents[1];

        /// <inheritdoc cref="MathExpression{T}"/>
        protected override uint EvaluateOperation(uint[] parents) => parents[0] + parents[1];

        /// <inheritdoc cref="MathExpression{T}"/>
        protected override Vector2 EvaluateOperation(Vector2[] parents) => parents[0] + parents[1];

        /// <inheritdoc cref="MathExpression{T}"/>
        protected override Vector3 EvaluateOperation(Vector3[] parents) => parents[0] + parents[1];

        /// <inheritdoc cref="MathExpression{T}"/>
        protected override Vector4 EvaluateOperation(Vector4[] parents) => parents[0] + parents[1];

        /// <inheritdoc cref="MathExpression{T}"/>
        protected override Vector2Int EvaluateOperation(Vector2Int[] parents) => parents[0] + parents[1];

        /// <inheritdoc cref="MathExpression{T}"/>
        protected override Vector3Int EvaluateOperation(Vector3Int[] parents) => parents[0] + parents[1];

        /// <inheritdoc cref="IExpression"/>
        public override string GetCodeString(ReadOnlyList<string> parents) => $"{parents[0]} + {parents[1]}";
    }

    /// <summary>
    /// Expression that computes the cosine of the parent expression.
    /// </summary>
    /// <typeparam name="T">The result type of the expression.</typeparam>
    /*public*/ class CosineExpression<T> : MathExpression<T>
    {
        /// <summary>
        /// Creates a new expression with the parent expression.
        /// </summary>
        /// <param name="expression">The parent expression.</param>
        public CosineExpression(IExpression expression) : base(expression.Convert<T>())
        {
        }

        /// <inheritdoc cref="IExpression"/>
        public override bool Validate(ReadOnlyList<IExpression> parents) => parents.Count == 1 && IsFloatingPoint;

        /// <inheritdoc cref="MathExpression{T}"/>
        protected override float EvaluateOperation(float[] parents) => Mathf.Cos(parents[0]);

        /// <inheritdoc cref="MathExpression{T}"/>
        protected override double EvaluateOperation(double[] parents) => System.Math.Cos(parents[0]);

        /// <inheritdoc cref="IExpression"/>
        public override string GetCodeString(ReadOnlyList<string> parents) => $"{parents[0]}";
    }
}
