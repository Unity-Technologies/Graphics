using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class PhysicallyBasedSky : IVersionable<PhysicallyBasedSky.Version>
    {
        /// <summary>
        /// The version used during the migration
        /// </summary>
        protected enum Version
        {
            /// <summary>Version Step</summary>
            Initial,
            /// <summary>Version Step</summary>
            TypeEnum,
        }

        /// <summary>
        /// The migration steps for PhysicallyBasedSky
        /// </summary>
        protected static readonly MigrationDescription<Version, PhysicallyBasedSky> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.TypeEnum, (PhysicallyBasedSky p) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                p.type.value = p.m_ObsoleteEarthPreset.value ? PhysicallyBasedSkyModel.EarthAdvanced : PhysicallyBasedSkyModel.Custom;
                p.type.overrideState = p.m_ObsoleteEarthPreset.overrideState;
#pragma warning restore 618
            })
        );

        void Awake()
        {
            k_Migration.Migrate(this);
        }

        [SerializeField]
        Version m_SkyVersion;
        Version IVersionable<Version>.version { get => m_SkyVersion; set => m_SkyVersion = value; }

        /// <summary>Obsolete field. Simplifies the interface by using parameters suitable to simulate Earth.</summary>
        [SerializeField, FormerlySerializedAs("earthPreset"), Obsolete("For Data Migration")]
        BoolParameter m_ObsoleteEarthPreset = new BoolParameter(true);
    }
}
