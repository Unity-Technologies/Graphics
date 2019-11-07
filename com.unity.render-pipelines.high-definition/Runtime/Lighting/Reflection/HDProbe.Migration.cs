using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public abstract partial class HDProbe : IVersionable<HDProbe.Version>
    {
        protected enum Version
        {
            Initial,
            ProbeSettings,
            SeparatePassThrough,
            UpgradeFrameSettingsToStruct,
            AddFrameSettingSpecularLighting, // Not use anymore
            AddReflectionFrameSetting,
            AddFrameSettingDirectSpecularLighting,
        }

        protected static readonly MigrationDescription<Version, HDProbe> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.ProbeSettings, (HDProbe p) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                p.m_ProbeSettings.proxySettings.useInfluenceVolumeAsProxyVolume = !p.m_ObsoleteInfiniteProjection;
                p.m_ProbeSettings.influence = new InfluenceVolume();
                if (p.m_ObsoleteInfluenceVolume != null)
                    // We must create a copy here, otherwise the serialization will write the same memory destination
                    //   for both property p.m_ObsoleteInfluenceVolume and p.m_ProbeSettings.influence
                    p.m_ObsoleteInfluenceVolume.CopyTo(p.m_ProbeSettings.influence);

                p.m_ProbeSettings.cameraSettings.m_ObsoleteFrameSettings = p.m_ObsoleteFrameSettings;
                p.m_ProbeSettings.lighting.multiplier = p.m_ObsoleteMultiplier;
                p.m_ProbeSettings.lighting.weight = p.m_ObsoleteWeight;
                p.m_ProbeSettings.lighting.lightLayer = p.m_ObsoleteLightLayers;
                p.m_ProbeSettings.mode = p.m_ObsoleteMode;

                // Migrating Capture Settings
                p.m_ProbeSettings.cameraSettings.bufferClearing.clearColorMode = p.m_ObsoleteCaptureSettings.clearColorMode;
                p.m_ProbeSettings.cameraSettings.bufferClearing.backgroundColorHDR = p.m_ObsoleteCaptureSettings.backgroundColorHDR;
                p.m_ProbeSettings.cameraSettings.bufferClearing.clearDepth = p.m_ObsoleteCaptureSettings.clearDepth;
                p.m_ProbeSettings.cameraSettings.culling.cullingMask = p.m_ObsoleteCaptureSettings.cullingMask;
                p.m_ProbeSettings.cameraSettings.culling.useOcclusionCulling = p.m_ObsoleteCaptureSettings.useOcclusionCulling;
                p.m_ProbeSettings.cameraSettings.frustum.nearClipPlane = p.m_ObsoleteCaptureSettings.nearClipPlane;
                p.m_ProbeSettings.cameraSettings.frustum.farClipPlane = p.m_ObsoleteCaptureSettings.farClipPlane;
                p.m_ProbeSettings.cameraSettings.volumes.layerMask = p.m_ObsoleteCaptureSettings.volumeLayerMask;
                p.m_ProbeSettings.cameraSettings.volumes.anchorOverride = p.m_ObsoleteCaptureSettings.volumeAnchorOverride;
                p.m_ProbeSettings.cameraSettings.frustum.fieldOfView = p.m_ObsoleteCaptureSettings.fieldOfView;
                p.m_ProbeSettings.cameraSettings.m_ObsoleteRenderingPath = p.m_ObsoleteCaptureSettings.renderingPath;
#pragma warning restore 618
            }),
            MigrationStep.New(Version.SeparatePassThrough, (HDProbe p) =>
            {
                //note: 0 is UseGraphicsSettings
                const int k_RenderingPathCustom = 1;
                //note: 2 is PassThrough which was never supported on probes
#pragma warning disable 618 // Type or member is obsolete
                p.m_ProbeSettings.cameraSettings.customRenderingSettings = p.m_ProbeSettings.cameraSettings.m_ObsoleteRenderingPath == k_RenderingPathCustom;
#pragma warning restore 618
            }),
            MigrationStep.New(Version.UpgradeFrameSettingsToStruct, (HDProbe data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                if (data.m_ObsoleteFrameSettings != null)
                    FrameSettings.MigrateFromClassVersion(ref data.m_ProbeSettings.cameraSettings.m_ObsoleteFrameSettings, ref data.m_ProbeSettings.cameraSettings.renderingPathCustomFrameSettings, ref data.m_ProbeSettings.cameraSettings.renderingPathCustomFrameSettingsOverrideMask);
#pragma warning restore 618
            }),
            MigrationStep.New(Version.AddReflectionFrameSetting, (HDProbe data) =>
            {
                FrameSettings.MigrateToNoReflectionSettings(ref data.m_ProbeSettings.cameraSettings.renderingPathCustomFrameSettings);
            }),
            MigrationStep.New(Version.AddFrameSettingDirectSpecularLighting, (HDProbe data) =>
            {
                FrameSettings.MigrateToNoDirectSpecularLighting(ref data.m_ProbeSettings.cameraSettings.renderingPathCustomFrameSettings);
            })
        );

        [SerializeField]
        Version m_HDProbeVersion;
        Version IVersionable<Version>.version { get => m_HDProbeVersion; set => m_HDProbeVersion = value; }

        // Legacy fields for HDProbe
        [SerializeField, FormerlySerializedAs("m_InfiniteProjection"), Obsolete("For Data Migration")]
        protected bool m_ObsoleteInfiniteProjection = true;

        [SerializeField, FormerlySerializedAs("m_InfluenceVolume"), Obsolete("For Data Migration")]
        protected InfluenceVolume m_ObsoleteInfluenceVolume;

#pragma warning disable 618 // Type or member is obsolete
        [SerializeField, FormerlySerializedAs("m_FrameSettings"), Obsolete("For Data Migration")]
        ObsoleteFrameSettings m_ObsoleteFrameSettings = null;
#pragma warning restore 618

        [SerializeField, FormerlySerializedAs("m_Multiplier"), FormerlySerializedAs("dimmer")]
        [FormerlySerializedAs("m_Dimmer"), FormerlySerializedAs("multiplier"), Obsolete("For Data Migration")]
        protected float m_ObsoleteMultiplier = 1.0f;
        [SerializeField, FormerlySerializedAs("m_Weight"), FormerlySerializedAs("weight")]
        [Obsolete("For Data Migration")]
        [Range(0.0f, 1.0f)]
        protected float m_ObsoleteWeight = 1.0f;

        [SerializeField, FormerlySerializedAs("m_Mode"), Obsolete("For Data Migration")]
        protected ProbeSettings.Mode m_ObsoleteMode = ProbeSettings.Mode.Baked;

        [SerializeField, FormerlySerializedAs("lightLayers"), Obsolete("For Data Migration")]
        LightLayerEnum m_ObsoleteLightLayers = LightLayerEnum.LightLayerDefault;

        [SerializeField, FormerlySerializedAs("m_CaptureSettings"), Obsolete("For Data Migration")]
        protected ObsoleteCaptureSettings m_ObsoleteCaptureSettings;
    }
}
