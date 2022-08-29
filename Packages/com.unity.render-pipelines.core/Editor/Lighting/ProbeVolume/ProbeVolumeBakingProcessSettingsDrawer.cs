using UnityEditor;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering
{
    [CustomPropertyDrawer(typeof(ProbeVolumeBakingProcessSettings))]
    class ProbeVolumeBakingProcessSettingsDrawer : PropertyDrawer
    {
        static class Styles
        {
            public static readonly GUIContent enableDilation = new GUIContent("Enable Dilation", "Replace invalid probe data with valid data from neighboring probes during baking.");
            public static readonly GUIContent dilationDistance = new GUIContent("Search Radius", "How far to search from invalid probes when looking for valid neighbors. Higher values include more distant probes which may be unwanted.");
            public static readonly GUIContent dilationValidity = new GUIContent("Validity Threshold", "The threshold of backfaces seen by probes before they are invalidated during baking. Higher values mean the probe is more likely to be marked invalid.");
            public static readonly GUIContent dilationIterationCount = new GUIContent("Dilation Iterations", "The number of times Unity repeats the Dilation calculation. This will cause the area of dilation to grow.");
            public static readonly GUIContent dilationSquaredDistanceWeighting = new GUIContent("Squared Distance Weighting", "During dilation, weight the contribution of neighbouring probes by squared distance, rather than linear distance.");
            public static readonly GUIContent useVirtualOffset = EditorGUIUtility.TrTextContent("Enable Virtual Offset", "Push invalid probes outside of geometry to prevent backface hits. Produces better visual results than Dilation, but increases baking times.");
            public static readonly GUIContent virtualOffsetSearchMultiplier = EditorGUIUtility.TrTextContent("Search Distance Multiplier", "Determines the length of the sampling ray Unity uses to search for valid probe positions. High values may result in unwanted results, such as pushing probes through neighboring geometry.");
            public static readonly GUIContent virtualOffsetBiasOutGeometry = EditorGUIUtility.TrTextContent("Geometry Bias", "Determines how far Unity pushes a probe out of geometry after a ray hit.");
            public static readonly GUIContent virtualOffsetRayOriginBias = EditorGUIUtility.TrTextContent("Ray Origin Bias", "Distance from the probe position used to determine the origin of the sampling ray.");
            public static readonly GUIContent virtualOffsetMaxHitsPerRay = EditorGUIUtility.TrTextContent("Max Ray Hits", "How many collisions to allow per ray before determining the Virtual Offset probe position.");
            public static readonly GUIContent virtualOffsetCollisionMask = EditorGUIUtility.TrTextContent("Layer Mask", "Layers to include in collision calculations for Virtual Offset.");

            public static readonly GUIContent advanced = EditorGUIUtility.TrTextContent("Advanced");

            public static readonly GUIContent dilationSettingsTitle = EditorGUIUtility.TrTextContent("Probe Dilation Settings");
            public static readonly GUIContent virtualOffsetSettingsTitle = EditorGUIUtility.TrTextContent("Virtual Offset Settings");

            public static GUIStyle voButtonStyle = new GUIStyle(EditorStyles.miniButton);
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

            property.serializedObject.Update();

            EditorGUI.FloatField(position, 100f);

            if (ProbeVolumeBakingWindow.Foldout(Styles.dilationSettingsTitle, ProbeVolumeBakingWindow.Expandable.Dilation))
                DrawDilationSettings(dilationSettings);
            EditorGUILayout.Space();
            if (ProbeVolumeBakingWindow.Foldout(Styles.virtualOffsetSettingsTitle, ProbeVolumeBakingWindow.Expandable.VirtualOffset))
                DrawVirtualOffsetSettings(virtualOffsetSettings);
            EditorGUI.EndProperty();

            property.serializedObject.ApplyModifiedProperties();
        }

        void DrawDilationSettings(SerializedProperty dilationSettings)
        {
            var enableDilation = dilationSettings.FindPropertyRelative("enableDilation");
            var maxDilationSampleDistance = dilationSettings.FindPropertyRelative("dilationDistance");
            var dilationValidityThreshold = dilationSettings.FindPropertyRelative("dilationValidityThreshold");
            float dilationValidityThresholdInverted = 1f - dilationValidityThreshold.floatValue;
            var dilationIterations = dilationSettings.FindPropertyRelative("dilationIterations");
            var dilationInvSquaredWeight = dilationSettings.FindPropertyRelative("squaredDistWeighting");

            EditorGUI.indentLevel++;
            enableDilation.boolValue = EditorGUILayout.Toggle(Styles.enableDilation, enableDilation.boolValue);
            EditorGUI.BeginDisabledGroup(!enableDilation.boolValue);
            maxDilationSampleDistance.floatValue = Mathf.Max(EditorGUILayout.FloatField(Styles.dilationDistance, maxDilationSampleDistance.floatValue), 0);
            dilationValidityThresholdInverted = EditorGUILayout.Slider(Styles.dilationValidity, dilationValidityThresholdInverted, 0f, 0.95f);
            dilationValidityThreshold.floatValue = Mathf.Max(0.05f, 1.0f - dilationValidityThresholdInverted);
            dilationIterations.intValue = EditorGUILayout.IntSlider(Styles.dilationIterationCount, dilationIterations.intValue, 1, 5);
            dilationInvSquaredWeight.boolValue = EditorGUILayout.Toggle(Styles.dilationSquaredDistanceWeighting, dilationInvSquaredWeight.boolValue);
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;

            if (Unsupported.IsDeveloperMode())
            {
                if (GUILayout.Button(EditorGUIUtility.TrTextContent("Refresh dilation"), EditorStyles.miniButton))
                {
                    ProbeGIBaking.RevertDilation();
                    ProbeGIBaking.PerformDilation();
                }
            }
        }

        void DrawVirtualOffsetSettings(SerializedProperty virtualOffsetSettings)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                var enableVirtualOffset = virtualOffsetSettings.FindPropertyRelative("useVirtualOffset");
                EditorGUILayout.PropertyField(enableVirtualOffset, Styles.useVirtualOffset);

                using (new EditorGUI.DisabledScope(!enableVirtualOffset.boolValue))
                {
                    EditorGUILayout.LabelField(Styles.advanced);
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            var virtualOffsetGeometrySearchMultiplier = virtualOffsetSettings.FindPropertyRelative("searchMultiplier");
                            var virtualOffsetBiasOutOfGeometry = virtualOffsetSettings.FindPropertyRelative("outOfGeoOffset");
                            var virtualOffsetRayOriginBias = virtualOffsetSettings.FindPropertyRelative("rayOriginBias");
                            var virtualOffsetMaxHitsPerRay = virtualOffsetSettings.FindPropertyRelative("maxHitsPerRay");
                            var virtualOffsetCollisionMask = virtualOffsetSettings.FindPropertyRelative("collisionMask");

                            EditorGUILayout.PropertyField(virtualOffsetGeometrySearchMultiplier, Styles.virtualOffsetSearchMultiplier);
                            EditorGUILayout.PropertyField(virtualOffsetBiasOutOfGeometry, Styles.virtualOffsetBiasOutGeometry);
                            EditorGUILayout.PropertyField(virtualOffsetRayOriginBias, Styles.virtualOffsetRayOriginBias);
                            EditorGUILayout.PropertyField(virtualOffsetMaxHitsPerRay, Styles.virtualOffsetMaxHitsPerRay);
                            EditorGUILayout.PropertyField(virtualOffsetCollisionMask, Styles.virtualOffsetCollisionMask);
                        }
                    }

                    Styles.voButtonStyle.margin.left = 16 * (EditorGUI.indentLevel + 1);
                    if (GUILayout.Button(EditorGUIUtility.TrTextContent("Regenerate virtual offset for Debug", "Re-run the virtual offset simulation; it will be applied only for debug visualization sake and not affect baked data."), Styles.voButtonStyle))
                    {
                        ProbeGIBaking.RecomputeVOForDebugOnly();
                    }
                }
            }
        }
    }
}
