using UnityEditor;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering
{
    [CustomPropertyDrawer(typeof(ProbeVolumeBakingProcessSettings))]
    class ProbeVolumeBakingProcessSettingsDrawer : PropertyDrawer
    {
        static class Styles
        {
            public static readonly GUIContent enableDilation = new GUIContent("Enable Dilation", "Whether to enable dilation after the baking. Dilation will dilate valid probes data into invalid probes.");
            public static readonly GUIContent dilationDistance = new GUIContent("Dilation Distance", "The distance used to pick neighbouring probes to dilate into the invalid probe.");
            public static readonly GUIContent dilationValidity = new GUIContent("Dilation Validity Threshold", "The validity threshold used to identify invalid probes.");
            public static readonly GUIContent dilationIterationCount = new GUIContent("Dilation Iteration Count", "The number of times the dilation process takes place.");
            public static readonly GUIContent dilationSquaredDistanceWeighting = new GUIContent("Squared Distance Weighting", "Whether to weight neighbouring probe contribution using squared distance rather than linear distance.");
            public static readonly GUIContent useVirtualOffset = EditorGUIUtility.TrTextContent("Use Virtual Offset", "Push invalid probes out of geometry. Please note, this feature is currently a proof of concept, it is fairly slow and not optimal in quality.");
            public static readonly GUIContent virtualOffsetSearchMultiplier = EditorGUIUtility.TrTextContent("Search multiplier", "A multiplier to be applied on the distance between two probes to derive the search distance out of geometry.");
            public static readonly GUIContent virtualOffsetBiasOutGeometry = EditorGUIUtility.TrTextContent("Bias out geometry", "Determines how much a probe is pushed out of the geometry on top of the distance to closest hit.");
            public static readonly GUIContent virtualOffsetRayOriginBias = EditorGUIUtility.TrTextContent("Ray origin bias", "The distance with which to bias each ray direction away from the probe position.");
            public static readonly GUIContent virtualOffsetMaxHitsPerRay = EditorGUIUtility.TrTextContent("Max hits per ray", "Determines how many colliders intersecting each ray are included in calculations.");
            public static readonly GUIContent virtualOffsetCollisionMask = EditorGUIUtility.TrTextContent("Collision mask", "The collision layer mask to cast rays against.");

            public static readonly GUIContent advanced = EditorGUIUtility.TrTextContent("Advanced");

            public static readonly GUIContent dilationSettingsTitle = EditorGUIUtility.TrTextContent("Dilation Settings");
            public static readonly GUIContent virtualOffsetSettingsTitle = EditorGUIUtility.TrTextContent("Virtual Offset Settings");
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
                }
            }
        }
    }
}
