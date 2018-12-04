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
            AsynComputeSettings = 1 << 3
        }

        readonly static ExpandedState<Expandable, FrameSettings> k_ExpandedState = new ExpandedState<Expandable, FrameSettings>(~(-1), "HDRP");


        internal static CED.IDrawer Inspector(bool withOverride = true)
        {
            return CED.Group(
                CED.Group((serialized, owner) =>
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField(FrameSettingsUI.frameSettingsHeaderContent, EditorStyles.boldLabel);
                }),
                InspectorInnerbox(withOverride),
                CED.Group((serialized, owner) => EditorGUILayout.EndVertical())
                );
        }

        //separated to add enum popup on default frame settings
        internal static CED.IDrawer InspectorInnerbox(bool withOverride = true)
        {
            return CED.Group(
                SectionRenderingPasses(withOverride),
                SectionRenderingSettings(withOverride),
                SectionLightingSettings(withOverride),
                SectionAsyncComputeSettings(withOverride),
                CED.Select(
                    (serialized, owner) => serialized.lightLoopSettings,
                    LightLoopSettingsUI.SectionLightLoopSettings(withOverride)
                    )
                );
        }
        
        public static CED.IDrawer SectionRenderingPasses(bool withOverride)
        {
            return CED.FoldoutGroup(
                renderingPassesHeaderContent,
                Expandable.RenderingPasses,
                k_ExpandedState,
                FoldoutOption.Indent | FoldoutOption.Boxed,
                CED.Group(200, CED.Group((serialized, owner) => Drawer_SectionRenderingPasses(serialized, owner, withOverride)))
                );
        }
        
        public static CED.IDrawer SectionRenderingSettings(bool withOverride)
        {
            return CED.FoldoutGroup(
                renderingSettingsHeaderContent,
                Expandable.RenderingSettings,
                k_ExpandedState,
                FoldoutOption.Indent | FoldoutOption.Boxed,
                CED.Group(250, CED.Group((serialized, owner) => Drawer_SectionRenderingSettings(serialized, owner, withOverride)))
                );
        }
        
        public static CED.IDrawer SectionAsyncComputeSettings(bool withOverride)
        {
            return CED.FoldoutGroup(
                asyncComputeSettingsHeaderContent,
                Expandable.AsynComputeSettings,
                k_ExpandedState,
                FoldoutOption.Indent | FoldoutOption.Boxed,
                CED.Group(250, CED.Group((serialized, owner) => Drawer_SectionAsyncComputeSettings(serialized, owner, withOverride)))
                );
        }
        
        public static CED.IDrawer SectionLightingSettings(bool withOverride)
        {
            return CED.FoldoutGroup(
                lightSettingsHeaderContent,
                Expandable.LightingSettings,
                k_ExpandedState,
                FoldoutOption.Indent | FoldoutOption.Boxed,
                CED.Group(250, CED.Group((serialized, owner) => Drawer_SectionLightingSettings(serialized, owner, withOverride)))
                );
        }

        internal static HDRenderPipelineAsset GetHDRPAssetFor(Editor owner)
        {
            HDRenderPipelineAsset hdrpAsset;
            if (owner is HDRenderPipelineEditor)
            {
                //When drawing the inspector of a selected HDRPAsset in Project windows, access HDRP by owner drawing itself
                hdrpAsset = (owner as HDRenderPipelineEditor).target as HDRenderPipelineAsset;
            }
            else
            {
                //Else rely on GraphicsSettings are you should be in hdrp and owner could be probe or camera.
                hdrpAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            }
            return hdrpAsset;
        }

        internal static FrameSettings GetDefaultFrameSettingsFor(Editor owner)
        {
            HDRenderPipelineAsset hdrpAsset = GetHDRPAssetFor(owner);
            if (owner is IHDProbeEditor)
            {
                if ((owner as IHDProbeEditor).GetTarget(owner.target).mode == ProbeSettings.Mode.Realtime)
                {
                    return hdrpAsset.GetRealtimeReflectionFrameSettings();
                }
                else
                {
                    return hdrpAsset.GetBakedOrCustomReflectionFrameSettings();
                }
            }
            return hdrpAsset.GetFrameSettings();
        }

        static void Drawer_SectionRenderingPasses(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            RenderPipelineSettings hdrpSettings = GetHDRPAssetFor(owner).renderPipelineSettings;
            FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            OverridableSettingsArea area = new OverridableSettingsArea(8);
            area.Add(serialized.enableTransparentPrepass, transparentPrepassContent, () => serialized.overridesTransparentPrepass, a => serialized.overridesTransparentPrepass = a, defaultValue: defaultFrameSettings.enableTransparentPrepass);
            area.Add(serialized.enableTransparentPostpass, transparentPostpassContent, () => serialized.overridesTransparentPostpass, a => serialized.overridesTransparentPostpass = a, defaultValue: defaultFrameSettings.enableTransparentPostpass);
            area.Add(serialized.enableMotionVectors, motionVectorContent, () => serialized.overridesMotionVectors, a => serialized.overridesMotionVectors = a, () => hdrpSettings.supportMotionVectors, defaultValue: defaultFrameSettings.enableMotionVectors);
            area.Add(serialized.enableObjectMotionVectors, objectMotionVectorsContent, () => serialized.overridesObjectMotionVectors, a => serialized.overridesObjectMotionVectors = a, () => hdrpSettings.supportMotionVectors && serialized.enableMotionVectors.boolValue, defaultValue: defaultFrameSettings.enableObjectMotionVectors, indent: 1);
            area.Add(serialized.enableDecals, decalsContent, () => serialized.overridesDecals, a => serialized.overridesDecals = a, () => hdrpSettings.supportDecals, defaultValue: defaultFrameSettings.enableDecals);
            area.Add(serialized.enableRoughRefraction, roughRefractionContent, () => serialized.overridesRoughRefraction, a => serialized.overridesRoughRefraction = a, defaultValue: defaultFrameSettings.enableRoughRefraction);
            area.Add(serialized.enableDistortion, distortionContent, () => serialized.overridesDistortion, a => serialized.overridesDistortion = a, () => hdrpSettings.supportDistortion, defaultValue: defaultFrameSettings.enableDistortion);
            area.Add(serialized.enablePostprocess, postprocessContent, () => serialized.overridesPostprocess, a => serialized.overridesPostprocess = a, defaultValue: defaultFrameSettings.enablePostprocess);
            area.Draw(withOverride);
        }

        static void Drawer_SectionRenderingSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            RenderPipelineSettings hdrpSettings = GetHDRPAssetFor(owner).renderPipelineSettings;
            FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            OverridableSettingsArea area = new OverridableSettingsArea(6);
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
                    defaultShaderLitMode = defaultFrameSettings.shaderLitMode;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException("Unknown ShaderLitMode");
            }

            area.Add(serialized.litShaderMode, litShaderModeContent, () => serialized.overridesShaderLitMode, a => serialized.overridesShaderLitMode = a,
                () => !GL.wireframe && hdrpSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.Both,
                defaultValue: defaultShaderLitMode);

            bool assetAllowMSAA = hdrpSettings.supportedLitShaderMode != RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly && hdrpSettings.supportMSAA;
            bool frameSettingsAllowMSAA = serialized.litShaderMode.enumValueIndex == (int)LitShaderMode.Forward && serialized.overridesShaderLitMode || !serialized.overridesShaderLitMode && defaultShaderLitMode == LitShaderMode.Forward;
            area.Add(serialized.enableMSAA, msaaContent, () => serialized.overridesMSAA, a => serialized.overridesMSAA = a,
                () => !GL.wireframe
                && assetAllowMSAA && frameSettingsAllowMSAA,
                defaultValue: defaultFrameSettings.enableMSAA && hdrpSettings.supportMSAA && !GL.wireframe && (hdrpSettings.supportedLitShaderMode & RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly) != 0 && (serialized.overridesShaderLitMode && serialized.litShaderMode.enumValueIndex == (int)LitShaderMode.Forward || !serialized.overridesShaderLitMode && defaultFrameSettings.shaderLitMode == (int)LitShaderMode.Forward));
            area.Add(serialized.enableDepthPrepassWithDeferredRendering, depthPrepassWithDeferredRenderingContent, () => serialized.overridesDepthPrepassWithDeferredRendering, a => serialized.overridesDepthPrepassWithDeferredRendering = a,
                () => (defaultFrameSettings.shaderLitMode == LitShaderMode.Deferred && !serialized.overridesShaderLitMode || serialized.overridesShaderLitMode && serialized.litShaderMode.enumValueIndex == (int)LitShaderMode.Deferred) && (hdrpSettings.supportedLitShaderMode & RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly) != 0,
                defaultValue: defaultFrameSettings.enableDepthPrepassWithDeferredRendering && (hdrpSettings.supportedLitShaderMode & RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly) != 0 && serialized.litShaderMode.enumValueIndex == (int)LitShaderMode.Deferred);
            area.Add(serialized.enableOpaqueObjects, opaqueObjectsContent, () => serialized.overridesOpaqueObjects, a => serialized.overridesOpaqueObjects = a, defaultValue: defaultFrameSettings.enableOpaqueObjects);
            area.Add(serialized.enableTransparentObjects, transparentObjectsContent, () => serialized.overridesTransparentObjects, a => serialized.overridesTransparentObjects = a, defaultValue: defaultFrameSettings.enableTransparentObjects);
            area.Add(serialized.enableRealtimePlanarReflection, realtimePlanarReflectionContent, () => serialized.overridesRealtimePlanarReflection, a => serialized.overridesRealtimePlanarReflection = a, defaultValue: defaultFrameSettings.enableRealtimePlanarReflection);
            area.Draw(withOverride);
        }

        static void Drawer_SectionAsyncComputeSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            OverridableSettingsArea area = new OverridableSettingsArea(4);
            FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            area.Add(serialized.enableAsyncCompute, asyncComputeContent, () => serialized.overridesAsyncCompute, a => serialized.overridesAsyncCompute = a, defaultValue: defaultFrameSettings.enableAsyncCompute);
            area.Add(serialized.runBuildLightListAsync, lightListAsyncContent, () => serialized.overrideLightListInAsync, a => serialized.overrideLightListInAsync = a, () => serialized.enableAsyncCompute.boolValue, defaultValue: defaultFrameSettings.runLightListAsync, indent: 1);
            area.Add(serialized.runSSRAsync, SSRAsyncContent, () => serialized.overrideSSRInAsync, a => serialized.overrideSSRInAsync = a, () => serialized.enableAsyncCompute.boolValue, defaultValue: defaultFrameSettings.runSSRAsync, indent: 1);
            area.Add(serialized.runSSAOAsync, SSAOAsyncContent, () => serialized.overrideSSAOInAsync, a => serialized.overrideSSAOInAsync = a, () => serialized.enableAsyncCompute.boolValue, defaultValue: defaultFrameSettings.runSSAOAsync, indent: 1);
            area.Add(serialized.runContactShadowsAsync, contactShadowsAsyncContent, () => serialized.overrideContactShadowsInAsync, a => serialized.overrideContactShadowsInAsync = a, () => serialized.enableAsyncCompute.boolValue, defaultValue: defaultFrameSettings.runContactShadowsAsync, indent: 1);
            area.Add(serialized.runVolumeVoxelizationAsync, volumeVoxelizationAsyncContent, () => serialized.overrideVolumeVoxelizationInAsync, a => serialized.overrideVolumeVoxelizationInAsync = a, () => serialized.enableAsyncCompute.boolValue, defaultValue: defaultFrameSettings.runVolumeVoxelizationAsync, indent: 1);
            area.Draw(withOverride);
        }

        static void Drawer_SectionLightingSettings(SerializedFrameSettings serialized, Editor owner, bool withOverride)
        {
            RenderPipelineSettings hdrpSettings = GetHDRPAssetFor(owner).renderPipelineSettings;
            FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            OverridableSettingsArea area = new OverridableSettingsArea(10);
            area.Add(serialized.enableShadow, shadowContent, () => serialized.overridesShadow, a => serialized.overridesShadow = a, defaultValue: defaultFrameSettings.enableShadow);
            area.Add(serialized.enableContactShadow, contactShadowContent, () => serialized.overridesContactShadow, a => serialized.overridesContactShadow = a, defaultValue: defaultFrameSettings.enableContactShadows);
            area.Add(serialized.enableShadowMask, shadowMaskContent, () => serialized.overridesShadowMask, a => serialized.overridesShadowMask = a, () => hdrpSettings.supportShadowMask, defaultValue: defaultFrameSettings.enableShadowMask);
            area.Add(serialized.enableSSR, ssrContent, () => serialized.overridesSSR, a => serialized.overridesSSR = a, () => hdrpSettings.supportSSR, defaultValue: defaultFrameSettings.enableSSR);
            area.Add(serialized.enableSSAO, ssaoContent, () => serialized.overridesSSAO, a => serialized.overridesSSAO = a, () => hdrpSettings.supportSSAO, defaultValue: defaultFrameSettings.enableSSAO);
            area.Add(serialized.enableSubsurfaceScattering, subsurfaceScatteringContent, () => serialized.overridesSubsurfaceScattering, a => serialized.overridesSubsurfaceScattering = a, () => hdrpSettings.supportSubsurfaceScattering, defaultValue: defaultFrameSettings.enableSubsurfaceScattering);
            area.Add(serialized.enableTransmission, transmissionContent, () => serialized.overridesTransmission, a => serialized.overridesTransmission = a, defaultValue: defaultFrameSettings.enableTransmission);
            area.Add(serialized.enableAtmosphericScattering, atmosphericScatteringContent, () => serialized.overridesAtmosphericScaterring, a => serialized.overridesAtmosphericScaterring = a, defaultValue: defaultFrameSettings.enableAtmosphericScattering);
            area.Add(serialized.enableVolumetrics, volumetricContent, () => serialized.overridesVolumetrics, a => serialized.overridesVolumetrics = a, () => hdrpSettings.supportVolumetrics && serialized.enableAtmosphericScattering.boolValue, defaultValue: defaultFrameSettings.enableAtmosphericScattering, indent: 1);
            area.Add(serialized.enableReprojectionForVolumetrics, reprojectionForVolumetricsContent, () => serialized.overridesProjectionForVolumetrics, a => serialized.overridesProjectionForVolumetrics = a, () => hdrpSettings.supportVolumetrics && serialized.enableAtmosphericScattering.boolValue, defaultValue: defaultFrameSettings.enableVolumetrics, indent: 1);
            area.Add(serialized.enableLightLayers, lightLayerContent, () => serialized.overridesLightLayers, a => serialized.overridesLightLayers = a, () => hdrpSettings.supportLightLayers, defaultValue: defaultFrameSettings.enableLightLayers);
            area.Draw(withOverride);
        }
    }
}
