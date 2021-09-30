using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class InfluenceVolume : IVersionable<InfluenceVolume.Version>, ISerializationCallbackReceiver
    {
        enum Version
        {
            Initial,
            SphereOffset
        }

        static readonly MigrationDescription<Version, InfluenceVolume> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.SphereOffset, (InfluenceVolume i) =>
            {
                if (i.shape == InfluenceShape.Sphere)
                {
#pragma warning disable 618
                    i.m_ObsoleteOffset = i.m_ObsoleteSphereBaseOffset;
#pragma warning restore 618
                }
            })
        );

        [SerializeField]
        [ExcludeCopy]
        Version m_Version = MigrationDescription.LastVersion<Version>();
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        // Obsolete fields
#pragma warning disable 649 //never assigned
        [SerializeField, FormerlySerializedAs("m_SphereBaseOffset"), Obsolete("For Data Migration")]
        [ExcludeCopy]
        Vector3 m_ObsoleteSphereBaseOffset;
        [SerializeField, FormerlySerializedAs("m_BoxBaseOffset"), FormerlySerializedAs("m_Offset")]
        [ExcludeCopy]
        Vector3 m_ObsoleteOffset;
        [Obsolete("Only used for data migration purpose. Don't use this field.")]
        internal Vector3 obsoleteOffset { get => m_ObsoleteOffset; set => m_ObsoleteOffset = value; }
#pragma warning restore 649 //never assigned

        /// <summary>Serialization callback</summary>
        public void OnBeforeSerialize() { }
        /// <summary>Serialization callback</summary>
        public void OnAfterDeserialize() => k_Migration.Migrate(this);
    }
}
