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
        bool IMigratableAsset.Migrate() { return false; } // must call migration once there will be migration step availables after the MigratedFromHDRPAssetOrCreated

        bool IMigratableAsset.IsAtLastVersion()
            => m_Version == MigrationDescription.LastVersion<Version>();
#endif
    }
}
