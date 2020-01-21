using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipelineTest.TestGenerator;

namespace UnityEditor.Experimental.Rendering.HDPipelineTest.TestGenerator
{
    [CustomPropertyDrawer(typeof(MaterialModification))]
    public class MaterialModificationPropertyDrawer : PropertyDrawer
    {
        const string k_TextValue = "Value";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 2;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var nameProperty = property.FindPropertyRelative(nameof(MaterialModification.name));
            var kindProperty = property.FindPropertyRelative(nameof(MaterialModification.kind));

            EditorGUI.BeginProperty(position, label, property);

            var rect = new Rect(position) {height = EditorGUIUtility.singleLineHeight};
            EditorGUI.PropertyField(rect, nameProperty, EditorGUIUtility.TrTextContent(nameProperty.displayName));
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(rect, kindProperty, EditorGUIUtility.TrTextContent(kindProperty.displayName));
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            var kindValue = (MaterialModificationKind)kindProperty.enumValueIndex;
            SerializedProperty valueProperty = null;
            switch (kindValue)
            {
                case MaterialModificationKind.Bool:
                    valueProperty = property.FindPropertyRelative(nameof(MaterialModification.boolValue));
                    break;
                case MaterialModificationKind.Int:
                    valueProperty = property.FindPropertyRelative(nameof(MaterialModification.intValue));
                    break;
                case MaterialModificationKind.Float:
                    valueProperty = property.FindPropertyRelative(nameof(MaterialModification.floatValue));
                    break;
                case MaterialModificationKind.Texture:
                    valueProperty = property.FindPropertyRelative(nameof(MaterialModification.textureValue));
                    break;
            }

            EditorGUI.PropertyField(rect, valueProperty, EditorGUIUtility.TrTextContent(k_TextValue));

            EditorGUI.EndProperty();
        }
    }
}
