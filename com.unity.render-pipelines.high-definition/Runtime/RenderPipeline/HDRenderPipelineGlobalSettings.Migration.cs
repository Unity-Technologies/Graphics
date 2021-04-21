using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDRenderPipelineGlobalSettings : IVersionable<HDRenderPipelineGlobalSettings.Version>, IMigratableAsset
    {
        enum Version
        {
            First,
            MigratedFromHDRPAssetOrCreated
        }

        [SerializeField]
        Version m_Version = MigrationDescription.LastVersion<Version>();
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

#if UNITY_EDITOR
        static readonly MigrationDescription<Version, HDRenderPipelineGlobalSettings> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.MigratedFromHDRPAssetOrCreated, (HDRenderPipelineGlobalSettings data) =>
            {
                // if possible we need to finish migration of hdrpAsset in order to grab value from it
                if (GraphicsSettings.defaultRenderPipeline is HDRenderPipelineAsset hdrpAsset && hdrpAsset.IsVersionBelowAddedHDRenderPipelineGlobalSettings())
                    (hdrpAsset as IMigratableAsset).Migrate();
            })
        );
        bool IMigratableAsset.Migrate()
            => k_Migration.Migrate(this);

        bool IMigratableAsset.IsAtLastVersion()
            => m_Version == MigrationDescription.LastVersion<Version>();
#endif
    }
}
