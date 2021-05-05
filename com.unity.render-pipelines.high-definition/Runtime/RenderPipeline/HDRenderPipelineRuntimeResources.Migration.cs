using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDRenderPipelineRuntimeResources : IVersionable<HDRenderPipelineRuntimeResources.Version>, IMigratableAsset
    {
        enum Version
        {
            None,
            First,
            RemovedEditorOnlyResources = 4
        }

        [HideInInspector, SerializeField, FormerlySerializedAs("version")]
        Version m_Version = MigrationDescription.LastVersion<Version>();

        Version IVersionable<Version>.version
        {
            get => m_Version;
            set => m_Version = value;
        }

#if UNITY_EDITOR //formerly migration were only handled in editor for this asset
        static readonly MigrationDescription<Version, HDRenderPipelineRuntimeResources> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.RemovedEditorOnlyResources, (HDRenderPipelineRuntimeResources i) =>
            {
                //force full reimport to remove moved resources
                i.materials = null;
                i.shaderGraphs = null;
                i.textures = null;
                i.shaders = null;
                ResourceReloader.ReloadAllNullIn(i, HDUtils.GetHDRenderPipelinePath());
            })
        );

        bool IMigratableAsset.Migrate() => k_Migration.Migrate(this);

        bool IMigratableAsset.IsAtLastVersion()
            => m_Version == MigrationDescription.LastVersion<Version>();
#endif
    }
}
