using System;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class HDAdditionalCameraData : IVersionable<HDAdditionalCameraData.Version>
    {
        protected enum Version
        {
            None,
            First,
            SeparatePassThrough,
            UpgradingFrameSettingsToStruct,
            AddAfterPostProcessFrameSetting,
            AddFrameSettingSpecularLighting
        }

        [SerializeField, FormerlySerializedAs("version")]
        Version m_Version;

        protected static readonly MigrationDescription<Version, HDAdditionalCameraData> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.SeparatePassThrough, (HDAdditionalCameraData data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                switch ((int)data.m_ObsoleteRenderingPath)
#pragma warning restore 618
                {
                    case 0: //former RenderingPath.UseGraphicsSettings
                        data.fullscreenPassthrough = false;
                        data.customRenderingSettings = false;
                        break;
                    case 1: //former RenderingPath.Custom
                        data.fullscreenPassthrough = false;
                        data.customRenderingSettings = true;
                        break;
                    case 2: //former RenderingPath.FullscreenPassthrough
                        data.fullscreenPassthrough = true;
                        data.customRenderingSettings = false;
                        break;
                }
            }),
            MigrationStep.New(Version.UpgradingFrameSettingsToStruct, (HDAdditionalCameraData data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                if (data.m_ObsoleteFrameSettings != null)
                    FrameSettings.MigrateFromClassVersion(ref data.m_ObsoleteFrameSettings, ref data.renderingPathCustomFrameSettings, ref data.renderingPathCustomFrameSettingsOverrideMask);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.AddAfterPostProcessFrameSetting, (HDAdditionalCameraData data) =>
            {
                FrameSettings.MigrateToAfterPostprocess(ref data.renderingPathCustomFrameSettings);
            }),
            MigrationStep.New(Version.AddFrameSettingSpecularLighting, (HDAdditionalCameraData data) =>
                FrameSettings.MigrateToSpecularLighting(ref data.renderingPathCustomFrameSettings)
            )
        );

        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        void Awake() => k_Migration.Migrate(this);

#pragma warning disable 649 // Field never assigned
        [SerializeField, FormerlySerializedAs("renderingPath"), Obsolete("For Data Migration")]
        int m_ObsoleteRenderingPath;
        [SerializeField]
        [FormerlySerializedAs("serializedFrameSettings"), FormerlySerializedAs("m_FrameSettings")]
#pragma warning disable 618 // Type or member is obsolete
        ObsoleteFrameSettings m_ObsoleteFrameSettings;
#pragma warning restore 618
#pragma warning restore 649
    }
}
