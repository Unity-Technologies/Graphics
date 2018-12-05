using UnityEngine.Events;
using UnityEditor.AnimatedValues;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<SerializedHDRenderPipelineAsset>;
    
    static class HDRenderPipelineUI
    {
        enum Expandable
        {
            CameraFrameSettings = 1 << 0,
            BakedOrCustomProbeFrameSettings = 1 << 1,
            RealtimeProbeFrameSettings = 1 << 2
        }

        readonly static ExpandedState<Expandable, HDRenderPipelineAsset> k_ExpandedState = new ExpandedState<Expandable, HDRenderPipelineAsset>(Expandable.CameraFrameSettings, "HDRP");

        static readonly GUIContent defaultFrameSettingsContent = CoreEditorUtils.GetContent("Default Frame Settings For");
        static readonly GUIContent renderPipelineResourcesContent = CoreEditorUtils.GetContent("Render Pipeline Resources|Set of resources that need to be loaded when creating stand alone");
        static readonly GUIContent renderPipelineEditorResourcesContent = CoreEditorUtils.GetContent("Render Pipeline Editor Resources|Set of resources that need to be loaded for working in editor");
        static readonly GUIContent diffusionProfileSettingsContent = CoreEditorUtils.GetContent("Diffusion Profile Settings");
        //static readonly GUIContent enableShaderVariantStrippingContent = CoreEditorUtils.GetContent("Enable Shader Variant Stripping");
        static readonly GUIContent enableSRPBatcher = CoreEditorUtils.GetContent("Enable SRP Batcher (experimental)");
        static readonly GUIContent enableVariantStrippingLog = CoreEditorUtils.GetContent("Enable Variant stripping logging");

        internal enum SelectedFrameSettings { Camera, BakedOrCustomReflection, RealtimeReflection };
        internal static SelectedFrameSettings selectedFrameSettings = SelectedFrameSettings.Camera;

        static HDRenderPipelineUI()
        {
            Inspector = CED.Group(
                CED.Group(Drawer_SectionPrimarySettings),
                CED.space,
                CED.Select(
                    (serialized, owner) => serialized.renderPipelineSettings,
                    RenderPipelineSettingsUI.SupportedSettings
                    ),
                FrameSettingsSection,
                CED.Select(
                    (serialized, owner) => serialized.renderPipelineSettings,
                    RenderPipelineSettingsUI.Inspector
                    )
            );
        }
        
        public static readonly CED.IDrawer Inspector;

        static readonly CED.IDrawer FrameSettingsSection = CED.Group(
            CED.Group(
                (serialized, owner) => EditorGUILayout.BeginVertical("box"),
                Drawer_TitleDefaultFrameSettings
                ),
            CED.Conditional(
                (serialized, owner) => k_ExpandedState[Expandable.CameraFrameSettings],
                CED.Select(
                    (serialized, owner) => serialized.defaultFrameSettings,
                    FrameSettingsUI.InspectorInnerbox(withOverride: false)
                    )
                ),
            CED.Conditional(
                (serialized, owner) => k_ExpandedState[Expandable.BakedOrCustomProbeFrameSettings],
                CED.Select(
                    (serialized, owner) => serialized.defaultBakedOrCustomReflectionFrameSettings,
                    FrameSettingsUI.InspectorInnerbox(withOverride: false)
                    )
                ),
            CED.Conditional(
                (serialized, owner) => k_ExpandedState[Expandable.RealtimeProbeFrameSettings],
                CED.Select(
                    (serialized, owner) => serialized.defaultRealtimeReflectionFrameSettings,
                    FrameSettingsUI.InspectorInnerbox(withOverride: false)
                    )
                ),
            CED.Group((serialized, owner) => EditorGUILayout.EndVertical())
            );

        static public void Init(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            k_ExpandedState.CollapseAll();
            switch (selectedFrameSettings)
            {
                case SelectedFrameSettings.Camera:
                    k_ExpandedState.SetExpandedAreas(Expandable.CameraFrameSettings, true);
                    break;
                case SelectedFrameSettings.BakedOrCustomReflection:
                    k_ExpandedState.SetExpandedAreas(Expandable.BakedOrCustomProbeFrameSettings, true);
                    break;
                case SelectedFrameSettings.RealtimeReflection:
                    k_ExpandedState.SetExpandedAreas(Expandable.RealtimeProbeFrameSettings, true);
                    break;
            }
        }

        static void Drawer_TitleDefaultFrameSettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(defaultFrameSettingsContent, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            selectedFrameSettings = (SelectedFrameSettings)EditorGUILayout.EnumPopup(selectedFrameSettings);
            if (EditorGUI.EndChangeCheck())
            {
                Init(serialized, owner);
            }
            GUILayout.EndHorizontal();
        }

        static void Drawer_SectionPrimarySettings(SerializedHDRenderPipelineAsset serialized, Editor owner)
        {
            EditorGUILayout.PropertyField(serialized.renderPipelineResources, renderPipelineResourcesContent);

            HDRenderPipelineAsset hdrpAsset = serialized.serializedObject.targetObject as HDRenderPipelineAsset;
            hdrpAsset.renderPipelineEditorResources = EditorGUILayout.ObjectField(renderPipelineEditorResourcesContent, hdrpAsset.renderPipelineEditorResources, typeof(HDRenderPipelineEditorResources), allowSceneObjects: false) as HDRenderPipelineEditorResources;

            EditorGUILayout.PropertyField(serialized.diffusionProfileSettings, diffusionProfileSettingsContent);
            // EditorGUILayout.PropertyField(serialized.allowShaderVariantStripping, enableShaderVariantStrippingContent);

            EditorGUILayout.PropertyField(serialized.enableSRPBatcher, enableSRPBatcher);
            EditorGUILayout.PropertyField(serialized.enableVariantStrippingLog, enableVariantStrippingLog);
        }
    }
}
