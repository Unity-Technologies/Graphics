using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using Utilities;

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
            if (quality == (MaterialQuality) 0)
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

        internal static CED.IDrawer Inspector(bool withOverride = true) => CED.Group(
                CED.Group((serialized, owner) =>
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField(FrameSettingsUI.frameSettingsHeaderContent, EditorStyles.boldLabel);
                }),
                InspectorInnerbox(withOverride),
                CED.Group((serialized, owner) => EditorGUILayout.EndVertical())
                );

        //separated to add enum popup on default frame settings
        internal static CED.IDrawer InspectorInnerbox(bool withOverride = true) => CED.Group(
                CED.FoldoutGroup(renderingSettingsHeaderContent, Expandable.RenderingPasses, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.Boxed,
                    CED.Group(206, (serialized, owner) => Drawer_SectionRenderingSettings(serialized, owner, withOverride))
                    ),
                CED.FoldoutGroup(lightSettingsHeaderContent, Expandable.LightingSettings, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.Boxed,
                    CED.Group(206, (serialized, owner) => Drawer_SectionLightingSettings(serialized, owner, withOverride))
                    ),
                CED.FoldoutGroup(asyncComputeSettingsHeaderContent, Expandable.AsynComputeSettings, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.Boxed,
                    CED.Group(206, (serialized, owner) => Drawer_SectionAsyncComputeSettings(serialized, owner, withOverride))
                    ),
                CED.FoldoutGroup(lightLoopSettingsHeaderContent, Expandable.LightLoop, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.Boxed,
                    CED.Group(206, (serialized, owner) => Drawer_SectionLightLoopSettings(serialized, owner, withOverride))
                    )
                );

        static HDRenderPipelineAsset GetHDRPAssetFor(Editor owner)
        {
            HDRenderPipelineAsset hdrpAsset;
            if (owner is HDRenderPipelineEditor)
            {
                // When drawing the inspector of a selected HDRPAsset in Project windows, access HDRP by owner drawing itself
                hdrpAsset = (owner as HDRenderPipelineEditor).target as HDRenderPipelineAsset;
            }
            else
            {
                // Else rely on GraphicsSettings are you should be in hdrp and owner could be probe or camera.
                hdrpAsset = HDRenderPipeline.currentAsset;
            }
            return hdrpAsset;
        }

        static FrameSettings GetDefaultFrameSettingsFor(Editor owner)
        {
            HDRenderPipelineAsset hdrpAsset = GetHDRPAssetFor(owner);
            if (owner is IHDProbeEditor)
            {
                if ((owner as IHDProbeEditor).GetTarget(owner.target).mode == ProbeSettings.Mode.Realtime)
                    return hdrpAsset.GetDefaultFrameSettings(FrameSettingsRenderType.RealtimeReflection);
                else
                    return hdrpAsset.GetDefaultFrameSettings(FrameSettingsRenderType.CustomOrBakedReflection);
            }
            return hdrpAsset.GetDefaultFrameSettings(FrameSettingsRenderType.Camera);
        }

        static void Drawer_SectionRenderingSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            RenderPipelineSettings hdrpSettings = GetHDRPAssetFor(owner).currentPlatformRenderPipelineSettings;
            FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            var area = OverridableFrameSettingsArea.GetGroupContent(0, defaultFrameSettings, serialized);

            LitShaderMode defaultShaderLitMode;
            switch (hdrpSettings.supportedLitShaderMode)
            {
                case RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly:
                    defaultShaderLitMode = LitShaderMode.Forward;
                    break;
                case RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly:
                    defaultShaderLitMode = LitShaderMode.Deferred;
                    break;
                case RenderPipelineSettings.SupportedLitShaderMode.Both:
                    defaultShaderLitMode = defaultFrameSettings.litShaderMode;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException("Unknown ShaderLitMode");
            }

            area.AmmendInfo(FrameSettingsField.LitShaderMode,
                overrideable: () => hdrpSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.Both,
                overridedDefaultValue: defaultShaderLitMode);

            bool hdrpAssetSupportForward = hdrpSettings.supportedLitShaderMode != RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly;
            bool hdrpAssetSupportDeferred = hdrpSettings.supportedLitShaderMode != RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly;
            bool frameSettingsOverrideToForward = serialized.GetOverrides(FrameSettingsField.LitShaderMode) && serialized.litShaderMode == LitShaderMode.Forward;
            bool frameSettingsOverrideToDeferred = serialized.GetOverrides(FrameSettingsField.LitShaderMode) && serialized.litShaderMode == LitShaderMode.Deferred;
            bool defaultForwardUsed = !serialized.GetOverrides(FrameSettingsField.LitShaderMode) && defaultShaderLitMode == LitShaderMode.Forward;
            bool defaultDefferedUsed = !serialized.GetOverrides(FrameSettingsField.LitShaderMode) && defaultShaderLitMode == LitShaderMode.Deferred;
            bool msaaEnablable = hdrpAssetSupportForward && hdrpSettings.supportMSAA && (frameSettingsOverrideToForward || defaultForwardUsed);
            bool depthPrepassEnablable = hdrpAssetSupportDeferred && (defaultDefferedUsed || frameSettingsOverrideToDeferred);
            area.AmmendInfo(FrameSettingsField.MSAA,
                overrideable: () => msaaEnablable,
                overridedDefaultValue: msaaEnablable && defaultFrameSettings.IsEnabled(FrameSettingsField.MSAA),
                customOverrideable: () =>
                {
                    switch (hdrpSettings.supportedLitShaderMode)
                    {
                        case RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly:
                            return false; //negative dependency
                        case RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly:
                            return true; //negative dependency
                        case RenderPipelineSettings.SupportedLitShaderMode.Both:
                            return !(frameSettingsOverrideToForward || defaultForwardUsed); //negative dependency
                        default:
                            throw new System.ArgumentOutOfRangeException("Unknown ShaderLitMode");
                    }
                });
            area.AmmendInfo(FrameSettingsField.DepthPrepassWithDeferredRendering,
                overrideable: () => depthPrepassEnablable,
                overridedDefaultValue: depthPrepassEnablable && defaultFrameSettings.IsEnabled(FrameSettingsField.DepthPrepassWithDeferredRendering),
                customOverrideable: () =>
                {
                    switch (hdrpSettings.supportedLitShaderMode)
                    {
                        case RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly:
                            return false;
                        case RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly:
                            return true;
                        case RenderPipelineSettings.SupportedLitShaderMode.Both:
                            return frameSettingsOverrideToDeferred || defaultDefferedUsed;
                        default:
                            throw new System.ArgumentOutOfRangeException("Unknown ShaderLitMode");
                    }
                });

            area.AmmendInfo(FrameSettingsField.RayTracing, overrideable: () => hdrpSettings.supportRayTracing);
            area.AmmendInfo(FrameSettingsField.MotionVectors, overrideable: () => hdrpSettings.supportMotionVectors);
            area.AmmendInfo(FrameSettingsField.ObjectMotionVectors, overrideable: () => hdrpSettings.supportMotionVectors);
            area.AmmendInfo(FrameSettingsField.Decals, overrideable: () => hdrpSettings.supportDecals);
            area.AmmendInfo(FrameSettingsField.Distortion, overrideable: () => hdrpSettings.supportDistortion);

            area.AmmendInfo(
                FrameSettingsField.LODBiasMode,
                overridedDefaultValue: LODBiasMode.FromQualitySettings,
                customGetter: () => serialized.lodBiasMode.GetEnumValue<LODBiasMode>(),
                customSetter: v => serialized.lodBiasMode.SetEnumValue((LODBiasMode)v)
            );
            area.AmmendInfo(FrameSettingsField.LODBiasQualityLevel,
                overridedDefaultValue: ScalableLevel3ForFrameSettingsUIOnly.Low,
                customGetter: () => (ScalableLevel3ForFrameSettingsUIOnly)serialized.lodBiasQualityLevel.intValue,
                customSetter: v => serialized.lodBiasQualityLevel.intValue = (int)v,
                customOverrideable: () => serialized.lodBiasMode.GetEnumValue<LODBiasMode>() != LODBiasMode.OverrideQualitySettings);

            area.AmmendInfo(FrameSettingsField.LODBias,
                overridedDefaultValue: QualitySettings.lodBias,
                customGetter: () => serialized.lodBias.floatValue,
                customSetter: v => serialized.lodBias.floatValue = (float)v,
                customOverrideable: () => serialized.lodBiasMode.GetEnumValue<LODBiasMode>() != LODBiasMode.FromQualitySettings,
                labelOverride: serialized.lodBiasMode.GetEnumValue<LODBiasMode>() == LODBiasMode.ScaleQualitySettings ? "Scale Factor" : "LOD Bias");

            area.AmmendInfo(
                FrameSettingsField.MaximumLODLevelMode,
                overridedDefaultValue: MaximumLODLevelMode.FromQualitySettings,
                customGetter: () => serialized.maximumLODLevelMode.GetEnumValue<MaximumLODLevelMode>(),
                customSetter: v => serialized.maximumLODLevelMode.SetEnumValue((MaximumLODLevelMode)v)
            );
            area.AmmendInfo(FrameSettingsField.MaximumLODLevelQualityLevel,
                overridedDefaultValue: ScalableLevel3ForFrameSettingsUIOnly.Low,
                customGetter: () => (ScalableLevel3ForFrameSettingsUIOnly)serialized.maximumLODLevelQualityLevel.intValue,
                customSetter: v => serialized.maximumLODLevelQualityLevel.intValue = (int)v,
                customOverrideable: () => serialized.maximumLODLevelMode.GetEnumValue<MaximumLODLevelMode>() != MaximumLODLevelMode.OverrideQualitySettings);

            area.AmmendInfo(FrameSettingsField.MaximumLODLevel,
                overridedDefaultValue: QualitySettings.maximumLODLevel,
                customGetter: () => serialized.maximumLODLevel.intValue,
                customSetter: v => serialized.maximumLODLevel.intValue = (int)v,
                customOverrideable: () => serialized.maximumLODLevelMode.GetEnumValue<MaximumLODLevelMode>() != MaximumLODLevelMode.FromQualitySettings,
                labelOverride: serialized.maximumLODLevelMode.GetEnumValue<MaximumLODLevelMode>() == MaximumLODLevelMode.OffsetQualitySettings ? "Offset Factor" : "Maximum LOD Level");

            area.AmmendInfo(FrameSettingsField.MaterialQualityLevel,
                overridedDefaultValue: defaultFrameSettings.materialQuality.Into(),
                customGetter: () => ((MaterialQuality)serialized.materialQuality.intValue).Into(),
                customSetter: v => serialized.materialQuality.intValue = (int)((MaterialQualityMode)v).Into()
            );

            area.Draw(withOverride);
        }

        // Use an enum to have appropriate UI enum field in the frame setting api
        // Do not use anywhere else
        enum ScalableLevel3ForFrameSettingsUIOnly
        {
            Low,
            Medium,
            High
        }

        static void Drawer_SectionLightingSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            RenderPipelineSettings hdrpSettings = GetHDRPAssetFor(owner).currentPlatformRenderPipelineSettings;
            FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            var area = OverridableFrameSettingsArea.GetGroupContent(1, defaultFrameSettings, serialized);
            area.AmmendInfo(FrameSettingsField.Shadowmask, overrideable: () => hdrpSettings.supportShadowMask);
            area.AmmendInfo(FrameSettingsField.SSR, overrideable: () => hdrpSettings.supportSSR);
            area.AmmendInfo(FrameSettingsField.SSAO, overrideable: () => hdrpSettings.supportSSAO);
            area.AmmendInfo(FrameSettingsField.SubsurfaceScattering, overrideable: () => hdrpSettings.supportSubsurfaceScattering);
            area.AmmendInfo(FrameSettingsField.Volumetrics, overrideable: () => hdrpSettings.supportVolumetrics);
            area.AmmendInfo(FrameSettingsField.ReprojectionForVolumetrics, overrideable: () => hdrpSettings.supportVolumetrics);
            area.AmmendInfo(FrameSettingsField.LightLayers, overrideable: () => hdrpSettings.supportLightLayers);
            area.AmmendInfo(FrameSettingsField.ScreenSpaceShadows, overrideable: () => hdrpSettings.hdShadowInitParams.supportScreenSpaceShadows);
            area.Draw(withOverride);
        }

        static void Drawer_SectionAsyncComputeSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            var area = GetFrameSettingSectionContent(2, serialized, owner);
            area.Draw(withOverride);
        }

        static void Drawer_SectionLightLoopSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            var area = GetFrameSettingSectionContent(3, serialized, owner);
            area.Draw(withOverride);
        }

        static OverridableFrameSettingsArea GetFrameSettingSectionContent(int group, SerializedFrameSettings serialized, Editor owner)
        {
            RenderPipelineSettings hdrpSettings = GetHDRPAssetFor(owner).currentPlatformRenderPipelineSettings;
            FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            var area = OverridableFrameSettingsArea.GetGroupContent(group, defaultFrameSettings, serialized);
            return area;
        }
    }
}
