using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    using NewFlipYMode = FlipYMode;
    using NewClearColorMode = ClearColorMode;
    using NewAntialiasingMode = AntialiasingMode;
    using NewSMAAQualityLevel = SMAAQualityLevel;
    using NewTAAQualityLevel = TAAQualityLevel;

    public partial class HDAdditionalCameraData : IVersionable<HDAdditionalCameraData.Version>
    {
        /// <summary>
        /// Define migration versions of the HDAdditionalCameraData
        /// </summary>
        protected enum Version
        {
            /// <summary>Version Step.</summary>
            None,
            /// <summary>Version Step.</summary>
            First,
            /// <summary>Version Step.</summary>
            SeparatePassThrough,
            /// <summary>Version Step.</summary>
            UpgradingFrameSettingsToStruct,
            /// <summary>Version Step.</summary>
            AddAfterPostProcessFrameSetting,
            /// <summary>Version Step.</summary>
            AddFrameSettingSpecularLighting, // Not used anymore
            /// <summary>Version Step.</summary>
            AddReflectionSettings,
            /// <summary>Version Step.</summary>
            AddCustomPostprocessAndCustomPass,
            /// <summary>Version Step.</summary>
            UseExtension,
        }

        [SerializeField, FormerlySerializedAs("version")]
        Version m_Version = MigrationDescription.LastVersion<Version>();

        static readonly MigrationDescription<Version, HDAdditionalCameraData> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.SeparatePassThrough, (HDAdditionalCameraData data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                switch ((int)data.m_ObsoleteRenderingPath)
#pragma warning restore 618
                {
                    case 0: //former RenderingPath.UseGraphicsSettings
                        data.m_FullscreenPassthrough = false;
                        data.m_CustomRenderingSettings = false;
                        break;
                    case 1: //former RenderingPath.Custom
                        data.m_FullscreenPassthrough = false;
                        data.m_CustomRenderingSettings = true;
                        break;
                    case 2: //former RenderingPath.FullscreenPassthrough
                        data.m_FullscreenPassthrough = true;
                        data.m_CustomRenderingSettings = false;
                        break;
                }
            }),
            MigrationStep.New(Version.UpgradingFrameSettingsToStruct, (HDAdditionalCameraData data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                if (data.m_ObsoleteFrameSettings != null)
                    FrameSettings.MigrateFromClassVersion(ref data.m_ObsoleteFrameSettings, ref data.m_RenderingPathCustomFrameSettings, ref data.m_RenderingPathCustomFrameSettingsOverrideMask);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.AddAfterPostProcessFrameSetting, (HDAdditionalCameraData data) =>
                FrameSettings.MigrateToAfterPostprocess(ref data.m_RenderingPathCustomFrameSettings)
            ),
            MigrationStep.New(Version.AddReflectionSettings, (HDAdditionalCameraData data) =>
                FrameSettings.MigrateToDefaultReflectionSettings(ref data.m_RenderingPathCustomFrameSettings)
            ),
            MigrationStep.New(Version.AddCustomPostprocessAndCustomPass, (HDAdditionalCameraData data) =>
            {
                FrameSettings.MigrateToCustomPostprocessAndCustomPass(ref data.m_RenderingPathCustomFrameSettings);
            }),
            MigrationStep.New(Version.UseExtension, (HDAdditionalCameraData data) =>
            {
                Camera cam = data.GetComponent<Camera>();
                if (!cam.HasExtension<HDCameraExtension>())
                    cam.CreateExtension<HDCameraExtension>();
                HDCameraExtension extension = cam.SwitchActiveExtensionTo<HDCameraExtension>();

#pragma warning disable 618 // Type or member is obsolete
                extension.state.allowDynamicResolution = data.allowDynamicResolution;
                extension.state.antialiasing = (NewAntialiasingMode)data.antialiasing;
                extension.state.backgroundColorHDR = data.backgroundColorHDR;
                extension.state.clearColorMode = (NewClearColorMode)data.clearColorMode;
                extension.state.clearDepth = data.clearDepth;
                extension.state.customRenderingSettings = data.customRenderingSettings;
                extension.state.defaultFrameSettings = data.defaultFrameSettings;
                extension.state.dithering = data.dithering;
                extension.state.exposureTarget = data.exposureTarget;
                extension.state.flipYMode = (NewFlipYMode)data.flipYMode;
                extension.state.fullscreenPassthrough = data.fullscreenPassthrough;
                extension.state.hasPersistentHistory = data.hasPersistentHistory;
                extension.state.invertFaceCulling = data.invertFaceCulling;
                extension.state.physicalParameters = data.physicalParameters;
                extension.state.probeLayerMask = data.probeLayerMask;
                extension.state.renderingPathCustomFrameSettings = data.renderingPathCustomFrameSettings;
                extension.state.renderingPathCustomFrameSettingsOverrideMask = data.renderingPathCustomFrameSettingsOverrideMask;
                extension.state.SMAAQuality = (NewSMAAQualityLevel)data.SMAAQuality;
                extension.state.stopNaNs = data.stopNaNs;
                extension.state.taaAntiFlicker = data.taaAntiFlicker;
                extension.state.taaAntiHistoryRinging = data.taaAntiHistoryRinging;
                extension.state.taaHistorySharpening = data.taaHistorySharpening;
                extension.state.taaMotionVectorRejection = data.taaMotionVectorRejection;
                extension.state.TAAQuality = (NewTAAQualityLevel)data.TAAQuality;
                extension.state.taaSharpenStrength = data.taaSharpenStrength;
                extension.state.volumeAnchorOverride = data.volumeAnchorOverride;
                extension.state.volumeLayerMask = data.volumeLayerMask;
                extension.state.xrRendering = data.xrRendering;
#pragma warning restore 618 // Type or member is obsolete
            })
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
#pragma warning restore 618 // Type or member is obsolete
        
#pragma warning disable 618 // Type or member is obsolete
        [SerializeField, FormerlySerializedAs("clearColorMode")] ClearColorMode m_ClearColorMode;
        [SerializeField, FormerlySerializedAs("flipYMode")] FlipYMode m_FlipYMode;
        [SerializeField, FormerlySerializedAs("antialiasing")] AntialiasingMode m_Antialiasing;
        [SerializeField, FormerlySerializedAs("SMAAQuality")] SMAAQualityLevel m_SmaaQuality;
        [SerializeField, FormerlySerializedAs("TAAQuality")] TAAQualityLevel m_TAAQuality;
#pragma warning restore 618 // Type or member is obsolete
#pragma warning disable 414 // Type or member assigned but never used
        [SerializeField, FormerlySerializedAs("fullscreenPassthrough")] bool m_FullscreenPassthrough;
        [SerializeField, FormerlySerializedAs("customRenderingSettings")] bool m_CustomRenderingSettings;
#pragma warning restore 414 // Type or member assigned but never used
        [SerializeField, FormerlySerializedAs("backgroundColorHDR")] Color m_BackgroundColorHDR;
        [SerializeField, FormerlySerializedAs("clearDepth")] bool m_ClearDepth;
        [SerializeField, FormerlySerializedAs("volumeLayerMask")] LayerMask m_VolumeLayerMask;
        [SerializeField, FormerlySerializedAs("volumeAnchorOverride")] Transform m_VolumeAnchorOverride;
        [SerializeField, FormerlySerializedAs("dithering")] bool m_Dithering;
        [SerializeField, FormerlySerializedAs("stopNaNs")] bool m_StopNaNs;
        [SerializeField, FormerlySerializedAs("taaSharpenStrength")] float m_TaaSharpenStrength;
        [SerializeField, FormerlySerializedAs("taaHistorySharpening")] float m_TaaHistorySharpening;
        [SerializeField, FormerlySerializedAs("taaAntiFlicker")] float m_TaaAntiFlicker;
        [SerializeField, FormerlySerializedAs("taaMotionVectorRejection")] float m_TaaMotionVectorRejection;
        [SerializeField, FormerlySerializedAs("taaAntiHistoryRinging")] bool m_TaaAntiHistoryRinging;
        [SerializeField, FormerlySerializedAs("physicalParameters")] HDPhysicalCamera m_PhysicalParameters;
        [SerializeField, FormerlySerializedAs("xrRendering")] bool m_XrRendering;
        [SerializeField, FormerlySerializedAs("allowDynamicResolution")] bool m_AllowDynamicResolution;
        [SerializeField, FormerlySerializedAs("invertFaceCulling")] bool m_InvertFaceCulling;
        [SerializeField, FormerlySerializedAs("probeLayerMask")] LayerMask m_ProbeLayerMask;
        [SerializeField, FormerlySerializedAs("hasPersistentHistory")] bool m_HasPersistentHistory;
        [SerializeField, FormerlySerializedAs("exposureTarget")] GameObject m_ExposureTarget;
        [SerializeField, FormerlySerializedAs("renderingPathCustomFrameSettings")] FrameSettings m_RenderingPathCustomFrameSettings;
        [SerializeField, FormerlySerializedAs("renderingPathCustomFrameSettingsOverrideMask")] FrameSettingsOverrideMask m_RenderingPathCustomFrameSettingsOverrideMask;
        [SerializeField, FormerlySerializedAs("defaultFrameSettings")] FrameSettingsRenderType m_DefaultFrameSettings;
#pragma warning restore 649 // Field never assigned
    }
}
