using UnityEditor;
using UnityEditor.Rendering;
using static UnityEngine.Rendering.ProbeVolumeLightingTab;

namespace UnityEngine.Rendering
{
    [CustomPropertyDrawer(typeof(ProbeVolumeDenoiserSettings))]
    class ProbeVolumeDenoiserSettingsDrawer : PropertyDrawer
    {
        static class Styles
        {
            public static readonly GUIContent enableDenoising = new GUIContent("Enable Denoising", "Runs the denoiser after the bake has completed.");
            public static readonly GUIContent kernelFilterType = new GUIContent("Kernel Filter", "");
            public static readonly GUIContent kernelSize = new GUIContent("Kernel Size", "");
            public static readonly GUIContent nlmM = new GUIContent("NLM Patch Size", "");
            public static readonly GUIContent nlmAlpha = new GUIContent("Alpha", "");
            public static readonly GUIContent nlmK = new GUIContent("K", "");

            public static readonly GUIContent aiModelType = new GUIContent("Emphasis", "");

            public static readonly GUIContent fineTuningTitle = new GUIContent("Fine Tuning", "");
            public static readonly GUIContent samplerBias = new GUIContent("Sampler Bias", "");

            public static readonly GUIContent debugMode = new GUIContent("Debug Mode", "");
            public static readonly GUIContent isolateCell = new GUIContent("Isolate cell", "");
            public static readonly GUIContent isolateCellIdx = new GUIContent("Cell index", "");
            public static readonly GUIContent showInvalidProbes = new GUIContent("Show Invalid Probes", "");

            public static readonly string[] kernelFilterTypeOptions = new string[] { "Box", "Epanechnikov", "NLM", "AI" };
            public static readonly string[] aiModelTypeOptions = new string[] { "Noise removal", "Detail preservation" };
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

            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUI.DisabledScope(!enableDenoising.boolValue))
                {
                    var kernelFilterType = property.FindPropertyRelative("kernelFilterType");
                    kernelFilterType.intValue = EditorGUILayout.Popup(Styles.kernelFilterType, kernelFilterType.intValue, Styles.kernelFilterTypeOptions);

                    if (kernelFilterType.intValue != (int)ProbeVolumeDenoiserSettings.KernelFilterType.kAI)
                    {
                        var kernelSize = property.FindPropertyRelative("kernelSize");
                        kernelSize.intValue = EditorGUILayout.IntSlider(Styles.kernelSize, kernelSize.intValue, 0, 5);

                        if (kernelFilterType.intValue == (int)ProbeVolumeDenoiserSettings.KernelFilterType.kNLM)
                        {
                            var nlmM = property.FindPropertyRelative("nlmM");
                            nlmM.intValue = EditorGUILayout.IntSlider(Styles.nlmM, nlmM.intValue, 0, 2);

                            var nlmAlpha = property.FindPropertyRelative("nlmAlpha");
                            nlmAlpha.floatValue = EditorGUILayout.Slider(Styles.nlmAlpha, nlmAlpha.floatValue, 0.0f, 3.0f);

                            var nlmK = property.FindPropertyRelative("nlmK");
                            nlmK.floatValue = EditorGUILayout.Slider(Styles.nlmK, nlmK.floatValue, 0.0f, 3.0f);
                        }
                    }
                    else
                    {
                        var aiModelType = property.FindPropertyRelative("aiModelType");
                        aiModelType.intValue = EditorGUILayout.Popup(Styles.aiModelType, aiModelType.intValue, Styles.aiModelTypeOptions);
                    }
                }                
            }

            EditorGUILayout.LabelField(Styles.fineTuningTitle.text);
            using (new EditorGUI.IndentLevelScope())
            {
                var samplerBias = property.FindPropertyRelative("samplerBias");
                samplerBias.floatValue = Mathf.Clamp(EditorGUILayout.FloatField(Styles.samplerBias, samplerBias.floatValue), -1.0f, 1.0f);
            }

            var debugMode = property.FindPropertyRelative("debugMode");
            EditorGUILayout.PropertyField(debugMode, Styles.debugMode);

            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUI.DisabledScope(!debugMode.boolValue))
                {
                    var isolateCell = property.FindPropertyRelative("isolateCell");
                    EditorGUILayout.PropertyField(isolateCell, Styles.isolateCell);

                    using (new EditorGUI.DisabledScope(!isolateCell.boolValue))
                    {
                        var isolateCellIdx = property.FindPropertyRelative("isolateCellIdx");
                        isolateCellIdx.intValue = EditorGUILayout.IntField(Styles.isolateCellIdx, isolateCellIdx.intValue);
                    }

                    var showInvalidProbes = property.FindPropertyRelative("showInvalidProbes");
                    EditorGUILayout.PropertyField(showInvalidProbes, Styles.showInvalidProbes);
                }
            }

            EditorGUI.EndProperty();
        }        
    }
}
