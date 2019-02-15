using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<SerializedFrameSettings>;

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
                    CED.Group(190, (serialized, owner) => Drawer_SectionRenderingSettings(serialized, owner, withOverride))
                    ),
                CED.FoldoutGroup(lightSettingsHeaderContent, Expandable.LightingSettings, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.Boxed,
                    CED.Group(190, (serialized, owner) => Drawer_SectionLightingSettings(serialized, owner, withOverride))
                    ),
                CED.FoldoutGroup(asyncComputeSettingsHeaderContent, Expandable.AsynComputeSettings, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.Boxed,
                    CED.Group(190, (serialized, owner) => Drawer_SectionAsyncComputeSettings(serialized, owner, withOverride))
                    ),
                CED.FoldoutGroup(lightLoopSettingsHeaderContent, Expandable.LightLoop, k_ExpandedState, FoldoutOption.Indent | FoldoutOption.Boxed,
                    CED.Group(190, (serialized, owner) => Drawer_SectionLightLoopSettings(serialized, owner, withOverride))
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
                hdrpAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
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
                overrideable: () => !GL.wireframe && hdrpSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.Both,
                overridedDefaultValue: defaultShaderLitMode);

            bool hdrpAssetSupportForward = hdrpSettings.supportedLitShaderMode != RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly;
            bool hdrpAssetSupportDeferred = hdrpSettings.supportedLitShaderMode != RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly;
            bool frameSettingsOverrideToForward = serialized.GetOverrides(FrameSettingsField.LitShaderMode) && serialized.litShaderMode == LitShaderMode.Forward;
            bool frameSettingsOverrideToDeferred = serialized.GetOverrides(FrameSettingsField.LitShaderMode) && serialized.litShaderMode == LitShaderMode.Deferred;
            bool defaultForwardUsed = !serialized.GetOverrides(FrameSettingsField.LitShaderMode) && defaultShaderLitMode == LitShaderMode.Forward;
            bool defaultDefferedUsed = !serialized.GetOverrides(FrameSettingsField.LitShaderMode) && defaultShaderLitMode == LitShaderMode.Deferred;
            bool msaaEnablable = !GL.wireframe && hdrpAssetSupportForward && hdrpSettings.supportMSAA && (frameSettingsOverrideToForward || defaultForwardUsed);
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
            
            area.AmmendInfo(FrameSettingsField.MotionVectors, overrideable: () => hdrpSettings.supportMotionVectors);
            area.AmmendInfo(FrameSettingsField.ObjectMotionVectors, overrideable: () => hdrpSettings.supportMotionVectors);
            area.AmmendInfo(FrameSettingsField.Decals, overrideable: () => hdrpSettings.supportDecals);
            area.AmmendInfo(FrameSettingsField.Distortion, overrideable: () => hdrpSettings.supportDistortion);
            area.Draw(withOverride);
        }

        static void Drawer_SectionLightingSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            RenderPipelineSettings hdrpSettings = GetHDRPAssetFor(owner).currentPlatformRenderPipelineSettings;
            FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            var area = OverridableFrameSettingsArea.GetGroupContent(1, defaultFrameSettings, serialized);
            area.AmmendInfo(FrameSettingsField.ShadowMask, overrideable: () => hdrpSettings.supportShadowMask);
            area.AmmendInfo(FrameSettingsField.SSR, overrideable: () => hdrpSettings.supportSSR);
            area.AmmendInfo(FrameSettingsField.SSAO, overrideable: () => hdrpSettings.supportSSAO);
            area.AmmendInfo(FrameSettingsField.SubsurfaceScattering, overrideable: () => hdrpSettings.supportSubsurfaceScattering);
            area.AmmendInfo(FrameSettingsField.Volumetrics, overrideable: () => hdrpSettings.supportVolumetrics);
            area.AmmendInfo(FrameSettingsField.ReprojectionForVolumetrics, overrideable: () => hdrpSettings.supportVolumetrics);
            area.AmmendInfo(FrameSettingsField.LightLayers, overrideable: () => hdrpSettings.supportLightLayers);
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
