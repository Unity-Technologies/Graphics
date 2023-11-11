using System;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class WaterFoamGenerator : IVersionable<WaterFoamGenerator.Version>
    {
        enum Version
        {
            First,
            FoamRemap,

            Count,
        }

        [SerializeField]
        Version m_Version = Version.First; //MigrationDescription.LastVersion<Version>() - 1;
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        static readonly MigrationDescription<Version, WaterFoamGenerator> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.FoamRemap, (WaterFoamGenerator s) =>
            {
                s.surfaceFoamDimmer = Mathf.Min(s.surfaceFoamDimmer * 3.0f, 1.0f);
                s.deepFoamDimmer = Mathf.Min(s.deepFoamDimmer * 3.0f, 1.0f);
            })
        );
    }
}
