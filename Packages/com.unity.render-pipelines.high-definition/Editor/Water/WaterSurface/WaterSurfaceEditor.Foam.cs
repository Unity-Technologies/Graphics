using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static UnityEditor.EditorGUI;

namespace UnityEditor.Rendering.HighDefinition
{
    sealed partial class WaterSurfaceEditor : Editor
    {
        // Generic Foam
        SerializedProperty m_Foam;
        SerializedProperty m_FoamResolution;
        SerializedProperty m_FoamAreaSize;
        SerializedProperty m_FoamAreaOffset;
        SerializedProperty m_FoamPersistenceMultiplier;
        SerializedProperty m_FoamCurrentInfluence;
        SerializedProperty m_FoamSmoothness;
        SerializedProperty m_FoamTextureTiling;
        SerializedProperty m_FoamColor;

        // Simulation Foam
        SerializedProperty m_SimulationFoam;
        SerializedProperty m_SimulationFoamAmount;
        SerializedProperty m_SimulationFoamMask;
        SerializedProperty m_SimulationFoamMaskExtent;
        SerializedProperty m_SimulationFoamMaskOffset;
        SerializedProperty m_SimulationFoamWindCurve;

        void OnEnableFoam(PropertyFetcher<WaterSurface> o)
        {
            // Generic Foam
            m_Foam = o.Find(x => x.foam);
            m_FoamResolution = o.Find(x => x.foamResolution);
            m_FoamAreaSize = o.Find(x => x.foamAreaSize);
            m_FoamAreaOffset = o.Find(x => x.foamAreaOffset);
            m_FoamPersistenceMultiplier = o.Find(x => x.foamPersistenceMultiplier);
            m_FoamCurrentInfluence = o.Find(x => x.foamCurrentInfluence);
            m_FoamSmoothness = o.Find(x => x.foamSmoothness);
            m_FoamTextureTiling = o.Find(x => x.foamTextureTiling);
            m_FoamColor = o.Find(x => x.foamColor);

            // Simulation Foam
            m_SimulationFoam = o.Find(x => x.simulationFoam);
            m_SimulationFoamAmount = o.Find(x => x.simulationFoamAmount);
            m_SimulationFoamMask = o.Find(x => x.simulationFoamMask);
            m_SimulationFoamMaskExtent = o.Find(x => x.simulationFoamMaskExtent);
            m_SimulationFoamMaskOffset = o.Find(x => x.simulationFoamMaskOffset);
            m_SimulationFoamWindCurve = o.Find(x => x.simulationFoamWindCurve);
        }

        static public readonly GUIContent k_Foam = EditorGUIUtility.TrTextContent("Enable", "Specifies if the water surfaces supports foam rendering.");
        static public readonly GUIContent k_FoamResolution = EditorGUIUtility.TrTextContent("Resolution", "Specifies the resolution of the foam texture used to simulation the foam simulation.");
        static public readonly GUIContent k_FoamAreaSize = EditorGUIUtility.TrTextContent("Area Size", "Specifies the size of the foam area in meters.");
        static public readonly GUIContent k_FoamAreaOffset = EditorGUIUtility.TrTextContent("Area Offset", "Specifies the offset of the foam area in meters.");

        static internal void WaterSurfaceFoamSection(WaterSurfaceEditor serialized, Editor owner)
        {
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportWaterFoam ?? false)
            {
                EditorGUILayout.Space();
                HDEditorUtils.QualitySettingsHelpBox("The current HDRP Asset does not support foam for Water Surfaces.", MessageType.Error,
                    HDRenderPipelineUI.ExpandableGroup.Rendering, HDRenderPipelineUI.ExpandableRendering.Water, "m_RenderPipelineSettings.supportWaterFoam");
                return;
            }

            using (new BoldLabelScope())
                EditorGUILayout.PropertyField(serialized.m_Foam, k_Foam);

            if (!serialized.m_Foam.boolValue)
                return;

            using (new IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serialized.m_FoamResolution, k_FoamResolution);
                EditorGUILayout.PropertyField(serialized.m_FoamAreaSize, k_FoamAreaSize);
                SanitizeExtentsVector2(serialized.m_FoamAreaSize);
                EditorGUILayout.PropertyField(serialized.m_FoamAreaOffset, k_FoamAreaOffset);

                // Foam properties
                EditorGUILayout.PropertyField(serialized.m_FoamPersistenceMultiplier, k_FoamPersistenceMultiplier);
                EditorGUILayout.PropertyField(serialized.m_FoamCurrentInfluence, k_FoamCurrentInfluence);
                CoreEditorUtils.ColorFieldLinear(serialized.m_FoamColor, k_FoamColor);
                EditorGUILayout.PropertyField(serialized.m_FoamSmoothness, k_FoamSmoothness);
                EditorGUILayout.PropertyField(serialized.m_FoamTextureTiling, k_FoamTextureTiling);

                // We only support foam for oceans and rivers
                WaterSurfaceType surfaceType = (WaterSurfaceType)(serialized.m_SurfaceType.enumValueIndex);
                if (serialized.m_SurfaceType.hasMultipleDifferentValues || serialized.m_SimulationFoam.hasMultipleDifferentValues)
                {
                    EditorGUI.showMixedValue = true;
                    using (new BoldLabelScope())
                        using (new DisabledScope())
                            EditorGUILayout.PropertyField(serialized.m_SimulationFoam, k_SimulationFoam);
                    EditorGUI.showMixedValue = false;
                }
                else if (surfaceType == WaterSurfaceType.Pool)
                {
                    EditorGUILayout.LabelField(k_SimulationFoam, EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Simulation foam rendering is not supported for Pools.", MessageType.Info, wide: true);
                }
                else
                {
                    // Simulation foam
                    using (new BoldLabelScope())
                        EditorGUILayout.PropertyField(serialized.m_SimulationFoam, k_SimulationFoam);

                    if (serialized.m_SimulationFoam.boolValue)
                    {
                        using (new IndentLevelScope())
                        {
                            EditorGUILayout.PropertyField(serialized.m_SimulationFoamAmount);

                            // Foam masking
                            using (new BoldLabelScope())
                                MapWithExtent(serialized.m_SimulationFoamMask, k_SimulationFoamMask, serialized.m_SimulationFoamMaskExtent);

                            if (serialized.m_SimulationFoamMask.objectReferenceValue != null)
                            {
                                using (new IndentLevelScope())
                                {
                                    EditorGUILayout.PropertyField(serialized.m_SimulationFoamMaskExtent, k_FoamMaskExtent);
                                    EditorGUILayout.PropertyField(serialized.m_SimulationFoamMaskOffset, k_FoamMaskOffset);
                                }
                            }
                            EditorGUILayout.PropertyField(serialized.m_SimulationFoamWindCurve, k_WindFoamCurve);
                        }
                    }
                }
            }
        }
    }
}
