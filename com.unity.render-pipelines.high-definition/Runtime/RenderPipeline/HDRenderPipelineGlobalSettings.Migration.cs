using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDRenderPipelineGlobalSettings : IVersionable<HDRenderPipelineGlobalSettings.Version>, IMigratableAsset
    {
        enum Version
        {
            First,
            MigratedFromHDRPAssetOrCreated,
            UpdateMSAA,
        }

        // Sadly we cannot create asset at last version
        // We must always check if they need to be created from HDRPAsset
        // So we must always create them at first version and run the whole update process.
        // Hopefully, creation of thoses asset are pretty rare.
        // But this also means when adding a new feature that require a migration step, to be sure to comply with former patterns.
        [SerializeField]
        Version m_Version = Version.First;
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

#if UNITY_EDITOR
        static readonly MigrationDescription<Version, HDRenderPipelineGlobalSettings> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.MigratedFromHDRPAssetOrCreated, (HDRenderPipelineGlobalSettings data) =>
            {
                // if possible we need to finish migration of hdrpAsset in order to grab value from it
                if (GraphicsSettings.defaultRenderPipeline is HDRenderPipelineAsset hdrpAsset && hdrpAsset.IsVersionBelowAddedHDRenderPipelineGlobalSettings())
                    (hdrpAsset as IMigratableAsset).Migrate();
            }),
            MigrationStep.New(Version.UpdateMSAA, (HDRenderPipelineGlobalSettings data) =>
            {
                FrameSettingsOverrideMask unusedMaskForDefault = new FrameSettingsOverrideMask();
                FrameSettings.MigrateMSAA(ref data.m_RenderingPathDefaultCameraFrameSettings, ref unusedMaskForDefault);
                FrameSettings.MigrateMSAA(ref data.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings, ref unusedMaskForDefault);
                FrameSettings.MigrateMSAA(ref data.m_RenderingPathDefaultRealtimeReflectionFrameSettings, ref unusedMaskForDefault);
            })
        );
        bool IMigratableAsset.Migrate()
            => k_Migration.Migrate(this);

        bool IMigratableAsset.IsAtLastVersion()
            => m_Version == MigrationDescription.LastVersion<Version>();

        internal static void MigrateFromHDRPAsset(HDRenderPipelineAsset oldAsset)
        {
            if (instance != null
                && !instance.Equals(null)
                && instance.m_Version != Version.First)
                return;

            //1. Create the instance or load current one if at first version
            HDRenderPipelineGlobalSettings assetToUpgrade;

            if (instance == null || instance.Equals(null))
            {
                assetToUpgrade = Create($"Assets/{HDProjectSettings.projectSettingsFolderPath}/HDRenderPipelineGlobalSettings.asset");
                UpdateGraphicsSettings(assetToUpgrade);
            }
            else
                assetToUpgrade = instance;

            Debug.Assert(assetToUpgrade);

            //2. Migrate obsolete assets (version DefaultSettingsAsAnAsset)
#pragma warning disable 618 // Type or member is obsolete
            assetToUpgrade.volumeProfile        = oldAsset.m_ObsoleteDefaultVolumeProfile;
            assetToUpgrade.lookDevVolumeProfile = oldAsset.m_ObsoleteDefaultLookDevProfile;

            assetToUpgrade.m_RenderingPathDefaultCameraFrameSettings                  = oldAsset.m_ObsoleteFrameSettingsMovedToDefaultSettings;
            assetToUpgrade.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings = oldAsset.m_ObsoleteBakedOrCustomReflectionFrameSettingsMovedToDefaultSettings;
            assetToUpgrade.m_RenderingPathDefaultRealtimeReflectionFrameSettings      = oldAsset.m_ObsoleteRealtimeReflectionFrameSettingsMovedToDefaultSettings;

            assetToUpgrade.m_RenderPipelineResources           = oldAsset.m_ObsoleteRenderPipelineResources;
            assetToUpgrade.m_RenderPipelineRayTracingResources = oldAsset.m_ObsoleteRenderPipelineRayTracingResources;

            assetToUpgrade.beforeTransparentCustomPostProcesses.AddRange(oldAsset.m_ObsoleteBeforeTransparentCustomPostProcesses);
            assetToUpgrade.beforePostProcessCustomPostProcesses.AddRange(oldAsset.m_ObsoleteBeforePostProcessCustomPostProcesses);
            assetToUpgrade.afterPostProcessCustomPostProcesses.AddRange(oldAsset.m_ObsoleteAfterPostProcessCustomPostProcesses);
            assetToUpgrade.beforeTAACustomPostProcesses.AddRange(oldAsset.m_ObsoleteBeforeTAACustomPostProcesses);

            assetToUpgrade.lightLayerName0 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName0;
            assetToUpgrade.lightLayerName1 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName1;
            assetToUpgrade.lightLayerName2 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName2;
            assetToUpgrade.lightLayerName3 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName3;
            assetToUpgrade.lightLayerName4 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName4;
            assetToUpgrade.lightLayerName5 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName5;
            assetToUpgrade.lightLayerName6 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName6;
            assetToUpgrade.lightLayerName7 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName7;

            // Decal layer names were added in 2021 cycle
            if (oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName0 != null)
            {
                assetToUpgrade.decalLayerName0 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName0;
                assetToUpgrade.decalLayerName1 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName1;
                assetToUpgrade.decalLayerName2 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName2;
                assetToUpgrade.decalLayerName3 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName3;
                assetToUpgrade.decalLayerName4 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName4;
                assetToUpgrade.decalLayerName5 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName5;
                assetToUpgrade.decalLayerName6 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName6;
                assetToUpgrade.decalLayerName7 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName7;
            }

            assetToUpgrade.shaderVariantLogLevel = oldAsset.m_ObsoleteShaderVariantLogLevel;
            assetToUpgrade.lensAttenuationMode = oldAsset.m_ObsoleteLensAttenuation;

            // we need to make sure the old diffusion profile had time to upgrade before moving it away
            if (oldAsset.diffusionProfileSettings != null)
            {
                oldAsset.diffusionProfileSettings.TryToUpgrade();
            }

            int oldSize = oldAsset.m_ObsoleteDiffusionProfileSettingsList?.Length ?? 0;
            System.Array.Resize(ref assetToUpgrade.diffusionProfileSettingsList, oldSize);
            for (int i = 0; i < oldSize; ++i)
                assetToUpgrade.diffusionProfileSettingsList[i] = oldAsset.m_ObsoleteDiffusionProfileSettingsList[i];
#pragma warning restore 618

            //3. Set version to next & Launch remaining of migration
            assetToUpgrade.m_Version = Version.MigratedFromHDRPAssetOrCreated;
            (assetToUpgrade as IMigratableAsset).Migrate();
        }

        void OnEnable()
            => (this as IMigratableAsset).Migrate();
#endif
    }
}
