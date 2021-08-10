using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering
{
    // Note: there is no migration framework in CoreRP. So encode step by step upgrade in serialization callback for now.
    public partial class Volume : ISerializationCallbackReceiver
    {
        enum Version
        {
            First,
            ChangingPriotityFloatToInt,

            // Automatic end gathering, do not edit. Insert new Version above.
            AfterMax,
            Last = AfterMax - 1,
        }

        // There was no version prior. Cannot set it to Version.Last here as this lead to creation of the entry at last version for former serialized Volume too.
        // Case A: instance creation. OnAfterDeserialize is not called and right before first save, OnBeforeSerialize will set this to Version.Last.
        // Case B: load Volume in a previous format (before m_Version). No m_Version existed so it init to default (First). Then OnAfterDeserialize is called and the migration occures (all steps).
        // Case C: load Volume in a previous format (with m_Version). OnAfterDeserialize is called and the migration occures from the version serialized.
        // Case D: load Volume in last format. OnAfterDeserialize is called but no migration will be done (already up to date).
        [SerializeField]
        Version m_Version;

        void ISerializationCallbackReceiver.OnAfterDeserialize()
            => Upgrade();

        void ISerializationCallbackReceiver.OnBeforeSerialize()
            => m_Version = Version.Last;

        void Upgrade()
        {
            if (m_Version == Version.Last)
                return;

            for (int i = 0; i < s_UpgradeSteps.Length; ++i)
            {
                (Version version, Action<Volume> upgrader)step = s_UpgradeSteps[i];
                if (m_Version < step.version)
                {
                    step.upgrader(this);
                    m_Version = step.version;
                }
            }
        }

        // This should always be ordered by version
        static (Version, Action<Volume>)[] s_UpgradeSteps = new(Version, Action<Volume>)[]
        {
            (Version.ChangingPriotityFloatToInt, data => {
#pragma warning disable 618 // Type or member is obsolete
                data.m_Priority = Mathf.RoundToInt(data.m_ObsoletePriority * 1000);
#pragma warning restore 618 // Type or member is obsolete
            }),
        };

        [SerializeField, FormerlySerializedAs("priority")]
        [Obsolete("For Data Migration")]
        float m_ObsoletePriority = 0f;
    }
}
