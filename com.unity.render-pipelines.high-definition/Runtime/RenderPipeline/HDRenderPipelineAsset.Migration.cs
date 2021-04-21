using System;
using System.Collections.Generic; //needed for list of Custom Post Processes injections
using UnityEditor;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipelineAsset : IVersionable<HDRenderPipelineAsset.Version>
    {
        enum Version
        {
            None,
            First,
            UpgradeFrameSettingsToStruct,
            AddAfterPostProcessFrameSetting,
            AddFrameSettingSpecularLighting = 5, // Not used anymore - don't removed the number
            AddReflectionSettings,
            AddPostProcessFrameSettings,
            AddRayTracingFrameSettings,
            AddFrameSettingDirectSpecularLighting,
            AddCustomPostprocessAndCustomPass,
            ScalableSettingsRefactor,
            ShadowFilteringVeryHighQualityRemoval,
            SeparateColorGradingAndTonemappingFrameSettings,
            ReplaceTextureArraysByAtlasForCookieAndPlanar,
            AddedAdaptiveSSS,
            RemoveCookieCubeAtlasToOctahedral2D,
            RoughDistortion,
            VirtualTexturing,
            AddedHDRenderPipelineGlobalSettings
            // If you add more steps here, do not clear settings that are used for the migration to the HDRP Global Settings asset
        }

        static readonly MigrationDescription<Version, HDRenderPipelineAsset> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.UpgradeFrameSettingsToStruct, (HDRenderPipelineAsset data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                FrameSettingsOverrideMask unusedMaskForDefault = new FrameSettingsOverrideMask();
                if (data.m_ObsoleteFrameSettings != null)
                    FrameSettings.MigrateFromClassVersion(ref data.m_ObsoleteFrameSettings, ref data.m_ObsoleteFrameSettingsMovedToDefaultSettings, ref unusedMaskForDefault);
                if (data.m_ObsoleteBakedOrCustomReflectionFrameSettings != null)
                    FrameSettings.MigrateFromClassVersion(ref data.m_ObsoleteBakedOrCustomReflectionFrameSettings, ref data.m_ObsoleteBakedOrCustomReflectionFrameSettingsMovedToDefaultSettings, ref unusedMaskForDefault);
                if (data.m_ObsoleteRealtimeReflectionFrameSettings != null)
                    FrameSettings.MigrateFromClassVersion(ref data.m_ObsoleteRealtimeReflectionFrameSettings, ref data.m_ObsoleteRealtimeReflectionFrameSettingsMovedToDefaultSettings, ref unusedMaskForDefault);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.AddAfterPostProcessFrameSetting, (HDRenderPipelineAsset data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                FrameSettings.MigrateToAfterPostprocess(ref data.m_ObsoleteFrameSettingsMovedToDefaultSettings);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.AddReflectionSettings, (HDRenderPipelineAsset data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                FrameSettings.MigrateToDefaultReflectionSettings(ref data.m_ObsoleteFrameSettingsMovedToDefaultSettings);
                FrameSettings.MigrateToNoReflectionSettings(ref data.m_ObsoleteBakedOrCustomReflectionFrameSettingsMovedToDefaultSettings);
                FrameSettings.MigrateToNoReflectionRealtimeSettings(ref data.m_ObsoleteRealtimeReflectionFrameSettingsMovedToDefaultSettings);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.AddPostProcessFrameSettings, (HDRenderPipelineAsset data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                FrameSettings.MigrateToPostProcess(ref data.m_ObsoleteFrameSettingsMovedToDefaultSettings);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.AddRayTracingFrameSettings, (HDRenderPipelineAsset data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                FrameSettings.MigrateToRayTracing(ref data.m_ObsoleteFrameSettingsMovedToDefaultSettings);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.AddFrameSettingDirectSpecularLighting, (HDRenderPipelineAsset data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                FrameSettings.MigrateToDirectSpecularLighting(ref data.m_ObsoleteFrameSettingsMovedToDefaultSettings);
                FrameSettings.MigrateToNoDirectSpecularLighting(ref data.m_ObsoleteBakedOrCustomReflectionFrameSettingsMovedToDefaultSettings);
                FrameSettings.MigrateToDirectSpecularLighting(ref data.m_ObsoleteRealtimeReflectionFrameSettingsMovedToDefaultSettings);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.AddCustomPostprocessAndCustomPass, (HDRenderPipelineAsset data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                FrameSettings.MigrateToCustomPostprocessAndCustomPass(ref data.m_ObsoleteFrameSettingsMovedToDefaultSettings);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.ScalableSettingsRefactor, (HDRenderPipelineAsset data) =>
            {
                ref var shadowInit = ref data.m_RenderPipelineSettings.hdShadowInitParams;
                shadowInit.shadowResolutionArea.schemaId = ScalableSettingSchemaId.With4Levels;
                shadowInit.shadowResolutionDirectional.schemaId = ScalableSettingSchemaId.With4Levels;
                shadowInit.shadowResolutionPunctual.schemaId = ScalableSettingSchemaId.With4Levels;
            }),
            MigrationStep.New(Version.ShadowFilteringVeryHighQualityRemoval, (HDRenderPipelineAsset data) =>
            {
                ref var shadowInit = ref data.m_RenderPipelineSettings.hdShadowInitParams;
                shadowInit.shadowFilteringQuality = shadowInit.shadowFilteringQuality > HDShadowFilteringQuality.High ? HDShadowFilteringQuality.High : shadowInit.shadowFilteringQuality;
            }),
            MigrationStep.New(Version.SeparateColorGradingAndTonemappingFrameSettings, (HDRenderPipelineAsset data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                FrameSettings.MigrateToSeparateColorGradingAndTonemapping(ref data.m_ObsoleteFrameSettingsMovedToDefaultSettings);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.ReplaceTextureArraysByAtlasForCookieAndPlanar, (HDRenderPipelineAsset data) =>
            {
                ref var lightLoopSettings = ref data.m_RenderPipelineSettings.lightLoopSettings;

#pragma warning disable 618 // Type or member is obsolete
                float cookieAtlasSize = Mathf.Sqrt((int)lightLoopSettings.cookieAtlasSize * (int)lightLoopSettings.cookieAtlasSize * lightLoopSettings.cookieTexArraySize);
                float planarSize = Mathf.Sqrt((int)lightLoopSettings.planarReflectionAtlasSize * (int)lightLoopSettings.planarReflectionAtlasSize * lightLoopSettings.maxPlanarReflectionOnScreen);
#pragma warning restore 618

                // The atlas only supports power of two sizes
                cookieAtlasSize = (float)Mathf.NextPowerOfTwo((int)cookieAtlasSize);
                planarSize = (float)Mathf.NextPowerOfTwo((int)planarSize);

                // Clamp to avoid too large atlases
                cookieAtlasSize = Mathf.Clamp(cookieAtlasSize, (int)CookieAtlasResolution.CookieResolution256, (int)CookieAtlasResolution.CookieResolution8192);
                planarSize = Mathf.Clamp(planarSize, (int)PlanarReflectionAtlasResolution.Resolution256, (int)PlanarReflectionAtlasResolution.Resolution8192);

                lightLoopSettings.cookieAtlasSize = (CookieAtlasResolution)cookieAtlasSize;
                lightLoopSettings.planarReflectionAtlasSize = (PlanarReflectionAtlasResolution)planarSize;
            }),
            MigrationStep.New(Version.AddedAdaptiveSSS, (HDRenderPipelineAsset data) =>
            {
            #pragma warning disable 618 // Type or member is obsolete
                bool previouslyHighQuality = data.m_RenderPipelineSettings.m_ObsoleteincreaseSssSampleCount;
                FrameSettings.MigrateSubsurfaceParams(ref data.m_ObsoleteFrameSettingsMovedToDefaultSettings,                  previouslyHighQuality);
                FrameSettings.MigrateSubsurfaceParams(ref data.m_ObsoleteBakedOrCustomReflectionFrameSettingsMovedToDefaultSettings, previouslyHighQuality);
                FrameSettings.MigrateSubsurfaceParams(ref data.m_ObsoleteRealtimeReflectionFrameSettingsMovedToDefaultSettings,      previouslyHighQuality);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.RemoveCookieCubeAtlasToOctahedral2D, (HDRenderPipelineAsset data) =>
            {
                ref var lightLoopSettings = ref data.m_RenderPipelineSettings.lightLoopSettings;

#pragma warning disable 618 // Type or member is obsolete
                float cookieAtlasSize = Mathf.Sqrt((int)lightLoopSettings.cookieAtlasSize * (int)lightLoopSettings.cookieAtlasSize * lightLoopSettings.cookieTexArraySize);
                float planarSize = Mathf.Sqrt((int)lightLoopSettings.planarReflectionAtlasSize * (int)lightLoopSettings.planarReflectionAtlasSize * lightLoopSettings.maxPlanarReflectionOnScreen);
#pragma warning restore 618

                Debug.Log("HDRP Internally changed the storage of Cube Cookie to use Octahedral Projection inside the 2D Cookie Atlas. It is recommended that you increase the size of the 2D Cookie Atlas if your cookies no longer fit.");
            }),
            MigrationStep.New(Version.RoughDistortion, (HDRenderPipelineAsset data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                FrameSettings.MigrateRoughDistortion(ref data.m_ObsoleteFrameSettingsMovedToDefaultSettings);
                FrameSettings.MigrateRoughDistortion(ref data.m_ObsoleteBakedOrCustomReflectionFrameSettingsMovedToDefaultSettings);
                FrameSettings.MigrateRoughDistortion(ref data.m_ObsoleteRealtimeReflectionFrameSettingsMovedToDefaultSettings);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.VirtualTexturing, (HDRenderPipelineAsset data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                FrameSettings.MigrateVirtualTexturing(ref data.m_ObsoleteFrameSettingsMovedToDefaultSettings);
                FrameSettings.MigrateVirtualTexturing(ref data.m_ObsoleteBakedOrCustomReflectionFrameSettingsMovedToDefaultSettings);
                FrameSettings.MigrateVirtualTexturing(ref data.m_ObsoleteRealtimeReflectionFrameSettingsMovedToDefaultSettings);
#pragma warning restore 618
            }) ,
            MigrationStep.New(Version.AddedHDRenderPipelineGlobalSettings, (HDRenderPipelineAsset data) =>
            {
#if UNITY_EDITOR
                if (data == GraphicsSettings.defaultRenderPipeline)
                {
#pragma warning disable 618 // Type or member is obsolete
                    // We need to duplicate the migration logic for the MSAA change on frame settings here (from HDRenderPipelineGlobalSettings.Migration.cs)
                    // The reason is that, if we are upgrading a project prior to global settings change, the new global setting will be created to the latest version
                    // So it will skip the MSAA migration code. To fix that, we need to migrate frame settings before creating the new asset.
                    FrameSettingsOverrideMask unusedMaskForDefault = new FrameSettingsOverrideMask();
                    FrameSettings.MigrateMSAA(ref data.m_ObsoleteFrameSettingsMovedToDefaultSettings, ref unusedMaskForDefault);
                    FrameSettings.MigrateMSAA(ref data.m_ObsoleteBakedOrCustomReflectionFrameSettingsMovedToDefaultSettings, ref unusedMaskForDefault);
                    FrameSettings.MigrateMSAA(ref data.m_ObsoleteRealtimeReflectionFrameSettingsMovedToDefaultSettings, ref unusedMaskForDefault);
#pragma warning restore 618

                    HDRenderPipelineGlobalSettings.MigrateFromHDRPAsset(data);
                }
#endif
            })
        );

        [SerializeField]
        Version m_Version = MigrationDescription.LastVersion<Version>();
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        void OnEnable() => k_Migration.Migrate(this);

#pragma warning disable 618 // Type or member is obsolete
        [SerializeField]
        [FormerlySerializedAs("serializedFrameSettings"), FormerlySerializedAs("m_FrameSettings"), Obsolete("For data migration")]
        ObsoleteFrameSettings m_ObsoleteFrameSettings;
        [SerializeField]
        [FormerlySerializedAs("m_BakedOrCustomReflectionFrameSettings"), Obsolete("For data migration")]
        ObsoleteFrameSettings m_ObsoleteBakedOrCustomReflectionFrameSettings;
        [SerializeField]
        [FormerlySerializedAs("m_RealtimeReflectionFrameSettings"), Obsolete("For data migration")]
        ObsoleteFrameSettings m_ObsoleteRealtimeReflectionFrameSettings;

        #region Settings Moved from the HDRP Asset to HDRenderPipelineGlobalSettings
        [SerializeField]
        [FormerlySerializedAs("m_DefaultVolumeProfile"), Obsolete("Moved from HDRPAsset to HDGlobal Settings")]
        internal VolumeProfile m_ObsoleteDefaultVolumeProfile;
        [SerializeField]
        [FormerlySerializedAs("m_DefaultLookDevProfile"), Obsolete("Moved from HDRPAsset to HDGlobal Settings")]
        internal VolumeProfile m_ObsoleteDefaultLookDevProfile;

        [SerializeField]
        [FormerlySerializedAs("m_RenderingPathDefaultCameraFrameSettings"), Obsolete("Moved from HDRPAsset to HDGlobal Settings")]
        internal FrameSettings m_ObsoleteFrameSettingsMovedToDefaultSettings;
        [SerializeField]
        [FormerlySerializedAs("m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings"), Obsolete("Moved from HDRPAsset to HDGlobal Settings")]
        internal FrameSettings m_ObsoleteBakedOrCustomReflectionFrameSettingsMovedToDefaultSettings;
        [SerializeField]
        [FormerlySerializedAs("m_RenderingPathDefaultRealtimeReflectionFrameSettings"), Obsolete("Moved from HDRPAsset to HDGlobal Settings")]
        internal FrameSettings m_ObsoleteRealtimeReflectionFrameSettingsMovedToDefaultSettings;

        [SerializeField]
        [FormerlySerializedAs("m_RenderPipelineResources"), Obsolete("Moved from HDRPAsset to HDGlobal Settings")]
        internal HDRenderPipelineRuntimeResources m_ObsoleteRenderPipelineResources;
        [SerializeField]
        [FormerlySerializedAs("m_RenderPipelineRayTracingResources"), Obsolete("Moved from HDRPAsset to HDGlobal Settings")]
        internal HDRenderPipelineRayTracingResources m_ObsoleteRenderPipelineRayTracingResources;

        [SerializeField]
        [FormerlySerializedAs("beforeTransparentCustomPostProcesses"), Obsolete("Moved from HDRPAsset to HDGlobal Settings")]
        internal List<string> m_ObsoleteBeforeTransparentCustomPostProcesses;
        [SerializeField]
        [FormerlySerializedAs("beforePostProcessCustomPostProcesses"), Obsolete("Moved from HDRPAsset to HDGlobal Settings")]
        internal List<string> m_ObsoleteBeforePostProcessCustomPostProcesses;
        [SerializeField]
        [FormerlySerializedAs("afterPostProcessCustomPostProcesses"), Obsolete("Moved from HDRPAsset to HDGlobal Settings")]
        internal List<string> m_ObsoleteAfterPostProcessCustomPostProcesses;
        [SerializeField]
        [FormerlySerializedAs("beforeTAACustomPostProcesses"), Obsolete("Moved from HDRPAsset to HDGlobal Settings")]
        internal List<string> m_ObsoleteBeforeTAACustomPostProcesses;

        [SerializeField]
        [FormerlySerializedAs("shaderVariantLogLevel"), Obsolete("Moved from HDRPAsset to HDGlobal Settings")]
        internal ShaderVariantLogLevel m_ObsoleteShaderVariantLogLevel;
        [SerializeField]
        [FormerlySerializedAs("m_LensAttenuation"), Obsolete("Moved from HDRPAsset to HDGlobal Settings")]
        internal LensAttenuationMode m_ObsoleteLensAttenuation;
        [SerializeField]
        [FormerlySerializedAs("diffusionProfileSettingsList"), Obsolete("Moved from HDRPAsset to HDGlobal Settings")]
        internal DiffusionProfileSettings[] m_ObsoleteDiffusionProfileSettingsList;
        #endregion

#pragma warning restore 618
    }
}
