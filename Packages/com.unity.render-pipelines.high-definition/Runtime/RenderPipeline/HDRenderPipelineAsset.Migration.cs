using System;
using System.Collections.Generic; //needed for list of Custom Post Processes injections
using UnityEngine.Serialization;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.PackageManager;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipelineAsset : IVersionable<HDRenderPipelineAsset.Version>, IMigratableAsset
    {
        // /!\ For each new version, you must now upgrade asset in HDRP_Runtime, HDRP_Performance and SRP_SmokeTest test project.
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
            AddedHDRenderPipelineGlobalSettings,
            DecalSurfaceGradient,
            RemovalOfUpscaleFilter,
            CombinedPlanarAndCubemapReflectionAtlases
            // If you add more steps here, do not clear settings that are used for the migration to the HDRP Global Settings asset
        }

        #region Migration steps

        private static readonly MigrationDescription<Version, HDRenderPipelineAsset> k_Migration = MigrationDescription.New(
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

                // The atlas only supports power of two sizes
                cookieAtlasSize = (float)Mathf.NextPowerOfTwo((int)cookieAtlasSize);
                planarSize = (float)Mathf.NextPowerOfTwo((int)planarSize);

                // Clamp to avoid too large atlases
                cookieAtlasSize = Mathf.Clamp(cookieAtlasSize, (int)CookieAtlasResolution.CookieResolution256, (int)CookieAtlasResolution.CookieResolution8192);
                planarSize = Mathf.Clamp(planarSize, (int)PlanarReflectionAtlasResolution.Resolution256, (int)PlanarReflectionAtlasResolution.Resolution8192);

                lightLoopSettings.cookieAtlasSize = (CookieAtlasResolution)cookieAtlasSize;
                lightLoopSettings.planarReflectionAtlasSize = (PlanarReflectionAtlasResolution)planarSize;
#pragma warning restore 618
            }),
            MigrationStep.New(Version.AddedAdaptiveSSS, (HDRenderPipelineAsset data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                bool previouslyHighQuality = data.m_RenderPipelineSettings.m_ObsoleteincreaseSssSampleCount;
                FrameSettings.MigrateSubsurfaceParams(ref data.m_ObsoleteFrameSettingsMovedToDefaultSettings, previouslyHighQuality);
                FrameSettings.MigrateSubsurfaceParams(ref data.m_ObsoleteBakedOrCustomReflectionFrameSettingsMovedToDefaultSettings, previouslyHighQuality);
                FrameSettings.MigrateSubsurfaceParams(ref data.m_ObsoleteRealtimeReflectionFrameSettingsMovedToDefaultSettings, previouslyHighQuality);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.RemoveCookieCubeAtlasToOctahedral2D, (HDRenderPipelineAsset data) =>
            {
                ref var lightLoopSettings = ref data.m_RenderPipelineSettings.lightLoopSettings;

#pragma warning disable 618 // Type or member is obsolete
                float cookieAtlasSize = Mathf.Sqrt((int)lightLoopSettings.cookieAtlasSize * (int)lightLoopSettings.cookieAtlasSize * lightLoopSettings.cookieTexArraySize);
                float planarSize = Mathf.Sqrt((int)lightLoopSettings.planarReflectionAtlasSize * (int)lightLoopSettings.planarReflectionAtlasSize * lightLoopSettings.maxPlanarReflectionOnScreen);
#pragma warning restore 618

                Debug.Log("HDRP Internally changed the storage of Cube Cookie to use Octahedral Projection inside the 2D Cookie Atlas. It is recommended that you increase the size of the 2D Cookie Atlas if your cookies no longer fit. To fix this, select your HDRP Asset and in the Inspector, go to Lighting > Cookies. In the 2D Atlas Size drop-down, select a larger cookie resolution.");
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
            }),
            MigrationStep.New(Version.AddedHDRenderPipelineGlobalSettings, (HDRenderPipelineAsset data) =>
            {
#if UNITY_EDITOR
                if (data == GraphicsSettings.defaultRenderPipeline)
                    HDRenderPipelineGlobalSettings.MigrateFromHDRPAsset(data);
#endif
#pragma warning disable 618 // Type or member is obsolete
                data.m_ObsoleteDefaultVolumeProfile = null;
                data.m_ObsoleteDefaultLookDevProfile = null;

                data.m_ObsoleteRenderPipelineResources = null;
                data.m_ObsoleteRenderPipelineRayTracingResources = null;

                data.m_ObsoleteBeforeTransparentCustomPostProcesses = null;
                data.m_ObsoleteBeforePostProcessCustomPostProcesses = null;
                data.m_ObsoleteAfterPostProcessCustomPostProcesses = null;
                data.m_ObsoleteBeforeTAACustomPostProcesses = null;
                data.m_ObsoleteDiffusionProfileSettingsList = null;

                data.m_RenderPipelineSettings.m_ObsoleteLightLayerName0 = null;
                data.m_RenderPipelineSettings.m_ObsoleteLightLayerName1 = null;
                data.m_RenderPipelineSettings.m_ObsoleteLightLayerName2 = null;
                data.m_RenderPipelineSettings.m_ObsoleteLightLayerName3 = null;
                data.m_RenderPipelineSettings.m_ObsoleteLightLayerName4 = null;
                data.m_RenderPipelineSettings.m_ObsoleteLightLayerName5 = null;
                data.m_RenderPipelineSettings.m_ObsoleteLightLayerName6 = null;
                data.m_RenderPipelineSettings.m_ObsoleteLightLayerName7 = null;

                data.m_RenderPipelineSettings.m_ObsoleteDecalLayerName0 = null;
                data.m_RenderPipelineSettings.m_ObsoleteDecalLayerName1 = null;
                data.m_RenderPipelineSettings.m_ObsoleteDecalLayerName2 = null;
                data.m_RenderPipelineSettings.m_ObsoleteDecalLayerName3 = null;
                data.m_RenderPipelineSettings.m_ObsoleteDecalLayerName4 = null;
                data.m_RenderPipelineSettings.m_ObsoleteDecalLayerName5 = null;
                data.m_RenderPipelineSettings.m_ObsoleteDecalLayerName6 = null;
                data.m_RenderPipelineSettings.m_ObsoleteDecalLayerName7 = null;
#pragma warning restore 618
            }),
            MigrationStep.New(Version.DecalSurfaceGradient, (HDRenderPipelineAsset data) =>
            {
                data.m_RenderPipelineSettings.supportSurfaceGradient = false;
            }),
#pragma warning disable 618 // Type or member is obsolete
            MigrationStep.New(Version.RemovalOfUpscaleFilter, (HDRenderPipelineAsset data) =>
            {
                if (data.m_RenderPipelineSettings.dynamicResolutionSettings.upsampleFilter == DynamicResUpscaleFilter.Bilinear)
                    data.m_RenderPipelineSettings.dynamicResolutionSettings.upsampleFilter = DynamicResUpscaleFilter.CatmullRom;
                if (data.m_RenderPipelineSettings.dynamicResolutionSettings.upsampleFilter == DynamicResUpscaleFilter.Lanczos)
                    data.m_RenderPipelineSettings.dynamicResolutionSettings.upsampleFilter = DynamicResUpscaleFilter.ContrastAdaptiveSharpen;
            }),
#pragma warning restore 618
            MigrationStep.New(Version.CombinedPlanarAndCubemapReflectionAtlases, (HDRenderPipelineAsset data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                ref var lightLoopSettings = ref data.m_RenderPipelineSettings.lightLoopSettings;

                CubeReflectionResolution cubeResolution = lightLoopSettings.reflectionCubemapSize;
                CubeReflectionResolution[] enumValues = (CubeReflectionResolution[])Enum.GetValues(typeof(CubeReflectionResolution));
                int index = Mathf.Max(Array.IndexOf(enumValues, cubeResolution), 0);
                CubeReflectionResolution[] cubeResolutions = new CubeReflectionResolution[]
                {
                    enumValues[Mathf.Min(index,     enumValues.Length - 1)],
                    enumValues[Mathf.Min(index + 1, enumValues.Length - 1)],
                    enumValues[Mathf.Min(index + 2, enumValues.Length - 1)]
                };
                data.m_RenderPipelineSettings.cubeReflectionResolution = new RenderPipelineSettings.ReflectionProbeResolutionScalableSetting(cubeResolutions, ScalableSettingSchemaId.With3Levels);

                int newCubeReflectionSize = ReflectionProbeTextureCache.GetReflectionProbeSizeInAtlas((int)lightLoopSettings.reflectionCubemapSize);
                int cubeReflectionAtlasArea = lightLoopSettings.reflectionProbeCacheSize * newCubeReflectionSize * newCubeReflectionSize;
                int planarReflectionAtlasArea = (int)lightLoopSettings.planarReflectionAtlasSize * (int)lightLoopSettings.planarReflectionAtlasSize;
                int totalNeededPixelCount = cubeReflectionAtlasArea + planarReflectionAtlasArea;

                // Set a default value for the reflection probe cache size in case we don't find a suitable atlas resolution (too many probes in upgraded atlas)
                lightLoopSettings.reflectionProbeTexCacheSize = ReflectionProbeTextureCacheResolution.Resolution16384x16384;

                // Find closes pixel count in the ReflectionProbeTextureCacheResolution enum:
                var availableResolutions = Enum.GetValues(typeof(ReflectionProbeTextureCacheResolution)).Cast<ReflectionProbeTextureCacheResolution>();
                foreach (ReflectionProbeTextureCacheResolution res in availableResolutions.OrderBy(r => (int)r & 0xFFFF))
                {
                    int height = (int)res & 0xFFFF;
                    int width = (int)res >> 16;
                    if (width == 0)
                        width = height;

                    int currentPixelCount = width * height;
                    if (currentPixelCount >= totalNeededPixelCount)
                    {
                        lightLoopSettings.reflectionProbeTexCacheSize = res;
                        break;
                    }
                }

                lightLoopSettings.maxCubeReflectionOnScreen = Mathf.Clamp(lightLoopSettings.maxEnvLightsOnScreen - lightLoopSettings.maxPlanarReflectionOnScreen, HDRenderPipeline.k_MaxCubeReflectionsOnScreen / 2, HDRenderPipeline.k_MaxCubeReflectionsOnScreen);
#pragma warning restore 618
            })
            );
        #endregion

        [SerializeField]
        Version m_Version = MigrationDescription.LastVersion<Version>();
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

#pragma warning disable 618 // Type or member is obsolete
        #region FrameSettings Moved
        [SerializeField]
        [FormerlySerializedAs("serializedFrameSettings"), FormerlySerializedAs("m_FrameSettings"), Obsolete("For data migration")]
        ObsoleteFrameSettings m_ObsoleteFrameSettings;
        [SerializeField]
        [FormerlySerializedAs("m_BakedOrCustomReflectionFrameSettings"), Obsolete("For data migration")]
        ObsoleteFrameSettings m_ObsoleteBakedOrCustomReflectionFrameSettings;
        [SerializeField]
        [FormerlySerializedAs("m_RealtimeReflectionFrameSettings"), Obsolete("For data migration")]
        ObsoleteFrameSettings m_ObsoleteRealtimeReflectionFrameSettings;
        #endregion

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
        internal int m_ObsoleteShaderVariantLogLevel;
        [SerializeField]
        [FormerlySerializedAs("m_LensAttenuation"), Obsolete("Moved from HDRPAsset to HDGlobal Settings")]
        internal LensAttenuationMode m_ObsoleteLensAttenuation;
        [SerializeField]
        [FormerlySerializedAs("diffusionProfileSettingsList"), Obsolete("Moved from HDRPAsset to HDGlobal Settings")]
        internal DiffusionProfileSettings[] m_ObsoleteDiffusionProfileSettingsList;
        #endregion
#pragma warning restore 618


#if UNITY_EDITOR
        const string packageName = "com.unity.render-pipelines.high-definition";

        [InitializeOnLoadMethod]
        static void  SubscribeToPacManEvents()
        {
            UnityEditor.PackageManager.Events.registeredPackages += RegisteredPackagesEventHandler;
        }

        static void RegisteredPackagesEventHandler(PackageRegistrationEventArgs packageRegistrationEventArgs)
        {
            foreach (var addedPackage in packageRegistrationEventArgs.added)
            {
                if (addedPackage.name == packageName)
                {
                    MigrateDueToHDRPPackageUpdate();
                    return;
                }
            }

            for (int i = 0; i <= packageRegistrationEventArgs.changedTo.Count; i++)
            {
                if (i >= packageRegistrationEventArgs.changedTo.Count)
                    continue;

                if (packageRegistrationEventArgs.changedTo[i].name == packageName)
                {
                    MigrateDueToHDRPPackageUpdate();
                    return;
                }
            }
        }

        static void MigrateDueToHDRPPackageUpdate()
        {
            // Migrate all HDRPAsset but also Resources assets and any HDRenderPipelineGlobalSettings (always migrated last)
            foreach (IMigratableAsset asset in CoreUtils.LoadAllAssets<IMigratableAsset>().OrderBy(asset => asset is HDRenderPipelineGlobalSettings ? 1 : 0))
                asset.Migrate();
        }

        bool IMigratableAsset.Migrate()
            => Migrate();

        bool IMigratableAsset.IsAtLastVersion()
            => m_Version == MigrationDescription.LastVersion<Version>();

        internal bool IsVersionBelowAddedHDRenderPipelineGlobalSettings()
            => m_Version < Version.AddedHDRenderPipelineGlobalSettings;
#endif

        bool Migrate()
            => k_Migration.Migrate(this);
    }
}
