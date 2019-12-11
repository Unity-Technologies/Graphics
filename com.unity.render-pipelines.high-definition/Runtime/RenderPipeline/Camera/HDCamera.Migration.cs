using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDCamera : IVersionable<HDCamera.Version>
    {
        enum Version
        {
            None,
            RemovalOfAdditionalDataPattern
        }

        [SerializeField]
        Version m_Version;

        static readonly MigrationDescription<Version, HDCamera> k_Migration = MigrationDescription.New<Version, HDCamera>(
            //add migration step here
        );

        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        void Awake() => k_Migration.Migrate(this);

        // Add obsolete legacy data keeped for migration below this line
        // -------------------------------------------------------------
    }
}
