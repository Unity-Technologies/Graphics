using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;



namespace UnityEngine.TestTools.Graphics
{
    //This class is a temporary solution before this property drawer is pushed to the test framework itself.
    //Once this class lands on the test framework, this one needs to be removed on another PR
    [CustomPropertyDrawer(typeof(ImageComparisonSettings))]
    public class ImageComparisonSettingsDrawer : PropertyDrawer
    {
        bool imageComparisonSettingFoldoutStatus = true;
        bool comparisonSettingsFoldoutStatus = false;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            imageComparisonSettingFoldoutStatus = EditorGUILayout.Foldout(imageComparisonSettingFoldoutStatus, label);
            if(imageComparisonSettingFoldoutStatus)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(property.FindPropertyRelative("UseHDR"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative("UseBackBuffer"));

                if(property.FindPropertyRelative("UseBackBuffer").boolValue)
                {
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("ImageResolution"));
                }
                else
                {
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("TargetWidth"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("TargetHeight"));
                }

                comparisonSettingsFoldoutStatus = EditorGUILayout.Foldout(comparisonSettingsFoldoutStatus, "Comparison Settings");
                if(comparisonSettingsFoldoutStatus)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("PerPixelCorrectnessThreshold"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("PerPixelGammaThreshold"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("PerPixelAlphaThreshold"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("AverageCorrectnessThreshold"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("IncorrectPixelsThreshold"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("ActiveImageTests"));
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("ActivePixelTests"));
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

        }

        //Hack to remove the empty space on above the property drawer.
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) { return 0; }


    }
}
