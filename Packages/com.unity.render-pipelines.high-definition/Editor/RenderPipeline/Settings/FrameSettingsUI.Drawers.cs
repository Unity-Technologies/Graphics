using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedFrameSettings>;

    // Mirrors MaterialQuality enum and adds `FromQualitySettings`
    enum MaterialQualityMode
    {
        Low,
        Medium,
        High,
        FromQualitySettings,
    }

    static class MaterialQualityModeExtensions
    {
        public static MaterialQuality Into(this MaterialQualityMode quality)
        {
            switch (quality)
            {
                case MaterialQualityMode.High: return MaterialQuality.High;
                case MaterialQualityMode.Medium: return MaterialQuality.Medium;
                case MaterialQualityMode.Low: return MaterialQuality.Low;
                case MaterialQualityMode.FromQualitySettings: return (MaterialQuality)0;
                default: throw new ArgumentOutOfRangeException(nameof(quality));
            }
        }

        public static MaterialQualityMode Into(this MaterialQuality quality)
        {
            if (quality == (MaterialQuality)0)
                return MaterialQualityMode.FromQualitySettings;
            switch (quality)
            {
                case MaterialQuality.High: return MaterialQualityMode.High;
                case MaterialQuality.Medium: return MaterialQualityMode.Medium;
                case MaterialQuality.Low: return MaterialQualityMode.Low;
                default: throw new ArgumentOutOfRangeException(nameof(quality));
            }
        }
    }

    interface IDefaultFrameSettingsType
    {
        FrameSettingsRenderType GetFrameSettingsType();
    }

    partial class FrameSettingsUI
    {
        enum Expandable
        {
            RenderingPasses = 1 << 0,
            RenderingSettings = 1 << 1,
            LightingSettings = 1 << 2,
            AsynComputeSettings = 1 << 3,
            LightLoop = 1 << 4,
        }

        readonly static ExpandedState<Expandable, FrameSettings> k_ExpandedState = new ExpandedState<Expandable, FrameSettings>(~(-1), "HDRP");

        static Rect lastBoxRect;
        internal static CED.IDrawer Inspector(bool withOverride = true) => CED.Group(
            CED.Group((serialized, owner) =>
            {
                lastBoxRect = EditorGUILayout.BeginVertical("box");

                // Add dedicated scope here and on each FrameSettings field to have the contextual menu on everything
                Rect rect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
                using (new SerializedFrameSettings.TitleDrawingScope(rect, FrameSettingsUI.frameSettingsHeaderContent, serialized))
                {
                    EditorGUI.LabelField(rect, FrameSettingsUI.frameSettingsHeaderContent, EditorStyles.boldLabel);
                }
            }),
            InspectorInnerbox(withOverride),
            CED.Group((serialized, owner) =>
            {
                EditorGUILayout.EndVertical();
                using (new SerializedFrameSettings.TitleDrawingScope(lastBoxRect, FrameSettingsUI.frameSettingsHeaderContent, serialized))
                {
                    //Nothing to draw.
                    //We just want to have a big blue bar at left that match the whole framesetting box.
                    //This is because framesettings will be considered as one bg block from prefab point
                    //of view as there is no way to separate it bit per bit in serialization and Prefab
                    //override API rely on SerializedProperty.
                }
            })
        );

        //separated to add enum popup on default frame settings
        internal static CED.IDrawer InspectorInnerbox(bool withOverride = true, bool isBoxed = true) => CED.Group(
            CED.FoldoutGroup(renderingSettingsHeaderContent, Expandable.RenderingPasses, k_ExpandedState, isBoxed ? FoldoutOption.Indent | FoldoutOption.Boxed : FoldoutOption.Indent,
                CED.Group(206, (serialized, owner) => Drawer_SectionRenderingSettings(serialized, owner, withOverride))
                ),
            CED.FoldoutGroup(lightSettingsHeaderContent, Expandable.LightingSettings, k_ExpandedState, isBoxed ? FoldoutOption.Indent | FoldoutOption.Boxed : FoldoutOption.Indent,
                CED.Group(206, (serialized, owner) => Drawer_SectionLightingSettings(serialized, owner, withOverride))
                ),
            CED.FoldoutGroup(asyncComputeSettingsHeaderContent, Expandable.AsynComputeSettings, k_ExpandedState, isBoxed ? FoldoutOption.Indent | FoldoutOption.Boxed : FoldoutOption.Indent,
                CED.Group(206, (serialized, owner) => Drawer_SectionAsyncComputeSettings(serialized, owner, withOverride))
                ),
            CED.FoldoutGroup(lightLoopSettingsHeaderContent, Expandable.LightLoop, k_ExpandedState, isBoxed ? FoldoutOption.Indent | FoldoutOption.Boxed : FoldoutOption.Indent,
                CED.Group(206, (serialized, owner) => Drawer_SectionLightLoopSettings(serialized, owner, withOverride))
                )
			// Message below isn't required as the async compute still works for rasterize effect, it is only when effect are setup for raytracing that it doesn't work
			/*			
            CED.Group((serialized, owner) =>
            {
                var hdrpAsset = GetHDRPAssetFor(owner);
                if (hdrpAsset != null)
                {
                    RenderPipelineSettings hdrpSettings = hdrpAsset.currentPlatformRenderPipelineSettings;
                    if (hdrpSettings.supportRayTracing)
                    {
                        bool rtEffectUseAsync = (serialized.IsEnabled(FrameSettingsField.SSRAsync) ?? false) || (serialized.IsEnabled(FrameSettingsField.SSAOAsync) ?? false)
                        //|| (serialized.IsEnabled(FrameSettingsField.ContactShadowsAsync) ?? false) // Contact shadow async is not visible in the UI for now and defaults to true.
                        ;
                        if (rtEffectUseAsync)
                            EditorGUILayout.HelpBox("Asynchronous execution of Raytracing effects is not supported. Asynchronous Execution will be forced to false for them", MessageType.Warning);
                    }
                }
            }*/
			);

        static HDRenderPipelineAsset GetHDRPAssetFor(Editor owner)
        {
            HDRenderPipelineAsset hdrpAsset;
            if (owner is HDRenderPipelineEditor)
            {
                // When drawing the inspector of a selected HDRPAsset in Project windows, access HDRP by owner drawing itself
                hdrpAsset = (owner as HDRenderPipelineEditor).target as HDRenderPipelineAsset;
            }
            else if (owner is HDRenderPipelineGlobalSettingsEditor || owner == null)
            {
                // When drawing the inspector of a selected HDRPAsset in Project windows, access HDRP by owner drawing itself
                hdrpAsset = null;
            }
            else
            {
                // Else rely on GraphicsSettings are you should be in hdrp and owner could be probe or camera.
                hdrpAsset = HDRenderPipeline.currentAsset;
            }
            return hdrpAsset;
        }

        static FrameSettings? GetDefaultFrameSettingsFor(Editor owner)
        {
            if (owner is IHDProbeEditor)
            {
                var getType = owner as IDefaultFrameSettingsType;
                return HDRenderPipelineGlobalSettings.instance.GetDefaultFrameSettings(getType.GetFrameSettingsType());
            }
            else if (owner is HDCameraEditor)
            {
                return HDRenderPipelineGlobalSettings.instance.GetDefaultFrameSettings(FrameSettingsRenderType.Camera);
            }
            return null;
        }

        static internal void Drawer_SectionRenderingSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            bool isGUIenabled = GUI.enabled;

            FrameSettings? defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            var area = OverridableFrameSettingsArea.GetGroupContent(0, defaultFrameSettings, serialized);

            area.AmmendInfo(FrameSettingsField.DepthPrepassWithDeferredRendering, ignoreDependencies: true);
            area.AmmendInfo(FrameSettingsField.ClearGBuffers, ignoreDependencies: true);

            area.AmmendInfo(FrameSettingsField.MSAAMode, ignoreDependencies: true);
            area.AmmendInfo(
                FrameSettingsField.MSAAMode,
                overridedDefaultValue: defaultFrameSettings?.msaaMode ?? MSAAMode.FromHDRPAsset,
                customGetter: () => serialized.msaaMode.GetEnumValue<MSAAMode>(),
                customSetter: v => serialized.msaaMode.SetEnumValue((MSAAMode)v),
                hasMixedValues: serialized.msaaMode.hasMultipleDifferentValues
            );

            area.AmmendInfo(FrameSettingsField.DecalLayers, ignoreDependencies: true);
            area.AmmendInfo(FrameSettingsField.ObjectMotionVectors, ignoreDependencies: true);
            area.AmmendInfo(FrameSettingsField.TransparentsWriteMotionVector, ignoreDependencies: true);

            var hdrpAsset = GetHDRPAssetFor(owner);
            bool isDefaultSetting = (defaultFrameSettings != null);
            RenderPipelineSettings qualityLevelSettings = hdrpAsset?.currentPlatformRenderPipelineSettings ?? default;
            area.AmmendInfo(
                FrameSettingsField.LODBiasMode,
                overridedDefaultValue: (isDefaultSetting) ? defaultFrameSettings?.lodBiasMode : serialized.lodBiasMode.GetEnumValue<LODBiasMode>(),
                customGetter: () => serialized.lodBiasMode.GetEnumValue<LODBiasMode>(),
                customSetter: v => serialized.lodBiasMode.SetEnumValue((LODBiasMode)v),
                hasMixedValues: serialized.lodBiasMode.hasMultipleDifferentValues
            );
            area.AmmendInfo(FrameSettingsField.LODBiasQualityLevel,
                overridedDefaultValue: (isDefaultSetting) ? (ScalableLevel3ForFrameSettingsUIOnly) defaultFrameSettings?.lodBiasQualityLevel : (ScalableLevel3ForFrameSettingsUIOnly)serialized.lodBiasQualityLevel.intValue,
                customGetter: () => (ScalableLevel3ForFrameSettingsUIOnly)serialized.lodBiasQualityLevel.intValue,
                customSetter: v => serialized.lodBiasQualityLevel.intValue = (int)v,
                overrideable: () => serialized.lodBiasMode.GetEnumValue<LODBiasMode>() != LODBiasMode.OverrideQualitySettings,
                ignoreDependencies: true,
                hasMixedValues: serialized.lodBiasQualityLevel.hasMultipleDifferentValues);

            area.AmmendInfo(FrameSettingsField.LODBias,
                overridedDefaultValue: hdrpAsset ? qualityLevelSettings.lodBias[serialized.lodBiasQualityLevel.intValue] : 0,
                customGetter: () => serialized.lodBias.floatValue,
                customSetter: v => serialized.lodBias.floatValue = (float)v,
                overrideable: () => serialized.lodBiasMode.GetEnumValue<LODBiasMode>() != LODBiasMode.FromQualitySettings,
                ignoreDependencies: true,
                labelOverride: serialized.lodBiasMode.GetEnumValue<LODBiasMode>() == LODBiasMode.ScaleQualitySettings ? "Scale Factor" : "LOD Bias",
                hasMixedValues: serialized.lodBias.hasMultipleDifferentValues);

            area.AmmendInfo(
                FrameSettingsField.MaximumLODLevelMode,
                overridedDefaultValue: (isDefaultSetting) ? defaultFrameSettings?.maximumLODLevelMode : serialized.maximumLODLevelMode.GetEnumValue<MaximumLODLevelMode>(),
                customGetter: () => serialized.maximumLODLevelMode.GetEnumValue<MaximumLODLevelMode>(),
                customSetter: v => serialized.maximumLODLevelMode.SetEnumValue((MaximumLODLevelMode)v),
                hasMixedValues: serialized.maximumLODLevelMode.hasMultipleDifferentValues
            );
            area.AmmendInfo(FrameSettingsField.MaximumLODLevelQualityLevel,
                overridedDefaultValue: (isDefaultSetting) ? (ScalableLevel3ForFrameSettingsUIOnly) defaultFrameSettings?.maximumLODLevelQualityLevel : (ScalableLevel3ForFrameSettingsUIOnly)serialized.maximumLODLevelQualityLevel.intValue,
                customGetter: () => (ScalableLevel3ForFrameSettingsUIOnly)serialized.maximumLODLevelQualityLevel.intValue,
                customSetter: v => serialized.maximumLODLevelQualityLevel.intValue = (int)v,
                overrideable: () => serialized.maximumLODLevelMode.GetEnumValue<MaximumLODLevelMode>() != MaximumLODLevelMode.OverrideQualitySettings,
                ignoreDependencies: true,
                hasMixedValues: serialized.maximumLODLevelQualityLevel.hasMultipleDifferentValues);

            area.AmmendInfo(FrameSettingsField.MaximumLODLevel,
                overridedDefaultValue: hdrpAsset ? qualityLevelSettings.maximumLODLevel[serialized.maximumLODLevelQualityLevel.intValue] : 0,
                customGetter: () => serialized.maximumLODLevel.intValue,
                customSetter: v => serialized.maximumLODLevel.intValue = (int)v,
                overrideable: () => serialized.maximumLODLevelMode.GetEnumValue<MaximumLODLevelMode>() != MaximumLODLevelMode.FromQualitySettings,
                ignoreDependencies: true,
                labelOverride: serialized.maximumLODLevelMode.GetEnumValue<MaximumLODLevelMode>() == MaximumLODLevelMode.OffsetQualitySettings ? "Offset Factor" : "Maximum LOD Level",
                hasMixedValues: serialized.maximumLODLevel.hasMultipleDifferentValues);

            area.AmmendInfo(FrameSettingsField.MaterialQualityLevel,
                overridedDefaultValue: defaultFrameSettings?.materialQuality.Into() ?? MaterialQualityMode.Medium,
                customGetter: () => ((MaterialQuality)serialized.materialQuality.intValue).Into(),
                customSetter: v => serialized.materialQuality.intValue = (int)((MaterialQualityMode)v).Into(),
                hasMixedValues: serialized.materialQuality.hasMultipleDifferentValues
            );

            area.Draw(withOverride);
            GUI.enabled = isGUIenabled;
        }

        // Use an enum to have appropriate UI enum field in the frame setting api
        // Do not use anywhere else
        enum ScalableLevel3ForFrameSettingsUIOnly
        {
            Low,
            Medium,
            High
        }

        static internal void Drawer_SectionLightingSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            bool isGUIenabled = GUI.enabled;

            FrameSettings? defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            var hdrpAsset = GetHDRPAssetFor(owner);
            RenderPipelineSettings qualityLevelSettings = hdrpAsset?.currentPlatformRenderPipelineSettings ?? default;

            var area = OverridableFrameSettingsArea.GetGroupContent(1, defaultFrameSettings, serialized);

            area.AmmendInfo(FrameSettingsField.Volumetrics, ignoreDependencies: true);
            area.AmmendInfo(FrameSettingsField.ReprojectionForVolumetrics, ignoreDependencies: true);
            area.AmmendInfo(FrameSettingsField.TransparentSSR, ignoreDependencies: true);

            area.AmmendInfo(
                FrameSettingsField.SssQualityMode,
                overridedDefaultValue: SssQualityMode.FromQualitySettings,
                customGetter: () => serialized.sssQualityMode.GetEnumValue<SssQualityMode>(),
                customSetter: v => serialized.sssQualityMode.SetEnumValue((SssQualityMode)v),
                overrideable: () => serialized.IsEnabled(FrameSettingsField.SubsurfaceScattering) ?? false,
                ignoreDependencies: true,
                hasMixedValues: serialized.sssQualityMode.hasMultipleDifferentValues
            );
            area.AmmendInfo(FrameSettingsField.SssQualityLevel,
                overridedDefaultValue: ScalableLevel3ForFrameSettingsUIOnly.Low,
                customGetter: () => (ScalableLevel3ForFrameSettingsUIOnly)serialized.sssQualityLevel.intValue,// 3 levels
                customSetter: v => serialized.sssQualityLevel.intValue = Math.Max(0, Math.Min((int)v, 2)),// Levels 0-2
                overrideable: () => (serialized.IsEnabled(FrameSettingsField.SubsurfaceScattering) ?? false)
                && (serialized.sssQualityMode.GetEnumValue<SssQualityMode>() == SssQualityMode.FromQualitySettings),
                ignoreDependencies: true,
                hasMixedValues: serialized.sssQualityLevel.hasMultipleDifferentValues
            );
            area.AmmendInfo(FrameSettingsField.SssCustomSampleBudget,
                overridedDefaultValue: (int)DefaultSssSampleBudgetForQualityLevel.Low,
                customGetter: () => serialized.sssCustomSampleBudget.intValue,
                customSetter: v => serialized.sssCustomSampleBudget.intValue = Math.Max(1, Math.Min((int)v, (int)DefaultSssSampleBudgetForQualityLevel.Max)),
                overrideable: () => (serialized.IsEnabled(FrameSettingsField.SubsurfaceScattering) ?? false)
                && (serialized.sssQualityMode.GetEnumValue<SssQualityMode>() != SssQualityMode.FromQualitySettings),
                ignoreDependencies: true,
                hasMixedValues: serialized.sssCustomSampleBudget.hasMultipleDifferentValues
            );
            area.Draw(withOverride);

            GUI.enabled = isGUIenabled;
        }

        static internal void Drawer_SectionAsyncComputeSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            var area = GetFrameSettingSectionContent(2, serialized, owner);

            area.AmmendInfo(FrameSettingsField.LightListAsync, ignoreDependencies: true);
            area.AmmendInfo(FrameSettingsField.SSRAsync, ignoreDependencies: true);
            area.AmmendInfo(FrameSettingsField.SSAOAsync, ignoreDependencies: true);
            area.AmmendInfo(FrameSettingsField.ContactShadowsAsync, ignoreDependencies: true);
            area.AmmendInfo(FrameSettingsField.VolumeVoxelizationsAsync, ignoreDependencies: true);

            area.Draw(withOverride);
        }

        static internal void Drawer_SectionLightLoopSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            var area = GetFrameSettingSectionContent(3, serialized, owner);

            area.AmmendInfo(FrameSettingsField.ComputeLightEvaluation, ignoreDependencies: true);
            area.AmmendInfo(FrameSettingsField.ComputeLightVariants, ignoreDependencies: true);
            area.AmmendInfo(FrameSettingsField.ComputeMaterialVariants, ignoreDependencies: true);

            area.Draw(withOverride);
        }

        static OverridableFrameSettingsArea GetFrameSettingSectionContent(int group, SerializedFrameSettings serialized, Editor owner)
        {
            FrameSettings? defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            var area = OverridableFrameSettingsArea.GetGroupContent(group, defaultFrameSettings, serialized);
            return area;
        }
    }
}
