using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDRenderPipelineGlobalSettings : IVersionable<HDRenderPipelineGlobalSettings.Version>
    {
        enum Version
        {
            First,
            UpdateMSAA,
        }

        static readonly MigrationDescription<Version, HDRenderPipelineGlobalSettings> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.UpdateMSAA, (HDRenderPipelineGlobalSettings data) =>
            {
                FrameSettingsOverrideMask unusedMaskForDefault = new FrameSettingsOverrideMask();
                FrameSettings.MigrateMSAA(ref data.m_RenderingPathDefaultCameraFrameSettings, ref unusedMaskForDefault);
                FrameSettings.MigrateMSAA(ref data.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings, ref unusedMaskForDefault);
                FrameSettings.MigrateMSAA(ref data.m_RenderingPathDefaultRealtimeReflectionFrameSettings, ref unusedMaskForDefault);
            })
        );

        [SerializeField]
        Version m_Version = MigrationDescription.LastVersion<Version>();
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        void OnEnable() => k_Migration.Migrate(this);
    }
}
