
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Expression that does a casting of the parent expression to a different result type.
    /// </summary>
    /// <typeparam name="T">The result type to cast to.</typeparam>
    /*public*/ abstract class CastExpression<T> : Expression<T>
    {
        /// <summary>
        /// String representing a valid type in HLSL to cast the result to.
        /// </summary>
        protected abstract string CastType { get; }

        /// <summary>
        /// Creates a new expression using the parent expression that will be converted.
        /// </summary>
        /// <param name="expression">The expression to cast to a new result type.</param>
        protected CastExpression(IExpression expression) : base(expression)
        {
        }

        /// <inheritdoc cref="IExpression"/>
        public override IValueExpression Evaluate(ReadOnlyList<IValueExpression> parents) => Evaluate(parents[0]);

        /// <summary>
        /// Creates a new value expression that is equivalent to applying the expression to the provided parent.
        /// If the evaluation is not possible, it will throw an exception.
        /// </summary>
        /// <param name="parent">Expression to be converted.</param>
        /// <returns>A new value expression equivalent to applying the expression to the provided parent.</returns>
        public virtual IValueExpression Evaluate(IValueExpression parent) => throw new System.InvalidCastException();

        /// <inheritdoc cref="IExpression"/>
        public override string GetCodeString(ReadOnlyList<string> parents) => $"({CastType})({parents[0]})";
    }

    /// <summary>
    /// Expression that converts the result of a parent expression to bool.
    /// </summary>
    /*public*/ class ConvertToBoolExpression : CastExpression<bool>
    {
        /// <inheritdoc cref="CastExpression{T}"/>
        protected override string CastType => "bool";

        /// <summary>
        /// Creates a new expression using the parent expression that will be converted.
        /// </summary>
        /// <param name="expression">The expression to convert.</param>
        public ConvertToBoolExpression(IExpression expression) : base(expression)
        {
        }

        /// <inheritdoc cref="CastExpression{T}"/>
        public override IValueExpression Evaluate(IValueExpression parent)
        {
            if (parent.TryGetValue(out bool value))
            {
                return parent;
            }
            if (parent.TryGetValue(out float floatValue))
            {
                value = floatValue != 0.0f;
            }
            if (parent.TryGetValue(out double doubleValue))
            {
                value = doubleValue != 0.0;
            }
            else if (parent.TryGetValue(out int intValue))
            {
                value = intValue != 0;
            }
            else if (parent.TryGetValue(out uint uintValue))
            {
                value = uintValue != 0;
            }
            else
            {
                base.Evaluate(parent);
            }

            return (Expression)value as IValueExpression;
        }
    }

    /// <summary>
    /// Expression that converts the result of a parent expression to float.
    /// </summary>
    /*public*/ class ConvertToFloatExpression : CastExpression<float>
    {
        /// <inheritdoc cref="CastExpression{T}"/>
        protected override string CastType => "float";

        /// <summary>
        /// Creates a new expression using the parent expression that will be converted.
        /// </summary>
        /// <param name="expression">The expression to convert.</param>
        public ConvertToFloatExpression(IExpression expression) : base(expression)
        {
        }

        /// <inheritdoc cref="CastExpression{T}"/>
        public override IValueExpression Evaluate(IValueExpression parent)
        {
            if (parent.TryGetValue(out float value))
            {
                return parent;
            }
            if (parent.TryGetValue(out double doubleValue))
            {
                value = (float)doubleValue;
            }
            else if (parent.TryGetValue(out int intValue))
            {
                value = intValue;
            }
            else if (parent.TryGetValue(out uint uintValue))
            {
                value = uintValue;
            }
            else
            {
                base.Evaluate(parent);
            }

            return (Expression)value as IValueExpression;
        }
    }

    /// <summary>
    /// Expression that converts the result of a parent expression to double.
    /// </summary>
    /*public*/ class ConvertToDoubleExpression : CastExpression<double>
    {
        /// <inheritdoc cref="CastExpression{T}"/>
        protected override string CastType => "double";

        /// <summary>
        /// Creates a new expression using the parent expression that will be converted.
        /// </summary>
        /// <param name="expression">The expression to convert.</param>
        public ConvertToDoubleExpression(IExpression expression) : base(expression)
        {
        }

        /// <inheritdoc cref="CastExpression{T}"/>
        public override IValueExpression Evaluate(IValueExpression parent)
        {
            if (parent.TryGetValue(out double value))
            {
                return parent;
            }
            if (parent.TryGetValue(out float floatValue))
            {
                value = floatValue;
            }
            else if (parent.TryGetValue(out int intValue))
            {
                value = intValue;
            }
            else if (parent.TryGetValue(out uint uintValue))
            {
                value = uintValue;
            }
            else
            {
                base.Evaluate(parent);
            }

            return (Expression)value as IValueExpression;
        }
    }

    /// <summary>
    /// Expression that converts the result of a parent expression to int.
    /// </summary>
    /*public*/ class ConvertToIntExpression : CastExpression<int>
    {
        /// <inheritdoc cref="CastExpression{T}"/>
        protected override string CastType => "int";

        /// <summary>
        /// Creates a new expression using the parent expression that will be converted.
        /// </summary>
        /// <param name="expression">The expression to convert.</param>
        public ConvertToIntExpression(IExpression expression) : base(expression)
        {
        }

        /// <inheritdoc cref="CastExpression{T}"/>
        public override IValueExpression Evaluate(IValueExpression parent)
        {
            if (parent.TryGetValue(out int value))
            {
                return parent;
            }
            else if (parent.TryGetValue(out uint uintValue))
            {
                value = (int)uintValue;
            }
            else if (parent.TryGetValue(out float floatValue))
            {
                value = Mathf.RoundToInt(floatValue);
            }
            else if (parent.TryGetValue(out double doubleValue))
            {
                value = Mathf.RoundToInt((float)doubleValue);
            }
            else
            {
                base.Evaluate(parent);
            }

            return (Expression)value as IValueExpression;
        }
    }

    /// <summary>
    /// Expression that converts the result of a parent expression to uint.
    /// </summary>
    /*public*/ class ConvertToUintExpression : CastExpression<uint>
    {
        /// <inheritdoc cref="CastExpression{T}"/>
        protected override string CastType => "uint";

        /// <summary>
        /// Creates a new expression using the parent expression that will be converted.
        /// </summary>
        /// <param name="expression">The expression to convert.</param>
        public ConvertToUintExpression(IExpression expression) : base(expression)
        {
        }

        /// <inheritdoc cref="CastExpression{T}"/>
        public override IValueExpression Evaluate(IValueExpression parent)
        {
            if (parent.TryGetValue(out uint value))
            {
                return parent;
            }
            else if (parent.TryGetValue(out int intValue))
            {
                value = (uint)intValue;
            }
            else if (parent.TryGetValue(out float floatValue))
            {
                value = (uint)Mathf.RoundToInt(floatValue);
            }
            else if (parent.TryGetValue(out double doubleValue))
            {
                value = (uint)Mathf.RoundToInt((float)doubleValue);
            }
            else
            {
                base.Evaluate(parent);
            }

            return (Expression)value as IValueExpression;
        }
    }

    /// <summary>
    /// Expression that converts the result of a parent expression to Vector2.
    /// </summary>
    /*public*/ class ConvertToVector2Expression : CastExpression<Vector2>
    {
        /// <inheritdoc cref="CastExpression{T}"/>
        protected override string CastType => "float2";

        /// <summary>
        /// Creates a new expression using the parent expression that will be converted.
        /// </summary>
        /// <param name="expression">The expression to convert.</param>
        public ConvertToVector2Expression(IExpression expression) : base(expression)
        {
        }

        /// <inheritdoc cref="CastExpression{T}"/>
        public override IValueExpression Evaluate(IValueExpression parent)
        {
            if (parent.TryGetValue(out Vector2 value))
            {
                return parent;
            }
            else 
            {
                // var floatExpression = ConvertToFloatExpression.Evaluate(parent) as IValueExpression<float>; // TODO: Evaluate should be static. Change when we support C#11
                var floatExpression = new ConvertToFloatExpression(parent).Evaluate(parent) as IValueExpression<float>;
                float floatValue = floatExpression.Value;
                value = new Vector2(floatValue, floatValue);
            }
            return (Expression)value as IValueExpression;
        }
    }

    /// <summary>
    /// Expression that converts the result of a parent expression to Vector3.
    /// </summary>
    /*public*/ class ConvertToVector3Expression : CastExpression<Vector3>
    {
        /// <inheritdoc cref="CastExpression{T}"/>
        protected override string CastType => "float3";

        /// <summary>
        /// Creates a new expression using the parent expression that will be converted.
        /// </summary>
        /// <param name="expression">The expression to convert.</param>
        public ConvertToVector3Expression(IExpression expression) : base(expression)
        {
        }

        /// <inheritdoc cref="CastExpression{T}"/>
        public override IValueExpression Evaluate(IValueExpression parent)
        {
            if (parent.TryGetValue(out Vector3 value))
            {
                return parent;
            }
            else
            {
                // var floatExpression = ConvertToFloatExpression.Evaluate(parent) as IValueExpression<float>; // TODO: Evaluate should be static. Change when we support C#11
                var floatExpression = new ConvertToFloatExpression(parent).Evaluate(parent) as IValueExpression<float>;
                float floatValue = floatExpression.Value;
                value = new Vector3(floatValue, floatValue, floatValue);
            }
            return (Expression)value as IValueExpression;
        }
    }

    /// <summary>
    /// Expression that converts the result of a parent expression to Vector4.
    /// </summary>
    /*public*/ class ConvertToVector4Expression : CastExpression<Vector4>
    {
        /// <inheritdoc cref="CastExpression{T}"/>
        protected override string CastType => "float4";

        /// <summary>
        /// Creates a new expression using the parent expression that will be converted.
        /// </summary>
        /// <param name="expression">The expression to convert.</param>
        public ConvertToVector4Expression(IExpression expression) : base(expression)
        {
        }

        /// <inheritdoc cref="CastExpression{T}"/>
        public override IValueExpression Evaluate(IValueExpression parent)
        {
            if (parent.TryGetValue(out Vector4 value))
            {
                return parent;
            }
            else
            {
                // var floatExpression = ConvertToFloatExpression.Evaluate(parent) as IValueExpression<float>; // TODO: Evaluate should be static. Change when we support C#11
                var floatExpression = new ConvertToFloatExpression(parent).Evaluate(parent) as IValueExpression<float>;
                float floatValue = floatExpression.Value;
                value = new Vector4(floatValue, floatValue, floatValue, floatValue);
            }
            return (Expression)value as IValueExpression;
        }
    }

    /// <summary>
    /// Expression that converts the result of a parent expression to Vector2Int.
    /// </summary>
    /*public*/ class ConvertToVector2IntExpression : CastExpression<Vector2Int>
    {
        /// <inheritdoc cref="CastExpression{T}"/>
        protected override string CastType => "int2";

        /// <summary>
        /// Creates a new expression using the parent expression that will be converted.
        /// </summary>
        /// <param name="expression">The expression to convert.</param>
        public ConvertToVector2IntExpression(IExpression expression) : base(expression)
        {
        }

        /// <inheritdoc cref="CastExpression{T}"/>
        public override IValueExpression Evaluate(IValueExpression parent)
        {
            if (parent.TryGetValue(out Vector2Int value))
            {
                return parent;
            }
            else
            {
                // var intExpression = ConvertToIntExpression.Evaluate(parent) as IValueExpression<int>; // TODO: Evaluate should be static. Change when we support C#11
                var intExpression = new ConvertToIntExpression(parent).Evaluate(parent) as IValueExpression<int>;
                int intValue = intExpression.Value;
                value = new Vector2Int(intValue, intValue);
            }
            return (Expression)value as IValueExpression;
        }
    }

    /// <summary>
    /// Expression that converts the result of a parent expression to Vector3Int.
    /// </summary>
    /*public*/ class ConvertToVector3IntExpression : CastExpression<Vector3Int>
    {
        /// <inheritdoc cref="CastExpression{T}"/>
        protected override string CastType => "int3";

        /// <summary>
        /// Creates a new expression using the parent expression that will be converted.
        /// </summary>
        /// <param name="expression">The expression to convert.</param>
        public ConvertToVector3IntExpression(IExpression expression) : base(expression)
        {
        }

        /// <inheritdoc cref="CastExpression{T}"/>
        public override IValueExpression Evaluate(IValueExpression parent)
        {
            if (parent.TryGetValue(out Vector3Int value))
            {
                return parent;
            }
            else
            {
                // var intExpression = ConvertToIntExpression.Evaluate(parent) as IValueExpression<int>; // TODO: Evaluate should be static. Change when we support C#11
                var intExpression = new ConvertToIntExpression(parent).Evaluate(parent) as IValueExpression<int>;
                int intValue = intExpression.Value;
                value = new Vector3Int(intValue, intValue, intValue);
            }
            return (Expression)value as IValueExpression;
        }
    }
}
