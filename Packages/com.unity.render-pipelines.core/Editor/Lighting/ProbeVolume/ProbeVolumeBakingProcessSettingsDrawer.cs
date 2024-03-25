using UnityEditor;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering
{
    [CustomPropertyDrawer(typeof(ProbeVolumeBakingProcessSettings))]
    class ProbeVolumeBakingProcessSettingsDrawer : PropertyDrawer
    {
        static class Styles
        {
            public static readonly GUIContent enableDilation = new GUIContent("Dilation", "Replace invalid probe data with valid data from neighboring probes during baking.");
            public static readonly GUIContent dilationDistance = new GUIContent("Search Radius", "How far to search from invalid probes when looking for valid neighbors. Higher values include more distant probes which may be unwanted.");
            public static readonly GUIContent dilationValidity = new GUIContent("Validity Threshold", "The threshold of backfaces seen by probes before they are invalidated during baking. Higher values mean the probe is more likely to be marked invalid.");
            public static readonly GUIContent dilationIterationCount = new GUIContent("Dilation Iterations", "The number of times Unity repeats the Dilation calculation. This will cause the area of dilation to grow.");
            public static readonly GUIContent dilationSquaredDistanceWeighting = new GUIContent("Squared Distance Weighting", "During dilation, weight the contribution of neighbouring probes by squared distance, rather than linear distance.");
            public static readonly GUIContent useVirtualOffset = EditorGUIUtility.TrTextContent("Virtual Offset", "Push invalid probes outside of geometry to prevent backface hits. Produces better visual results than Dilation, but increases baking times.");
            public static readonly GUIContent virtualOffsetThreshold = EditorGUIUtility.TrTextContent("Validity Threshold", "The threshold of backfaces or sky seen by probes before virtual offset is applied. Higher values mean probes are more likely to be offseted.");
            public static readonly GUIContent virtualOffsetSearchMultiplier = EditorGUIUtility.TrTextContent("Search Distance Multiplier", "Determines the length of the sampling ray Unity uses to search for valid probe positions.");
            public static readonly GUIContent virtualOffsetBiasOutGeometry = EditorGUIUtility.TrTextContent("Geometry Bias", "Determines how far Unity pushes a probe out of geometry after a ray hit.");
            public static readonly GUIContent virtualOffsetRayOriginBias = EditorGUIUtility.TrTextContent("Ray Origin Bias", "Distance from the probe position used to determine the origin of the sampling ray.");
            public static readonly GUIContent virtualOffsetCollisionMask = EditorGUIUtility.TrTextContent("Layer Mask", "Layers to include in collision calculations for Virtual Offset.");

            public static readonly GUIContent dilationSettingsTitle = EditorGUIUtility.TrTextContent("Probe Dilation Settings");
            public static readonly GUIContent virtualOffsetSettingsTitle = EditorGUIUtility.TrTextContent("Virtual Offset Settings");
        }

        static internal bool IndentedButton(GUIContent content)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.Space(15 * EditorGUI.indentLevel, false);
            bool value = GUILayout.Button(content, EditorStyles.miniButton);
            GUILayout.EndHorizontal();
            return value;
        }

        // PropertyDrawer are not made to use GUILayout, so it will try to reserve a rect before calling OnGUI
        // Tell we have a height of 0 so it doesn't interfere with our usage of GUILayout
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 0;

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var dilationSettings = property.FindPropertyRelative("dilationSettings");
            var virtualOffsetSettings = property.FindPropertyRelative("virtualOffsetSettings");

            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            DrawDilationSettings(dilationSettings);
            DrawVirtualOffsetSettings(virtualOffsetSettings);

            EditorGUI.EndProperty();
        }

        void DrawDilationSettings(SerializedProperty dilationSettings)
        {
            var enableDilation = dilationSettings.FindPropertyRelative("enableDilation");
            EditorGUILayout.PropertyField(enableDilation, Styles.enableDilation);
            if (!enableDilation.boolValue)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                var maxDilationSampleDistance = dilationSettings.FindPropertyRelative("dilationDistance");
                var dilationValidityThreshold = dilationSettings.FindPropertyRelative("dilationValidityThreshold");
                float dilationValidityThresholdInverted = 1f - dilationValidityThreshold.floatValue;
                var dilationIterations = dilationSettings.FindPropertyRelative("dilationIterations");
                var dilationInvSquaredWeight = dilationSettings.FindPropertyRelative("squaredDistWeighting");

                maxDilationSampleDistance.floatValue = Mathf.Max(EditorGUILayout.FloatField(Styles.dilationDistance, maxDilationSampleDistance.floatValue), 0);
                dilationValidityThresholdInverted = EditorGUILayout.Slider(Styles.dilationValidity, dilationValidityThresholdInverted, 0f, 0.95f);
                dilationValidityThreshold.floatValue = Mathf.Max(0.05f, 1.0f - dilationValidityThresholdInverted);
                dilationIterations.intValue = EditorGUILayout.IntSlider(Styles.dilationIterationCount, dilationIterations.intValue, 1, 5);
                dilationInvSquaredWeight.boolValue = EditorGUILayout.Toggle(Styles.dilationSquaredDistanceWeighting, dilationInvSquaredWeight.boolValue);

                if (Unsupported.IsDeveloperMode())
                {
                    if (IndentedButton(EditorGUIUtility.TrTextContent("Refresh Dilation")))
                    {
                        AdaptiveProbeVolumes.RevertDilation();
                        AdaptiveProbeVolumes.PerformDilation();
                    }
                }
            }
        }

        void DrawVirtualOffsetSettings(SerializedProperty virtualOffsetSettings)
        {
            var enableVirtualOffset = virtualOffsetSettings.FindPropertyRelative("useVirtualOffset");
            EditorGUILayout.PropertyField(enableVirtualOffset, Styles.useVirtualOffset);
            if (!enableVirtualOffset.boolValue)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                var validity = virtualOffsetSettings.FindPropertyRelative("validityThreshold");
                var virtualOffsetGeometrySearchMultiplier = virtualOffsetSettings.FindPropertyRelative("searchMultiplier");
                var virtualOffsetBiasOutOfGeometry = virtualOffsetSettings.FindPropertyRelative("outOfGeoOffset");
                var virtualOffsetRayOriginBias = virtualOffsetSettings.FindPropertyRelative("rayOriginBias");
                var virtualOffsetCollisionMask = virtualOffsetSettings.FindPropertyRelative("collisionMask");

                validity.floatValue = 1.0f - EditorGUILayout.Slider(Styles.virtualOffsetThreshold, 1.0f - validity.floatValue, 0f, 0.95f);
                EditorGUILayout.PropertyField(virtualOffsetGeometrySearchMultiplier, Styles.virtualOffsetSearchMultiplier);
                EditorGUILayout.PropertyField(virtualOffsetBiasOutOfGeometry, Styles.virtualOffsetBiasOutGeometry);
                EditorGUILayout.PropertyField(virtualOffsetRayOriginBias, Styles.virtualOffsetRayOriginBias);
                EditorGUILayout.PropertyField(virtualOffsetCollisionMask, Styles.virtualOffsetCollisionMask);

                if (IndentedButton(EditorGUIUtility.TrTextContent("Refresh Virtual Offset Debug", "Re-run the virtual offset simulation; it will be applied only for debug visualization sake and not affect baked data.")))
                {
                    AdaptiveProbeVolumes.RecomputeVOForDebugOnly();
                }
            }
        }
    }
}
