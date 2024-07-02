using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    sealed partial class WaterSurfaceEditor : Editor
    {
        SerializedProperty m_DecalRegionSize;
        SerializedProperty m_DecalRegionAnchor;

        SerializedProperty m_SimulationMask;
        SerializedProperty m_SimulationMaskResolution;
        SerializedProperty m_SupportSimulationFoamMask;

        SerializedProperty m_Foam;
        SerializedProperty m_FoamResolution;

        SerializedProperty m_Deformation;
        SerializedProperty m_DeformationRes;

        SerializedProperty m_LargeCurrent;
        SerializedProperty m_LargeCurrentRes;
        SerializedProperty m_RipplesCurrent;
        SerializedProperty m_RipplesCurrentRes;

        void OnEnableDecals(PropertyFetcher<WaterSurface> o)
        {
            m_DecalRegionSize = o.Find(x => x.decalRegionSize);
            m_DecalRegionAnchor = o.Find(x => x.decalRegionAnchor);

            m_SimulationMask = o.Find(x => x.simulationMask);
            m_SimulationMaskResolution = o.Find(x => x.maskRes);
            m_SupportSimulationFoamMask = o.Find(x => x.supportSimulationFoamMask);

            m_Foam = o.Find(x => x.foam);
            m_FoamResolution = o.Find(x => x.foamResolution);

            m_Deformation = o.Find(x => x.deformation);
            m_DeformationRes = o.Find(x => x.deformationRes);

            m_LargeCurrent = o.Find(x => x.supportLargeCurrent);
            m_LargeCurrentRes = o.Find(x => x.largeCurrentRes);
            m_RipplesCurrent = o.Find(x => x.supportRipplesCurrent);
            m_RipplesCurrentRes = o.Find(x => x.ripplesCurrentRes);
        }

        static public readonly GUIContent k_RegionSize = EditorGUIUtility.TrTextContent("Region Size", "Sets the extent of the decal region in meters.");
        static public readonly GUIContent k_RegionAnchor = EditorGUIUtility.TrTextContent("Region Anchor", "Sets the center of the decal region. If nothing is set, the region will follow the main camera transform.");

        static public readonly GUIContent k_DecalResolution = EditorGUIUtility.TrTextContent("Resolution", "Sets the resolution of the texture covering the region.");

        static public readonly GUIContent k_DeformationDecals = EditorGUIUtility.TrTextContent("Deformation", "Specifies if decals deforming the surface of the water are supported.");
        static public readonly GUIContent k_FoamDecals = EditorGUIUtility.TrTextContent("Foam", "Specifies if decals injecting foam are supported.");
        static public readonly GUIContent k_MaskDecals = EditorGUIUtility.TrTextContent("Simulation Mask", "Specifies if decals masking the simulation are supported.");
        static public readonly GUIContent k_FoamMaskDecals = EditorGUIUtility.TrTextContent("Simulation Foam Mask", "Specifies if decals masking the simulation foam are supported.");
        static public readonly GUIContent k_LargeCurrentDecals = EditorGUIUtility.TrTextContent("Large Current", "Specifies if decals affecting the swell current on oceans and the agitation current on rivers are supported.");
        static public readonly GUIContent k_RipplesCurrentDecals = EditorGUIUtility.TrTextContent("Ripples Current", "Specifies if decals affecting the ripples current are supported.");

        static void DecalSupportOption(SerializedProperty enable, GUIContent enableTitle, SerializedProperty resolution)
        {
            EditorGUILayout.PropertyField(enable, enableTitle);
            if (resolution != null && enable.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                    EditorGUILayout.PropertyField(resolution, k_DecalResolution);
            }
        }

        static internal void WaterSurfaceDecalSection(WaterSurfaceEditor serialized, Editor _)
        {
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportWaterDecals ?? false)
            {
                EditorGUILayout.Space();
                HDEditorUtils.QualitySettingsHelpBox("The current HDRP Asset does not support water decals.", MessageType.Error,
                    HDRenderPipelineUI.ExpandableGroup.Rendering, HDRenderPipelineUI.ExpandableRendering.Water, "m_RenderPipelineSettings.supportWaterDecals");
                return;
            }

            EditorGUILayout.PropertyField(serialized.m_DecalRegionSize, k_RegionSize);
            EditorGUILayout.PropertyField(serialized.m_DecalRegionAnchor, k_RegionAnchor);
            SanitizeExtentsVector2(serialized.m_DecalRegionSize);
            EditorGUILayout.Space();

            DecalSupportOption(serialized.m_Deformation, k_DeformationDecals, serialized.m_DeformationRes);
            DecalSupportOption(serialized.m_Foam, k_FoamDecals, serialized.m_FoamResolution);

            if (GraphicsSettings.GetRenderPipelineSettings<WaterSystemGlobalSettings>().waterDecalMaskAndCurrent)
            {
                WaterSurfaceType surfaceType = (WaterSurfaceType)(serialized.m_SurfaceType.enumValueIndex);

                DecalSupportOption(serialized.m_SimulationMask, k_MaskDecals, serialized.m_SimulationMaskResolution);
                if (serialized.m_SimulationMask.boolValue && surfaceType != WaterSurfaceType.Pool)
                {
                    using (new EditorGUI.IndentLevelScope())
                        DecalSupportOption(serialized.m_SupportSimulationFoamMask, k_FoamMaskDecals, null);
                }

                if (surfaceType != WaterSurfaceType.Pool)
                    DecalSupportOption(serialized.m_LargeCurrent, k_LargeCurrentDecals, serialized.m_LargeCurrentRes);
                if (surfaceType == WaterSurfaceType.Pool || (serialized.m_Ripples.boolValue && HasCustomRipplesCurrent(serialized)))
                    DecalSupportOption(serialized.m_RipplesCurrent, k_RipplesCurrentDecals, serialized.m_RipplesCurrentRes);
            }
            else
            {
                EditorGUILayout.Space();
                HDEditorUtils.GlobalSettingsHelpBox<WaterSystemGlobalSettings>("Water decals affecting mask and current are not enabled in the HDRP Global Settings.", MessageType.Info);
            }
        }
    }
}
