using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Extensions for <see cref="SerializedProperty"/>
    /// </summary>
    public static class SerializedPropertyExtension
    {
        /// <summary>
        /// Checks if the property target is alive
        /// </summary>
        /// <param name="property">The <see cref="SerializedProperty"/> to check </param>
        /// <returns>true, if the property is not null</returns>
        public static bool IsTargetAlive(this SerializedProperty property)
            => property != null && property.serializedObject.targetObject != null &&
            !property.serializedObject.targetObject.Equals(null);

        /// <summary>
        /// Helper to get an enum value from a SerializedProperty.
        /// This handle case where index do not correspond to enum value.
        /// </summary>
        /// <typeparam name="T">A valid <see cref="Enum"/></typeparam>
        /// <param name="property">The <see cref="SerializedProperty"/></param>
        /// <returns>The <see cref="Enum"/> value</returns>
        /// <example>
        /// <code>
        /// enum MyEnum
        /// {
        ///     A = 2,
        ///     B = 4,
        /// }
        /// public class MyObject : MonoBehavior
        /// {
        ///     public MyEnum theEnum = MyEnum.A;
        /// }
        /// #if UNITY_EDITOR
        /// [CustomEditor(typeof(MyObject))]
        /// class MyObjectEditor : Editor
        /// {
        ///     public override void OnInspectorGUI()
        ///     {
        ///         Debug.Log($"By enumValueIndex: {(MyEnum)serializedObject.FindProperty("theEnum").enumValueIndex}");         //write the value (MyEnum)(0)
        ///         Debug.Log($"By GetEnumValue: {(MyEnum)serializedObject.FindProperty("theEnum").GetEnumValue&lt;MyEnum&gt;()}"); //write the value MyEnum.A
        ///     }
        /// }
        /// #endif
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetEnumValue<T>(this SerializedProperty property)
            where T : Enum
            => GetEnumValue_Internal<T>(property);

        /// <summary>
        /// Helper to get an enum name from a SerializedProperty
        /// </summary>
        /// <typeparam name="T">A valid <see cref="Enum"/></typeparam>
        /// <param name="property">The <see cref="SerializedProperty"/></param>
        /// <returns>The string containing the name of the enum</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetEnumName<T>(this SerializedProperty property)
            where T : Enum
            => property.hasMultipleDifferentValues
            ? "MultipleDifferentValues"
            : property.enumNames[property.enumValueIndex];

        /// <summary>
        /// Helper to set an enum value to a SerializedProperty
        /// </summary>
        /// <typeparam name="T">A valid <see cref="Enum"/></typeparam>
        /// <param name="property">The <see cref="SerializedProperty"/></param>
        /// <param name="value">The value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetEnumValue<T>(this SerializedProperty property, T value)
            where T : Enum
            // intValue actually is the value underlying beside the enum
            => SetEnumValue_Internal(property, value);

        /// <summary>
        /// Get the value of a <see cref="SerializedProperty"/>.
        ///
        /// This function will be inlined by the compiler.
        /// Caution: The case of Enum is not handled here.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the value to get.
        ///
        /// It is expected to be a supported type by the <see cref="SerializedProperty"/>.
        /// </typeparam>
        /// <param name="serializedProperty">The property to get.</param>
        /// <returns>The value of the property.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetInline<T>(this SerializedProperty serializedProperty)
            where T : struct
        {
            if (typeof(T) == typeof(Color))
                return (T)(object)serializedProperty.colorValue;
            if (typeof(T) == typeof(string))
                return (T)(object)serializedProperty.stringValue;
            if (typeof(T) == typeof(double))
                return (T)(object)serializedProperty.doubleValue;
            if (typeof(T) == typeof(float))
                return (T)(object)serializedProperty.floatValue;
            if (typeof(T) == typeof(long))
                return (T)(object)serializedProperty.longValue;
            if (typeof(T) == typeof(int))
                return (T)(object)serializedProperty.intValue;
            if (typeof(T) == typeof(bool))
                return (T)(object)serializedProperty.boolValue;
            if (typeof(T) == typeof(BoundsInt))
                return (T)(object)serializedProperty.boundsIntValue;
            if (typeof(T) == typeof(Bounds))
                return (T)(object)serializedProperty.boundsValue;
            if (typeof(T) == typeof(RectInt))
                return (T)(object)serializedProperty.rectIntValue;
            if (typeof(T) == typeof(Rect))
                return (T)(object)serializedProperty.rectValue;
            if (typeof(T) == typeof(Quaternion))
                return (T)(object)serializedProperty.quaternionValue;
            if (typeof(T) == typeof(Vector2Int))
                return (T)(object)serializedProperty.vector2IntValue;
            if (typeof(T) == typeof(Vector4))
                return (T)(object)serializedProperty.vector4Value;
            if (typeof(T) == typeof(Vector3))
                return (T)(object)serializedProperty.vector3Value;
            if (typeof(T) == typeof(Vector2))
                return (T)(object)serializedProperty.vector2Value;
            if (typeof(T).IsEnum)
                return GetEnumValue_Internal<T>(serializedProperty);
            throw new ArgumentOutOfRangeException($"<{typeof(T)}> is not a valid type for a serialized property.");
        }

        /// <summary>
        /// Set the value of a <see cref="SerializedProperty"/>.
        ///
        /// This function will be inlined by the compiler.
        /// Caution: The case of Enum is not handled here.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the value to set.
        ///
        /// It is expected to be a supported type by the <see cref="SerializedProperty"/>.
        /// </typeparam>
        /// <param name="serializedProperty">The property to set.</param>
        /// <param name="value">The value to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetInline<T>(this SerializedProperty serializedProperty, T value)
            where T : struct
        {
            if (typeof(T) == typeof(Color))
            {
                serializedProperty.colorValue = (Color)(object)value;
                return;
            }
            if (typeof(T) == typeof(string))
            {
                serializedProperty.stringValue = (string)(object)value;
                return;
            }
            if (typeof(T) == typeof(double))
            {
                serializedProperty.doubleValue = (double)(object)value;
                return;
            }
            if (typeof(T) == typeof(float))
            {
                serializedProperty.floatValue = (float)(object)value;
                return;
            }
            if (typeof(T) == typeof(long))
            {
                serializedProperty.longValue = (long)(object)value;
                return;
            }
            if (typeof(T) == typeof(int))
            {
                serializedProperty.intValue = (int)(object)value;
                return;
            }
            if (typeof(T) == typeof(bool))
            {
                serializedProperty.boolValue = (bool)(object)value;
                return;
            }
            if (typeof(T) == typeof(BoundsInt))
            {
                serializedProperty.boundsIntValue = (BoundsInt)(object)value;
                return;
            }
            if (typeof(T) == typeof(Bounds))
            {
                serializedProperty.boundsValue = (Bounds)(object)value;
                return;
            }
            if (typeof(T) == typeof(RectInt))
            {
                serializedProperty.rectIntValue = (RectInt)(object)value;
                return;
            }
            if (typeof(T) == typeof(Rect))
            {
                serializedProperty.rectValue = (Rect)(object)value;
                return;
            }
            if (typeof(T) == typeof(Quaternion))
            {
                serializedProperty.quaternionValue = (Quaternion)(object)value;
                return;
            }
            if (typeof(T) == typeof(Vector2Int))
            {
                serializedProperty.vector2IntValue = (Vector2Int)(object)value;
                return;
            }
            if (typeof(T) == typeof(Vector4))
            {
                serializedProperty.vector4Value = (Vector4)(object)value;
                return;
            }
            if (typeof(T) == typeof(Vector3))
            {
                serializedProperty.vector3Value = (Vector3)(object)value;
                return;
            }
            if (typeof(T) == typeof(Vector2))
            {
                serializedProperty.vector2Value = (Vector2)(object)value;
                return;
            }
            if (typeof(T).IsEnum)
            {
                SetEnumValue_Internal(serializedProperty, value);
                return;
            }
            throw new ArgumentOutOfRangeException($"<{typeof(T)}> is not a valid type for a serialized property.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T GetEnumValue_Internal<T>(SerializedProperty property)
        // intValue actually is the value underlying beside the enum
            => (T)(object)property.intValue;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetEnumValue_Internal<T>(SerializedProperty property, T value)
        // intValue actually is the value underlying beside the enum
            => property.intValue = (int)(object)value;
    }
}
