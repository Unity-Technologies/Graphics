using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Bunch of extension methods for <see cref="SerializedProperty"/>
    /// </summary>
    public static class SerializedPropertyExtension
    {
        public static IEnumerable<string> EnumerateDisplayName(this SerializedProperty property)
        {
            while (property.NextVisible(true))
                yield return property.displayName;
        }

        public static bool IsTargetAlive(this SerializedProperty property)
            => property != null && property.serializedObject.targetObject != null &&
            !property.serializedObject.targetObject.Equals(null);

        /// <summary>
        /// Helper to get an enum value from a SerializedProperty.
        /// This handle case where index do not correspond to enum value.
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
        ///         Debug.Log($"By GetEnumValue: {(MyEnum)serializedObject.FindProperty("theEnum").GetEnumValue<MyEnum>()}");   //write the value MyEnum.A
        ///     }
        /// }
        /// #endif
        /// </code>
        /// </example>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetEnumValue<T>(this SerializedProperty property)
            => GetEnumValue_Internal<T>(property);

        /// <summary>
        /// Helper to get an enum name from a SerializedProperty
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetEnumName<T>(this SerializedProperty property)
            where T : Enum
            => property.hasMultipleDifferentValues
            ? "MultipleDifferentValues"
            : property.enumNames[property.enumValueIndex];

        /// <summary>
        /// Helper to set an enum value to a SerializedProperty
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetEnumValue<T>(this SerializedProperty property, T value)
            // intValue actually is the value underlying beside the enum
            => SetEnumValue_Internal(property, value);

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
