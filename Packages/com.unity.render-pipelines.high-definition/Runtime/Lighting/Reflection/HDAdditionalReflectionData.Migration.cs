using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public sealed partial class HDAdditionalReflectionData : IVersionable<HDAdditionalReflectionData.ReflectionProbeVersion>
    {
        enum ReflectionProbeVersion
        {
            First,
            RemoveUsageOfLegacyProbeParamsForStocking,
            HDProbeChild,
            UseInfluenceVolume,
            MergeEditors,
            AddCaptureSettingsAndFrameSettings,
            ModeAndTextures,
            ProbeSettings,
            SeparatePassThrough,
            UpgradeFrameSettingsToStruct
        }

        static readonly MigrationDescription<ReflectionProbeVersion, HDAdditionalReflectionData> k_ReflectionProbeMigration
            = MigrationDescription.New(
            MigrationStep.New(ReflectionProbeVersion.RemoveUsageOfLegacyProbeParamsForStocking, (HDAdditionalReflectionData t) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                t.m_ObsoleteBlendDistancePositive = t.m_ObsoleteBlendDistanceNegative = Vector3.one * t.reflectionProbe.blendDistance;
                t.m_ObsoleteWeight = t.reflectionProbe.importance;
                t.m_ObsoleteMultiplier = t.reflectionProbe.intensity;
                switch (t.reflectionProbe.refreshMode)
                {
                    case UnityEngine.Rendering.ReflectionProbeRefreshMode.EveryFrame: t.realtimeMode = ProbeSettings.RealtimeMode.EveryFrame; break;
                    case UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake: t.realtimeMode = ProbeSettings.RealtimeMode.OnEnable; break;
                }
#pragma warning restore 618 // Type or member is obsolete
            }),
            MigrationStep.New(ReflectionProbeVersion.UseInfluenceVolume, (HDAdditionalReflectionData t) =>
            {
#pragma warning disable 618
                t.m_ObsoleteInfluenceVolume = t.m_ObsoleteInfluenceVolume ?? new InfluenceVolume();
                t.m_ObsoleteInfluenceVolume.boxSize = t.reflectionProbe.size;
                t.m_ObsoleteInfluenceVolume.obsoleteOffset = t.reflectionProbe.center;
                t.m_ObsoleteInfluenceVolume.sphereRadius = t.m_ObsoleteInfluenceSphereRadius;
                t.m_ObsoleteInfluenceVolume.shape = t.m_ObsoleteInfluenceShape;     //must be done after each size transfert
                t.m_ObsoleteInfluenceVolume.boxBlendDistancePositive = t.m_ObsoleteBlendDistancePositive;
                t.m_ObsoleteInfluenceVolume.boxBlendDistanceNegative = t.m_ObsoleteBlendDistanceNegative;
                t.m_ObsoleteInfluenceVolume.boxBlendNormalDistancePositive = t.m_ObsoleteBlendNormalDistancePositive;
                t.m_ObsoleteInfluenceVolume.boxBlendNormalDistanceNegative = t.m_ObsoleteBlendNormalDistanceNegative;
                t.m_ObsoleteInfluenceVolume.boxSideFadePositive = t.m_ObsoleteBoxSideFadePositive;
                t.m_ObsoleteInfluenceVolume.boxSideFadeNegative = t.m_ObsoleteBoxSideFadeNegative;
#pragma warning restore 618
            }),
            MigrationStep.New(ReflectionProbeVersion.MergeEditors, (HDAdditionalReflectionData t) =>
            {
#pragma warning disable 618
                t.m_ObsoleteInfiniteProjection = !t.reflectionProbe.boxProjection;
#pragma warning restore 618
                t.reflectionProbe.boxProjection = false;
            }),
            MigrationStep.New(ReflectionProbeVersion.AddCaptureSettingsAndFrameSettings, (HDAdditionalReflectionData t) =>
            {
#pragma warning disable 618, 612
                t.m_ObsoleteCaptureSettings = t.m_ObsoleteCaptureSettings ?? new ObsoleteCaptureSettings();
                t.m_ObsoleteCaptureSettings.cullingMask = t.reflectionProbe.cullingMask;
#if UNITY_EDITOR //m_UseOcclusionCulling is not exposed in c# !
                var serializedReflectionProbe = new UnityEditor.SerializedObject(t.reflectionProbe);
                t.m_ObsoleteCaptureSettings.useOcclusionCulling = serializedReflectionProbe.FindProperty("m_UseOcclusionCulling").boolValue;
#endif
                t.m_ObsoleteCaptureSettings.nearClipPlane = t.reflectionProbe.nearClipPlane;
                t.m_ObsoleteCaptureSettings.farClipPlane = t.reflectionProbe.farClipPlane;
#pragma warning restore 618, 612
            }),
            MigrationStep.New(ReflectionProbeVersion.ModeAndTextures, (HDAdditionalReflectionData t) =>
            {
#pragma warning disable 618
                t.m_ObsoleteMode = (ProbeSettings.Mode)t.reflectionProbe.mode;
#pragma warning restore 618
                t.SetTexture(ProbeSettings.Mode.Baked, t.reflectionProbe.bakedTexture);
                t.SetTexture(ProbeSettings.Mode.Custom, t.reflectionProbe.customBakedTexture);
            }),
            MigrationStep.New(ReflectionProbeVersion.ProbeSettings, (HDAdditionalReflectionData t) =>
            {
                k_Migration.ExecuteStep(t, Version.ProbeSettings);

#pragma warning disable 618
                // Migrate capture position
                // Previously, the capture position of a reflection probe was the position of the game object
                //   and the center of the influence volume is (transform.position + t.influenceVolume.m_ObsoleteOffset) in world space
                // Now, the center of the influence volume is the position of the transform and the capture position
                //   is t.probeSettings.proxySettings.capturePositionProxySpace and is in capture space

                var capturePositionWS = t.transform.position;
                // set the transform position to the influence position world space
                var mat = Matrix4x4.TRS(t.transform.position, t.transform.rotation, Vector3.one);
                t.transform.position = mat.MultiplyPoint(t.influenceVolume.obsoleteOffset);

                var capturePositionPS = t.proxyToWorld.inverse.MultiplyPoint(capturePositionWS);
                t.m_ProbeSettings.proxySettings.capturePositionProxySpace = capturePositionPS;
#pragma warning restore 618
            }),
            MigrationStep.New(ReflectionProbeVersion.SeparatePassThrough, (HDAdditionalReflectionData t) => k_Migration.ExecuteStep(t, Version.SeparatePassThrough)),
            MigrationStep.New(ReflectionProbeVersion.UpgradeFrameSettingsToStruct, (HDAdditionalReflectionData t) => k_Migration.ExecuteStep(t, Version.UpgradeFrameSettingsToStruct))
            );

        [SerializeField, FormerlySerializedAs("version"), FormerlySerializedAs("m_Version")]
        int m_ReflectionProbeVersion;
        ReflectionProbeVersion IVersionable<ReflectionProbeVersion>.version { get => (ReflectionProbeVersion)m_ReflectionProbeVersion; set => m_ReflectionProbeVersion = (int)value; }

        #region Deprecated Fields
#pragma warning disable 649 //never assigned
        //data only kept for migration, to be removed in future version
        [SerializeField, FormerlySerializedAs("influenceShape"), System.Obsolete("influenceShape is deprecated, use influenceVolume parameters instead")]
        InfluenceShape m_ObsoleteInfluenceShape;
        [SerializeField, FormerlySerializedAs("influenceSphereRadius"), System.Obsolete("influenceSphereRadius is deprecated, use influenceVolume parameters instead")]
        float m_ObsoleteInfluenceSphereRadius = 3.0f;
        [SerializeField, FormerlySerializedAs("blendDistancePositive"), System.Obsolete("blendDistancePositive is deprecated, use influenceVolume parameters instead")]
        Vector3 m_ObsoleteBlendDistancePositive = Vector3.zero;
        [SerializeField, FormerlySerializedAs("blendDistanceNegative"), System.Obsolete("blendDistanceNegative is deprecated, use influenceVolume parameters instead")]
        Vector3 m_ObsoleteBlendDistanceNegative = Vector3.zero;
        [SerializeField, FormerlySerializedAs("blendNormalDistancePositive"), System.Obsolete("blendNormalDistancePositive is deprecated, use influenceVolume parameters instead")]
        Vector3 m_ObsoleteBlendNormalDistancePositive = Vector3.zero;
        [SerializeField, FormerlySerializedAs("blendNormalDistanceNegative"), System.Obsolete("blendNormalDistanceNegative is deprecated, use influenceVolume parameters instead")]
        Vector3 m_ObsoleteBlendNormalDistanceNegative = Vector3.zero;
        [SerializeField, FormerlySerializedAs("boxSideFadePositive"), System.Obsolete("boxSideFadePositive is deprecated, use influenceVolume parameters instead")]
        Vector3 m_ObsoleteBoxSideFadePositive = Vector3.one;
        [SerializeField, FormerlySerializedAs("boxSideFadeNegative"), System.Obsolete("boxSideFadeNegative is deprecated, use influenceVolume parameters instead")]
        Vector3 m_ObsoleteBoxSideFadeNegative = Vector3.one;
#pragma warning restore 649 //never assigned
        #endregion
    }
}
