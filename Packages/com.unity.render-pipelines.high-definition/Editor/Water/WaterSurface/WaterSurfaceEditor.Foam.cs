using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.HighDefinition;
using static UnityEditor.EditorGUI;
using static UnityEditor.Rendering.HighDefinition.HDProbeUI;

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
        SerializedProperty m_FoamSmoothness;
        SerializedProperty m_FoamTextureTiling;

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
            m_FoamSmoothness = o.Find(x => x.foamSmoothness);
            m_FoamTextureTiling = o.Find(x => x.foamTextureTiling);

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

            WaterSurfaceType surfaceType = (WaterSurfaceType)(serialized.m_SurfaceType.enumValueIndex);
            using (new IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serialized.m_FoamResolution, k_FoamResolution);
                EditorGUILayout.PropertyField(serialized.m_FoamAreaSize, k_FoamAreaSize);
                SanitizeExtentsVector2(serialized.m_FoamAreaSize);
                EditorGUILayout.PropertyField(serialized.m_FoamAreaOffset, k_FoamAreaOffset);

                // Foam properties
                serialized.m_FoamPersistenceMultiplier.floatValue = EditorGUILayout.Slider(k_FoamPersistenceMultiplier, serialized.m_FoamPersistenceMultiplier.floatValue, 0.0f, 1.0f);
                serialized.m_FoamSmoothness.floatValue = EditorGUILayout.Slider(k_FoamSmoothness, serialized.m_FoamSmoothness.floatValue, 0.0f, 1.0f);
                EditorGUILayout.PropertyField(serialized.m_FoamTextureTiling, k_FoamTextureTiling);
                serialized.m_FoamTextureTiling.floatValue = Mathf.Max(serialized.m_FoamTextureTiling.floatValue, 0.01f);

                // We only support foam for oceans and rivers
                if (surfaceType == WaterSurfaceType.Pool)
                {
                    EditorGUILayout.LabelField("Foam", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Foam rendering is currently not supported for Pools.", MessageType.Info, wide: true);
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
                            serialized.m_SimulationFoamAmount.floatValue = EditorGUILayout.Slider(k_SimulationFoamAmount, serialized.m_SimulationFoamAmount.floatValue, 0.0f, 1.0f);

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
