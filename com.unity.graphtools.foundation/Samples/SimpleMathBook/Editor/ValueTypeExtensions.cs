using System.Linq;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    /// <summary>
    /// Extension methods to add functionality to <see cref="ValueType"/>.
    /// </summary>
    public static class ValueTypeExtensions
    {
        /// <summary>
        /// Tells whether a value is a scalar number, like int or float, but not a vector.
        /// </summary>
        /// <param name="t">The type to test.</param>
        /// <returns>True if value is a number but not a vector, false otherwise.</returns>
        public static bool IsSingleNumber(this ValueType t) => t == ValueType.Float || t == ValueType.Int;

        /// <summary>
        /// Tells whether a value is numeric and has the same number of elements as another one.
        /// e.g. int and float match but Vector2 and Vector3 don't.
        /// </summary>
        /// <param name="a">The value to compare.</param>
        /// <param name="b">The other Value to compare to.</param>
        /// <returns>True if they have the same number of elements, false otherwise.</returns>
        public static bool IsNumsOfSameLengthAs(this ValueType a, ValueType b) => a == b || a.IsSingleNumber() && b.IsSingleNumber();

        private static (ValueType valueType, TypeHandle typeHandle)[] k_ValueTypeHandles =
        {
            (ValueType.Bool, TypeHandle.Bool),
            (ValueType.Float, TypeHandle.Float),
            (ValueType.Int, TypeHandle.Int),
            (ValueType.String, TypeHandle.String),
            (ValueType.Vector2, TypeHandle.Vector2),
            (ValueType.Vector3, TypeHandle.Vector3)
        };

        /// <summary>
        /// Gets a <see cref="ValueType"/> that matches a <see cref="TypeHandle"/>.
        /// </summary>
        /// <param name="typeHandle">The <see cref="TypeHandle"/> for which to find the <see cref="ValueType"/>.</param>
        /// <returns>The matching <see cref="ValueType"/> if any, default otherwise.</returns>
        public static ValueType GetValueType(this TypeHandle typeHandle)
        {
            return k_ValueTypeHandles.FirstOrDefault(t => t.typeHandle == typeHandle).valueType;
        }

        /// <summary>
        /// Gets a <see cref="TypeHandle"/> that matches a <see cref="ValueType"/>.
        /// </summary>
        /// <param name="valueType">The <see cref="ValueType"/> for which to find the <see cref="TypeHandle"/>.</param>
        /// <returns>The matching <see cref="TypeHandle"/> if any, default otherwise.</returns>
        public static TypeHandle GetTypeHandle(this ValueType valueType)
        {
            return k_ValueTypeHandles.FirstOrDefault(t => t.valueType == valueType).typeHandle;
        }
    }
}
