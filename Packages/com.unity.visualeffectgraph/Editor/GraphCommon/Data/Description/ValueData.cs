
namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Abstract base class describing a single data value.
    /// </summary>
    /*public*/ abstract class ValueData : IDataDescription
    {
        /// <summary>
        /// Gets the type of the data described by this object.
        /// </summary>
        public abstract System.Type Type { get; }

        /// <summary>
        /// Creates an instance of <see cref="ValueData{T}"/> for the specified data type.
        /// </summary>
        /// <param name="type">
        /// The type parameter for the generic instantiation of <see cref="ValueData{T}"/>.
        /// </param>
        /// <returns>
        /// A new instance of <see cref="ValueData{T}"/> with the specified type parameter.
        /// </returns>
        public static ValueData Create(System.Type type)
        {
            var genericType = typeof(ValueData<>).MakeGenericType(type);
            return (ValueData)System.Activator.CreateInstance(genericType);
        }
    }

    /// <summary>
    /// Describes a data element of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the data described by this instance.</typeparam>
    /*public*/ class ValueData<T> : ValueData
    {
        /// <inheritdoc cref="ValueData"/>
        public override System.Type Type => typeof(T);
    }
}
