using UnityEngine;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomPropertyDrawer(typeof(LightLayerEnum))]
    public class LightLayerEnumPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();
            int lightLayers = HDEditorUtils.LightLayerMaskPropertyDrawer(position, label, property.intValue);
            if (EditorGUI.EndChangeCheck())
                property.intValue = lightLayers;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return HDEditorUtils.ComputeLightLayerMaskPropertyDrawerHeight();
        }
    }

}
