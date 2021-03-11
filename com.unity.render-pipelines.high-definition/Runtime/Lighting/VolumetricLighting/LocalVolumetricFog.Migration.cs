using System;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial struct LocalVolumetricFogArtistParameters
    {
        internal void MigrateToFixUniformBlendDistanceToBeMetric()
        {
            //Note: At this revision, advanceMode boolean is obsolete and unusable anymore
            if (!m_EditorAdvancedFade)
            {
                //Replicate old editor behavior of normal mode to keep scene intact
                m_EditorAdvancedFade = true;
                negativeFade = positiveFade = m_EditorUniformFade * Vector3.one;
                m_EditorUniformFade = 0f;
            }

            //feed new variable to handle editor values
            m_EditorPositiveFade = positiveFade;
            m_EditorNegativeFade = negativeFade;
        }
    }

    public partial class LocalVolumetricFog : IVersionable<LocalVolumetricFog.Version>
    {
        enum Version
        {
            First,
            ScaleIndependent,
            FixUniformBlendDistanceToBeMetric,
        }

        static readonly MigrationDescription<Version, LocalVolumetricFog> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.ScaleIndependent, (LocalVolumetricFog data) =>
            {
                data.parameters.size = data.transform.lossyScale;

                //missing migrated data.
                //when migrated prior to this fix, Local Volumetric Fog have to be manually set on advance mode.
                data.parameters.m_EditorAdvancedFade = true;
            }),
            MigrationStep.New(Version.FixUniformBlendDistanceToBeMetric, (LocalVolumetricFog data) => data.parameters.MigrateToFixUniformBlendDistanceToBeMetric())
        );

        [SerializeField]
        Version m_Version = MigrationDescription.LastVersion<Version>();
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        void Awake() => k_Migration.Migrate(this);
    }
}
