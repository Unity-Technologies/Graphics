using System;
using UnityEngine;
using UnityEditor.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    [Serializable]
    sealed class HDMetadata : ScriptableObject
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

        public System.Collections.Generic.List<string> m_Locks = new System.Collections.Generic.List<string>();
    }

    static public class HDMetaDataHelper
    {
        static public System.Collections.Generic.List<string> GetLocksFromMetaData(Shader shader)
        {
            HDMetadata metaData;
            if (shader.TryGetMetadataOfType<HDMetadata>(out metaData))
                return metaData.m_Locks;
            else
                return null;
        }
    }
}
