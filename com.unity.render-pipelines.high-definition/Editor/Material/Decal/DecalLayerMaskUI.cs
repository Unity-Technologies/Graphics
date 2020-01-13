using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    public class DecalLayerMaskUI
    {
        static readonly string[] k_Options = Enumerable.Range(0, DecalLayerMask.Capacity)
            .Select(i => $"Layer {i}")
            .ToArray();

        public static DecalLayerMask GUIField(Rect rect, GUIContent label, DecalLayerMask value) =>
            (DecalLayerMask) EditorGUI.MaskField(rect, label, (int) value, k_Options);

        public static DecalLayerMask GUILayoutField(GUIContent label, DecalLayerMask value)
        {
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            return GUIField(rect, label, value);
        }

        public static void GUILayoutMaterialProperty(GUIContent label, MaterialProperty property)
        {
            var decalLayerMaskValue = (DecalLayerMask) (int) property.floatValue;
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMixedValue;

            // The 'Everyone' label is shown only for bit all set to 1 for an int
            // But we want it to be shown also when all bit of the specified capacity
            // are set
            if ((decalLayerMaskValue & DecalLayerMask.Full) == DecalLayerMask.Full)
                decalLayerMaskValue = new DecalLayerMask(-1);

            decalLayerMaskValue = GUILayoutField(label, decalLayerMaskValue);
            EditorGUI.showMixedValue = false;

            if (!EditorGUI.EndChangeCheck()) return;
            if (decalLayerMaskValue == new DecalLayerMask(-1))
                decalLayerMaskValue = DecalLayerMask.Full;
            property.floatValue = (int) decalLayerMaskValue;
        }
    }
}
