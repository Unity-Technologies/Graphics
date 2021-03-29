using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDRenderPipelineGlobalSettings : IVersionable<HDRenderPipelineGlobalSettings.Version>
    {
        enum Version
        {
            First,
        }

        [SerializeField]
        Version m_Version = MigrationDescription.LastVersion<Version>();
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }
    }
}
