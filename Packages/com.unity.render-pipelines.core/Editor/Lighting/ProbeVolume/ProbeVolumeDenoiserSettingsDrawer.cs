using UnityEditor;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering
{
    [CustomPropertyDrawer(typeof(ProbeVolumeDenoiserSettings))]
    class ProbeVolumeDenoiserSettingsDrawer : PropertyDrawer
    {
        static class Styles
        {
            public static readonly GUIContent enableDenoising = new GUIContent("Enable Denoising", "Runs the denoiser after the bake has completed.");
            public static readonly GUIContent denoisingModel = new GUIContent("Denoising Model", "");
            public static readonly GUIContent kernelFilter = new GUIContent("Kernel Filter", "");

            public static readonly GUIContent kernelSize = new GUIContent("Kernel Size", "");
            public static readonly GUIContent patchSize = new GUIContent("Patch Size", "");

            public static readonly string[] denoisingModelOptions = new string[] { "Static", "NLM" };
            public static readonly string[] kernelFilterOptions = new string[] { "Box", "Disc", "Epanechnikov" };

        }

        // PropertyDrawer are not made to use GUILayout, so it will try to reserve a rect before calling OnGUI
        // Tell we have a height of 0 so it doesn't interfere with our usage of GUILayout
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 0;

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            var enableDenoising = property.FindPropertyRelative("enableDenoising");
            EditorGUILayout.PropertyField(enableDenoising, Styles.enableDenoising);

            using (new EditorGUI.DisabledScope(!enableDenoising.boolValue))
            {
                var denoisingModel = property.FindPropertyRelative("denoisingModel");
                denoisingModel.intValue = EditorGUILayout.Popup(Styles.denoisingModel, denoisingModel.intValue, Styles.denoisingModelOptions);

                var kernelSize = property.FindPropertyRelative("kernelSize");
                kernelSize.intValue = EditorGUILayout.IntSlider(Styles.kernelSize, kernelSize.intValue, 0, 5);

                var patchSize = property.FindPropertyRelative("patchSize");
                patchSize.intValue = EditorGUILayout.IntSlider(Styles.patchSize, patchSize.intValue, 0, 2);
            }

            EditorGUI.EndProperty();
        }        
    }
}
