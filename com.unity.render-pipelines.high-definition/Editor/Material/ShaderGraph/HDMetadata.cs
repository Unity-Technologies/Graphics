using System;
using UnityEngine;
using UnityEditor.Rendering.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    [Serializable]
    sealed class HDMetadata : Metadata
    {
        [SerializeField]
        HDShaderUtils.ShaderID m_ShaderID;

        [SerializeField]
        bool m_MigrateFromOldCrossPipelineSG; // Keep track from which old SG master node we come from

        public HDShaderUtils.ShaderID shaderID
        {
            get => m_ShaderID;
            set => m_ShaderID = value;
        }

        public bool migrateFromOldCrossPipelineSG
        {
            get => m_MigrateFromOldCrossPipelineSG;
            set => m_MigrateFromOldCrossPipelineSG = value;
        }
    }
}
