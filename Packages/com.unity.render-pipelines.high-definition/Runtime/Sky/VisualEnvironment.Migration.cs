namespace UnityEngine.Rendering.HighDefinition
{
    public sealed partial class VisualEnvironment : IVersionable<VisualEnvironment.Version>
    {
        /// <summary>
        /// The version used during the migration
        /// </summary>
        enum Version
        {
            /// <summary>Version Step</summary>
            Initial,
            /// <summary>Version Step</summary>
            UnitChange,

            /// <summary>Latest Version</summary>
            Count,
        }

        /// <summary>
        /// The migration steps for PhysicallyBasedSky
        /// </summary>
        static readonly MigrationDescription<Version, VisualEnvironment> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.UnitChange, (VisualEnvironment env) =>
            {
                // Avoids migrating twice (the pbr sky also does it if present)
                if (env.planetRadius.value > k_DefaultEarthRadius / 1000.0f + 1.0f)
                {
                    env.planetRadius.value /= 1000.0f;
                    env.planetCenter.value /= 1000.0f;
                }
            })
        );

        void Awake()
        {
            if (m_Version == Version.Count)
                m_Version = Version.Initial;

            k_Migration.Migrate(this);
        }

        [SerializeField]
        Version m_Version = Version.Count;
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }
    }
}
