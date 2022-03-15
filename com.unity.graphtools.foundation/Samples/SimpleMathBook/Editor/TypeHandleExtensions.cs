using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    /// <summary>
    /// Extension methods to add functionality to <see cref="TypeHandle"/>.
    /// </summary>
    public static class TypeHandleExtensions
    {
        /// <summary>
        /// Tells whether a value is a scalar number, like int or float, but not a vector.
        /// </summary>
        /// <param name="t">The type to test.</param>
        /// <returns>True if value is a number but not a vector, false otherwise.</returns>
        public static bool IsScalar(this TypeHandle t) => t == TypeHandle.Float || t == TypeHandle.Int;

        /// <summary>
        /// Tells whether arithmetic can be done with the two values.
        /// E.g. int and float match but Vector2 and Vector3 don't.
        /// </summary>
        /// <param name="a">The value to compare.</param>
        /// <param name="b">The other Value to compare to.</param>
        /// <returns>True if arithmetic can be done with the two values, false otherwise.</returns>
        public static bool IsCompatibleWith(this TypeHandle a, TypeHandle b) => a == b || a.IsScalar() && b.IsScalar();
    }
}
