using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    ///     UI utilities for <see cref="DecalLayerMask" />.
    /// </summary>
    public static class DecalLayerMaskUI
    {
        static readonly string[] k_Options = DecalLayerMask.LayerNames;

        /// <summary>Draw a DecalLayerMask mask field in a rect.</summary>
        /// <param name="rect">The rect to draw into.</param>
        /// <param name="label">The label of the field.</param>
        /// <param name="value">The value to drawn.</param>
        /// <returns>The edited value.</returns>
        public static DecalLayerMask GUIField(Rect rect, GUIContent label, DecalLayerMask value)
        {
            // The 'Everyone' label is shown only for bit all set to 1 for an int
            // But we want it to be shown also when all bit of the specified capacity
            // are set
            var intValue = (int) value;
            if ((value & DecalLayerMask.Full) == DecalLayerMask.Full)
                intValue = -1;

            var newIntValue = EditorGUI.MaskField(rect, label, intValue, k_Options);
            if (newIntValue == -1)
                value = DecalLayerMask.Full;
            else
                value = (DecalLayerMask) newIntValue;

            return value;
        }

        /// <summary>Draw a DecalLayerMask mask field with an automatic layout.</summary>
        /// <param name="label">The label of the field.</param>
        /// <param name="value">The value to drawn.</param>
        /// <param name="options">The options to use for the layout.</param>
        /// <returns>The edited value.</returns>
        public static DecalLayerMask GUILayoutField(GUIContent label, DecalLayerMask value,
            params GUILayoutOption[] options)
        {
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, options);
            return GUIField(rect, label, value);
        }

        /// <summary>Draw a DecalLayerMask field for a <see cref="MaterialProperty"/>.</summary>
        /// <param name="label">The label of the field.</param>
        /// <param name="property">The property to draw and update</param>
        /// <param name="options">The options to use for the layout.</param>
        public static void GUILayoutMaterialProperty(GUIContent label, MaterialProperty property,
            params GUILayoutOption[] options)
        {
            var decalLayerMaskValue = (DecalLayerMask) (int) property.floatValue;
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMixedValue;

            decalLayerMaskValue = GUILayoutField(label, decalLayerMaskValue, options);
            EditorGUI.showMixedValue = false;

            if (!EditorGUI.EndChangeCheck()) return;
            property.floatValue = (int) decalLayerMaskValue;
        }
    }
}
