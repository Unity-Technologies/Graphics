using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomPropertyDrawer(typeof(LightLayerEnum))]
    public class LightLayerEnumPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            => HDEditorUtils.DrawLightLayerMask_Internal(position, label, property);

        //default height is good (= single line height)
    }
}
