using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using RenderingLayerMask = UnityEngine.Rendering.HighDefinition.RenderingLayerMask;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomPropertyDrawer(typeof(RenderingLayerMask))]
    class RenderingLayerMaskPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Draws a decal layer enum.
        /// </summary>
        /// <param name="position">The rect to draw.</param>
        /// <param name="property">The property to draw.</param>
        /// <param name="label">The label to draw.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            => HDEditorUtils.DrawRenderingLayerMask(position, property, label);

        //default height is good (= single line height)
    }
}
