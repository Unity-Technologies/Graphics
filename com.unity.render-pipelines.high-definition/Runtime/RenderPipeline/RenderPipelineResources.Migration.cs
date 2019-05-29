using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class RenderPipelineResources : ScriptableObject, IVersionable<RenderPipelineResources.Version>
    {
        enum Version
        {
            None,
            First,
            RemovedEditorOnlyResources = 4
        }

        [HideInInspector, SerializeField, FormerlySerializedAs("version")]
        Version m_Version = Version.First;  //keep former creation affectation

        Version IVersionable<Version>.version
        {
            get => m_Version;
            set => m_Version = value;
        }

#if UNITY_EDITOR //formerly migration were only handled in editor for this asset
        static readonly MigrationDescription<Version, RenderPipelineResources> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.RemovedEditorOnlyResources, (RenderPipelineResources i) =>
            {
                //force full reimport to remove moved resources
                i.materials = null;
                i.shaderGraphs = null;
                i.textures = null;
                i.shaders = null;
                ResourceReloader.ReloadAllNullIn(i, HDUtils.GetHDRenderPipelinePath());
            })
        );

        public void UpgradeIfNeeded() => k_Migration.Migrate(this);
#endif
    }
}
