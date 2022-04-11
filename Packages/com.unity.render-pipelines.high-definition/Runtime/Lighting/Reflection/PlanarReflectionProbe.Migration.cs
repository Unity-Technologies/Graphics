using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public sealed partial class PlanarReflectionProbe : IVersionable<PlanarReflectionProbe.PlanarProbeVersion>
    {
        enum PlanarProbeVersion
        {
            Initial,
            First = 2,
            CaptureSettings,
            ProbeSettings,
            SeparatePassThrough,
            UpgradeFrameSettingsToStruct,
            PlanarResolutionScalability
        }

        [SerializeField, FormerlySerializedAs("version"), FormerlySerializedAs("m_Version")]
        int m_PlanarProbeVersion;
        PlanarProbeVersion IVersionable<PlanarProbeVersion>.version { get => (PlanarProbeVersion)m_PlanarProbeVersion; set => m_PlanarProbeVersion = (int)value; }

        static readonly MigrationDescription<PlanarProbeVersion, PlanarReflectionProbe> k_PlanarProbeMigration = MigrationDescription.New(
            MigrationStep.New(PlanarProbeVersion.CaptureSettings, (PlanarReflectionProbe p) =>
            {
#pragma warning disable 618, 612
                if (p.m_ObsoleteCaptureSettings == null)
                    p.m_ObsoleteCaptureSettings = new ObsoleteCaptureSettings();
                if (p.m_ObsoleteOverrideFieldOfView)
                    p.m_ObsoleteCaptureSettings.overrides |= ObsoleteCaptureSettingsOverrides.FieldOfview;
                p.m_ObsoleteCaptureSettings.fieldOfView = p.m_ObsoleteFieldOfViewOverride;
                p.m_ObsoleteCaptureSettings.nearClipPlane = p.m_ObsoleteCaptureNearPlane;
                p.m_ObsoleteCaptureSettings.farClipPlane = p.m_ObsoleteCaptureFarPlane;
#pragma warning restore 618, 612
            }),
            MigrationStep.New(PlanarProbeVersion.ProbeSettings, (PlanarReflectionProbe p) =>
            {
                k_Migration.ExecuteStep(p, Version.ProbeSettings);

                // Migrate mirror position
                // Previously, the mirror as at the influence position and face the Y axis.
                // Now, the mirror is defined in proxy space and faces the Z axis.

                var mirrorPositionWS = p.transform.position;
                // set the transform position to the influence position world space
#pragma warning disable 618
                var mat = Matrix4x4.TRS(p.transform.position, p.transform.rotation, Vector3.one);
                p.transform.position = mat.MultiplyPoint(p.influenceVolume.obsoleteOffset);
#pragma warning restore 618
                var mirrorRotationWS = p.transform.rotation * Quaternion.Euler(-90, 0, 0);
                var worldToProxy = p.proxyToWorld.inverse;
                var mirrorPositionPS = worldToProxy.MultiplyPoint(mirrorPositionWS);
                var mirrorRotationPS = worldToProxy.rotation * mirrorRotationWS;
                p.m_ProbeSettings.proxySettings.mirrorPositionProxySpace = mirrorPositionPS;
                p.m_ProbeSettings.proxySettings.mirrorRotationProxySpace = mirrorRotationPS;
                p.m_LocalReferencePosition = Quaternion.Euler(-90, 0, 0) * -p.m_LocalReferencePosition;
            }),
            MigrationStep.New(PlanarProbeVersion.SeparatePassThrough, (PlanarReflectionProbe t) => k_Migration.ExecuteStep(t, Version.SeparatePassThrough)),
            MigrationStep.New(PlanarProbeVersion.UpgradeFrameSettingsToStruct, (PlanarReflectionProbe t) => k_Migration.ExecuteStep(t, Version.UpgradeFrameSettingsToStruct)),
            MigrationStep.New(PlanarProbeVersion.PlanarResolutionScalability, (PlanarReflectionProbe p) =>
            {
                k_Migration.ExecuteStep(p, Version.PlanarResolutionScalability);
                // Previously, we would only specify the value directly on the probe (so we set use override to true)
                p.m_ProbeSettings.resolutionScalable.useOverride = true;
                if (p.m_ProbeSettings.resolution != 0)
                {
                    p.m_ProbeSettings.resolutionScalable.@override = p.m_ProbeSettings.resolution;
                }
                else
                {
                    p.m_ProbeSettings.resolutionScalable.@override = PlanarReflectionAtlasResolution.Resolution512;
                }
            })
        );

        // Obsolete Properties
#pragma warning disable 649
        [SerializeField, FormerlySerializedAs("m_CaptureNearPlane"), Obsolete("For data migration")]
        float m_ObsoleteCaptureNearPlane = ObsoleteCaptureSettings.@default.nearClipPlane;
        [SerializeField, FormerlySerializedAs("m_CaptureFarPlane"), Obsolete("For data migration")]
        float m_ObsoleteCaptureFarPlane = ObsoleteCaptureSettings.@default.farClipPlane;

        [SerializeField, FormerlySerializedAs("m_OverrideFieldOfView"), Obsolete("For data migration")]
        bool m_ObsoleteOverrideFieldOfView;
        [SerializeField, FormerlySerializedAs("m_FieldOfViewOverride"), Obsolete("For data migration")]
        float m_ObsoleteFieldOfViewOverride = ObsoleteCaptureSettings.@default.fieldOfView;
#pragma warning restore 649
    }
}
