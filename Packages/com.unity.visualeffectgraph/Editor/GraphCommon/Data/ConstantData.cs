using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Abstract base class describing a constant value of data.
    /// </summary>
    /*public*/ abstract class ConstantValueData : ValueData
    {
        /// <summary>
        /// Gets the constant value as an object.
        /// </summary>
        public abstract object ObjectValue { get; }

        /// <summary>
        /// Creates an instance of <see cref="ConstantValueData{T}"/> for the specified value.
        /// The type of the value is inferred from the object provided.
        /// </summary>
        /// <param name="value">
        /// The constant value to be encapsulated.
        /// </param>
        /// <returns>
        /// A new instance of <see cref="ConstantValueData{T}"/> containing the provided value.
        /// </returns>
        public static ConstantValueData Create(object value)
        {
            Debug.Assert(value != null);
            return Create(value, value.GetType());
        }

        /// <summary>
        /// Creates an instance of <see cref="ConstantValueData{T}"/> for the specified value and type.
        /// </summary>
        /// <param name="value">
        /// The constant value to be encapsulated.
        /// </param>
        /// <param name="type">
        /// The type parameter for the generic instantiation of <see cref="ConstantValueData{T}"/>.
        /// </param>
        /// <returns>
        /// A new instance of <see cref="ConstantValueData{T}"/> with the specified type parameter and value.
        /// </returns>
        public static ConstantValueData Create(object value, System.Type type)
        {
            var genericType = typeof(ConstantValueData<>).MakeGenericType(type);
            return (ConstantValueData)System.Activator.CreateInstance(genericType, value);
        }
    }

    /// <summary>
    /// Describes a constant data element of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the constant value described by this instance.</typeparam>
    /*public*/ class ConstantValueData<T> : ConstantValueData
    {
        /// <inheritdoc cref="ValueData.Type"/>
        public override System.Type Type => typeof(T);

        /// <inheritdoc cref="ConstantValueData.ObjectValue"/>
        public override object ObjectValue => Value;

        /// <summary>
        /// Gets the constant value of type <typeparamref name="T"/>.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ConstantValueData{T}"/> with the specified value.
        /// </summary>
        /// <param name="value">
        /// The constant value to be encapsulated.
        /// </param>
        public ConstantValueData(T value)
        {
            Value = value;
        }
    }
}
