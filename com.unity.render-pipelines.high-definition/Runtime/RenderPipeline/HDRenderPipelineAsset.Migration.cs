using System;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class HDRenderPipelineAsset : IVersionable<HDRenderPipelineAsset.Version>
    {
        enum Version
        {
            None,
            First,
            UpgradeFrameSettingsToStruct,
            AddAfterPostProcessFrameSetting,
            AddFrameSettingSpecularLighting = 5
        }

        static readonly MigrationDescription<Version, HDRenderPipelineAsset> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.UpgradeFrameSettingsToStruct, (HDRenderPipelineAsset data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                FrameSettingsOverrideMask unusedMaskForDefault = new FrameSettingsOverrideMask();
                if (data.m_ObsoleteFrameSettings != null)
                    FrameSettings.MigrateFromClassVersion(ref data.m_ObsoleteFrameSettings, ref data.m_RenderingPathDefaultCameraFrameSettings, ref unusedMaskForDefault);
                if (data.m_ObsoleteBakedOrCustomReflectionFrameSettings != null)
                    FrameSettings.MigrateFromClassVersion(ref data.m_ObsoleteBakedOrCustomReflectionFrameSettings, ref data.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings, ref unusedMaskForDefault);
                if (data.m_ObsoleteRealtimeReflectionFrameSettings != null)
                    FrameSettings.MigrateFromClassVersion(ref data.m_ObsoleteRealtimeReflectionFrameSettings, ref data.m_RenderingPathDefaultRealtimeReflectionFrameSettings, ref unusedMaskForDefault);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.AddAfterPostProcessFrameSetting, (HDRenderPipelineAsset data) =>
            {
                FrameSettings.MigrateToAfterPostprocess(ref data.m_RenderingPathDefaultCameraFrameSettings);
            }),
            MigrationStep.New(Version.AddFrameSettingSpecularLighting, (HDRenderPipelineAsset data) =>
            {
                FrameSettings.MigrateToSpecularLighting(ref data.m_RenderingPathDefaultCameraFrameSettings);
                FrameSettings.MigrateToSpecularLighting(ref data.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings);
                FrameSettings.MigrateToSpecularLighting(ref data.m_RenderingPathDefaultRealtimeReflectionFrameSettings);
            })
        );

        [SerializeField]
        Version m_Version;
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        void Awake() => k_Migration.Migrate(this);

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
#pragma warning restore 618
    }
}
