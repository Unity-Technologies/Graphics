using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using static UnityEditor.EditorGUI;

namespace UnityEditor.Rendering.HighDefinition
{
    sealed partial class WaterSurfaceEditor : Editor
    {
        // Generic Foam
        SerializedProperty m_FoamPersistenceMultiplier;
        SerializedProperty m_FoamCurrentInfluence;
        SerializedProperty m_FoamSmoothness;
        SerializedProperty m_FoamTextureTiling;
        SerializedProperty m_FoamColor;

        // Simulation Foam
        SerializedProperty m_SimulationFoamAmount;
        SerializedProperty m_SimulationFoamMask;
        SerializedProperty m_SimulationFoamMaskExtent;
        SerializedProperty m_SimulationFoamMaskOffset;
        SerializedProperty m_SimulationFoamWindCurve;

        void OnEnableFoam(PropertyFetcher<WaterSurface> o)
        {
            // Generic Foam
            m_FoamPersistenceMultiplier = o.Find(x => x.foamPersistenceMultiplier);
            m_FoamCurrentInfluence = o.Find(x => x.foamCurrentInfluence);
            m_FoamSmoothness = o.Find(x => x.foamSmoothness);
            m_FoamTextureTiling = o.Find(x => x.foamTextureTiling);
            m_FoamColor = o.Find(x => x.foamColor);

            // Simulation Foam
            m_SimulationFoamAmount = o.Find(x => x.simulationFoamAmount);
            m_SimulationFoamMask = o.Find(x => x.simulationFoamMask);
            m_SimulationFoamMaskExtent = o.Find(x => x.simulationFoamMaskExtent);
            m_SimulationFoamMaskOffset = o.Find(x => x.simulationFoamMaskOffset);
            m_SimulationFoamWindCurve = o.Find(x => x.simulationFoamWindCurve);
        }

        static public readonly GUIContent k_FoamAreaSize = EditorGUIUtility.TrTextContent("Area Size", "Specifies the size of the foam area in meters.");
        static public readonly GUIContent k_FoamAreaOffset = EditorGUIUtility.TrTextContent("Area Offset", "Specifies the offset of the foam area in meters.");

        static internal void WaterSurfaceFoamSection(WaterSurfaceEditor serialized, Editor owner)
        {
            // Foam decals
            using (new DisabledScope(!serialized.m_Foam.boolValue))
                EditorGUILayout.PropertyField(serialized.m_FoamPersistenceMultiplier, k_FoamPersistenceMultiplier);

            // Generic foam
            EditorGUILayout.PropertyField(serialized.m_FoamCurrentInfluence, k_FoamCurrentInfluence);
            CoreEditorUtils.ColorFieldLinear(serialized.m_FoamColor, k_FoamColor);
            EditorGUILayout.PropertyField(serialized.m_FoamSmoothness, k_FoamSmoothness);
            EditorGUILayout.PropertyField(serialized.m_FoamTextureTiling, k_FoamTextureTiling);

            // We only support simulation foam for oceans and rivers
            WaterSurfaceType surfaceType = (WaterSurfaceType)(serialized.m_SurfaceType.enumValueIndex);
            if (!serialized.m_SurfaceType.hasMultipleDifferentValues && surfaceType != WaterSurfaceType.Pool)
            {
                EditorGUILayout.PropertyField(serialized.m_SimulationFoamAmount);

                if (serialized.m_SimulationFoamAmount.floatValue > 0.0f)
                {
                    using (new IndentLevelScope())
                    {
                        // Foam masking
                        if (!GraphicsSettings.GetRenderPipelineSettings<WaterSystemGlobalSettings>().waterDecalMaskAndCurrent)
                        {
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
                        }

                        EditorGUILayout.PropertyField(serialized.m_SimulationFoamWindCurve, k_WindFoamCurve);
                    }
                }
            }
        }
    }
}
