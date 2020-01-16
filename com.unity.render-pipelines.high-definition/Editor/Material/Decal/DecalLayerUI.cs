using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    ///     UI utilities for <see cref="DecalLayer" />.
    /// </summary>
    public static class DecalLayerUI
    {
        static readonly GUIContent[] k_Options = DecalLayer.LayerNames
            .Select(s => new GUIContent(s)).ToArray();

        /// <summary>Draw a DecalLayer mask field in a rect.</summary>
        /// <param name="rect">The rect to draw into.</param>
        /// <param name="label">The label of the field.</param>
        /// <param name="value">The value to drawn.</param>
        /// <returns>The edited value.</returns>
        public static DecalLayer GUIField(Rect rect, GUIContent label, DecalLayer value) => (DecalLayer)EditorGUI.Popup(rect, label, (int) value, k_Options);

        /// <summary>Draw a DecalLayer mask field with an automatic layout.</summary>
        /// <param name="label">The label of the field.</param>
        /// <param name="value">The value to drawn.</param>
        /// <param name="options">The options to use for the layout.</param>
        /// <returns>The edited value.</returns>
        public static DecalLayer GUILayoutField(GUIContent label, DecalLayer value,
            params GUILayoutOption[] options)
        {
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, options);
            return GUIField(rect, label, value);
        }

        /// <summary>Draw a DecalLayer field for a <see cref="MaterialProperty"/>.</summary>
        /// <param name="label">The label of the field.</param>
        /// <param name="property">The property to draw and update</param>
        /// <param name="options">The options to use for the layout.</param>
        public static void GUILayoutMaterialProperty(GUIContent label, MaterialProperty property,
            params GUILayoutOption[] options)
        {
            var decalLayerValue = (DecalLayer) (int) property.floatValue;
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMixedValue;

            decalLayerValue = GUILayoutField(label, decalLayerValue, options);
            EditorGUI.showMixedValue = false;

            if (!EditorGUI.EndChangeCheck()) return;
            property.floatValue = (int) decalLayerValue;
        }
    }

    [CustomPropertyDrawer(typeof(DecalLayer))]
    class DecalLayerPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            var decalLayer = new SerializedDecalLayer(property);
            decalLayer.value = DecalLayerUI.GUIField(position, label, decalLayer.value);
            EditorGUI.EndProperty();
        }
    }
}
