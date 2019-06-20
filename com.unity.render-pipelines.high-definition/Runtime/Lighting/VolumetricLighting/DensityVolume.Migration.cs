using System;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial struct DensityVolumeArtistParameters
    {
        [Obsolete("Never worked correctly due to having engine working in percent. Will be removed soon.")]
        public bool advancedFade => true;

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

    public partial class DensityVolume : IVersionable<DensityVolume.Version>
    {
        enum Version
        {
            First,
            ScaleIndependent,
            FixUniformBlendDistanceToBeMetric,
            // Add new version here and they will automatically be the Current one
            Max,
            Current = Max - 1
        }

        static readonly MigrationDescription<Version, DensityVolume> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.ScaleIndependent, (DensityVolume data) =>
            {
                data.parameters.size = data.transform.lossyScale;

                //missing migrated data.
                //when migrated prior to this fix, density volumes have to be manually set on advance mode.
                data.parameters.m_EditorAdvancedFade = true;
            }),
            MigrationStep.New(Version.FixUniformBlendDistanceToBeMetric, (DensityVolume data) => data.parameters.MigrateToFixUniformBlendDistanceToBeMetric())
        );

        [SerializeField]
        Version m_Version;
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        void Awake() => k_Migration.Migrate(this);
    }
}
