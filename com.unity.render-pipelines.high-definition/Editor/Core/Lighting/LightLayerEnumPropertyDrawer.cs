using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomPropertyDrawer(typeof(LightLayerEnum))]
    class LightLayerEnumPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Draws a light layer enum.
        /// </summary>
        /// <param name="position">The rect to draw.</param>
        /// <param name="property">The property to draw.</param>
        /// <param name="label">The label to draw.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            => HDEditorUtils.DrawLightLayerMask_Internal(position, label, property);

        //default height is good (= single line height)
    }
}
