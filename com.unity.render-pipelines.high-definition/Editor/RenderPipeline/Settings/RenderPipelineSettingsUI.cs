using UnityEditor.AnimatedValues;
using UnityEditor.Rendering;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<SerializedRenderPipelineSettings>;
    
    static class RenderPipelineSettingsUI
    {
        enum Expandable
        {
            SupportedFeature = 1 << 0
        }

        readonly static ExpandedState<Expandable, RenderPipelineSettings> k_ExpandedState = new ExpandedState<Expandable, RenderPipelineSettings>(Expandable.SupportedFeature, "HDRP");

        static readonly GUIContent k_SupportedFeatureHeaderContent = EditorGUIUtility.TrTextContent("Render Pipeline Supported Features");

        static readonly GUIContent k_SupportShadowMaskContent = EditorGUIUtility.TrTextContent("Shadow Mask", "Enable memory (Extra Gbuffer in deferred) and shader variant for shadow mask.");
        static readonly GUIContent k_SupportSSRContent = EditorGUIUtility.TrTextContent("SSR", "Enable memory use by SSR effect.");
        static readonly GUIContent k_SupportSSAOContent = EditorGUIUtility.TrTextContent("SSAO", "Enable memory use by SSAO effect.");
        static readonly GUIContent k_SupportedSSSContent = EditorGUIUtility.TrTextContent("Subsurface Scattering");
        static readonly GUIContent k_SSSSampleCountContent = EditorGUIUtility.TrTextContent("High quality", "This allows for better SSS quality. Warning: high performance cost, do not enable on consoles.");
        static readonly GUIContent k_SupportVolumetricContent = EditorGUIUtility.TrTextContent("Volumetrics", "Enable memory and shader variant for volumetric.");
        static readonly GUIContent k_VolumetricResolutionContent = EditorGUIUtility.TrTextContent("High quality", "Increase the resolution of volumetric lighting buffers. Warning: high performance cost, do not enable on consoles.");
        static readonly GUIContent k_SupportLightLayerContent = EditorGUIUtility.TrTextContent("LightLayers", "Enable light layers. In deferred this imply an extra render target in memory and extra cost.");
        static readonly GUIContent k_SupportLitShaderModeContent = EditorGUIUtility.TrTextContent("Supported Lit Shader Mode", "Remove all the memory and shader variant of GBuffer of non used mode. The renderer cannot be switch to non selected path anymore.");
        static readonly GUIContent k_MSAASampleCountContent = EditorGUIUtility.TrTextContent("MSAA Quality", "Allow to select the level of MSAA.");
        static readonly GUIContent k_SupportDecalContent = EditorGUIUtility.TrTextContent("Decals", "Enable memory and variant for decals buffer and cluster decals.");
        static readonly GUIContent k_SupportMotionVectorContent = EditorGUIUtility.TrTextContent("Motion Vectors", "Motion vector are use for Motion Blur, TAA, temporal re-projection of various effect like SSR.");
        static readonly GUIContent k_SupportRuntimeDebugDisplayContent = EditorGUIUtility.TrTextContent("Runtime debug display", "Remove all debug display shader variant only in the player. Allow faster build.");
        static readonly GUIContent k_SupportDitheringCrossFadeContent = EditorGUIUtility.TrTextContent("Dithering cross fade", "Remove all dithering cross fade shader variant only in the player. Allow faster build.");
        static readonly GUIContent k_SupportDistortion = EditorGUIUtility.TrTextContent("Distortion", "Remove all distortion shader variants only in the player. Allow faster build.");
        static readonly GUIContent k_SupportTransparentBackface = EditorGUIUtility.TrTextContent("Transparent Backface", "Remove all Transparent backface shader variants only in the player. Allow faster build.");
        static readonly GUIContent k_SupportTransparentDepthPrepass = EditorGUIUtility.TrTextContent("Transparent Depth Prepass", "Remove all Transparent Depth Prepass shader variants only in the player. Allow faster build.");
        static readonly GUIContent k_SupportTransparentDepthPostpass = EditorGUIUtility.TrTextContent("Transparent Depth Postpass", "Remove all Transparent Depth Postpass shader variants only in the player. Allow faster build.");
        static readonly GUIContent k_SupportRaytracing = EditorGUIUtility.TrTextContent("Support Realtime Raytracing");
        static readonly GUIContent k_EditorRaytracingFilterLayerMask = EditorGUIUtility.TrTextContent("Raytracing Filter Layer Mask for SceneView and Preview");

        static RenderPipelineSettingsUI()
        {
            Inspector = CED.Group(
                    CED.Select(
                        (serialized, owner) => serialized.lightLoopSettings,
                        GlobalLightLoopSettingsUI.Inspector
                        ),
                    CED.Select(
                        (serialized, owner) => serialized.hdShadowInitParams,
                        HDShadowInitParametersUI.Inspector
                        ),
                    CED.Select(
                        (serialized, owner) => serialized.decalSettings,
                        GlobalDecalSettingsUI.Inspector
                        ),
                    CED.Select(
                        (serialized, owner) => serialized.postProcessSettings,
                        GlobalPostProcessSettingsUI.Inspector
                        ),
                    CED.Select(
                        (serialized, owner) => serialized.dynamicResolutionSettings,
                        GlobalDynamicResolutionSettingsUI.Inspector
                        )
                    );
        }
        
        public static readonly CED.IDrawer Inspector;

        public static readonly CED.IDrawer SupportedSettings = CED.FoldoutGroup(
            k_SupportedFeatureHeaderContent,
            Expandable.SupportedFeature,
            k_ExpandedState,
            Drawer_SectionPrimarySettings
            );
        
        static void Drawer_SectionPrimarySettings(SerializedRenderPipelineSettings d, Editor o)
        {
            EditorGUILayout.PropertyField(d.supportShadowMask, k_SupportShadowMaskContent);
            EditorGUILayout.PropertyField(d.supportSSR, k_SupportSSRContent);
            EditorGUILayout.PropertyField(d.supportSSAO, k_SupportSSAOContent);

            EditorGUILayout.PropertyField(d.supportSubsurfaceScattering, k_SupportedSSSContent);
            using (new EditorGUI.DisabledScope(!d.supportSubsurfaceScattering.boolValue))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(d.increaseSssSampleCount, k_SSSSampleCountContent);
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(d.supportVolumetrics, k_SupportVolumetricContent);
            using (new EditorGUI.DisabledScope(!d.supportVolumetrics.boolValue))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(d.increaseResolutionOfVolumetrics, k_VolumetricResolutionContent);
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(d.supportLightLayers, k_SupportLightLayerContent);
            
            EditorGUILayout.PropertyField(d.supportedLitShaderMode, k_SupportLitShaderModeContent);

            // MSAA is an option that is only available in full forward but Camera can be set in Full Forward only. Thus MSAA have no dependency currently
            //Note: do not use SerializedProperty.enumValueIndex here as this enum not start at 0 as it is used as flags.
            bool msaaAllowed = d.supportedLitShaderMode.intValue == (int)UnityEngine.Experimental.Rendering.HDPipeline.RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly || d.supportedLitShaderMode.intValue == (int)UnityEngine.Experimental.Rendering.HDPipeline.RenderPipelineSettings.SupportedLitShaderMode.Both;
            using (new EditorGUI.DisabledScope(!msaaAllowed))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(d.MSAASampleCount, k_MSAASampleCountContent);
                --EditorGUI.indentLevel;
            }
            
            EditorGUILayout.PropertyField(d.supportDecals, k_SupportDecalContent);
            EditorGUILayout.PropertyField(d.supportMotionVectors, k_SupportMotionVectorContent);
            EditorGUILayout.PropertyField(d.supportRuntimeDebugDisplay, k_SupportRuntimeDebugDisplayContent);
            EditorGUILayout.PropertyField(d.supportDitheringCrossFade, k_SupportDitheringCrossFadeContent);
            EditorGUILayout.PropertyField(d.supportDistortion, k_SupportDistortion);
            EditorGUILayout.PropertyField(d.supportTransparentBackface, k_SupportTransparentBackface);
            EditorGUILayout.PropertyField(d.supportTransparentDepthPrepass, k_SupportTransparentDepthPrepass);
            EditorGUILayout.PropertyField(d.supportTransparentDepthPostpass, k_SupportTransparentDepthPostpass);

            // Only display the support ray tracing feature if the platform supports it
#if REALTIME_RAYTRACING_SUPPORT
            if(UnityEngine.SystemInfo.supportsRayTracing)
            {
                EditorGUILayout.PropertyField(d.supportRayTracing, k_SupportRaytracing);
                using (new EditorGUI.DisabledScope(!d.supportRayTracing.boolValue))
                {
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(d.editorRaytracingFilterLayerMask, k_EditorRaytracingFilterLayerMask);
                    --EditorGUI.indentLevel;
                }
            }
            else
#endif
            {
                d.supportRayTracing.boolValue = false;
            }
        }
    }
}
