using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class CloudLayer : IVersionable<CloudLayer.Version>, ISerializationCallbackReceiver
    {
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (m_Version == Version.Count) // serializing a newly created object
                m_Version = Version.Count - 1; // mark as up to date
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (m_Version == Version.Count) // deserializing and object without version
                m_Version = Version.Initial; // reset to run the migration
        }

        enum Version
        {
            Initial,
            Raymarching3D,

            Count
        }

        static readonly MigrationDescription<Version, CloudLayer> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.Raymarching3D, (CloudLayer c) =>
            {
                c.layerA.thickness.value = 100 + c.layerA.thickness.value * (8000-100);
                c.layerB.thickness.value = 100 + c.layerB.thickness.value * (8000-100);
            })
        );

        void Awake()
        {
            k_Migration.Migrate(this);
        }

        [SerializeField]
        Version m_Version = Version.Count;
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }
    }
}
