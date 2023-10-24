using System;
using UnityEngine;

using static UnityEngine.Rendering.HighDefinition.HDMaterial;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    [Serializable]
    sealed class HDMetadata : ScriptableObject
    {
        [SerializeField]
        ShaderID m_ShaderID;

        [SerializeField]
        string m_SubTargetGuidString;

        [SerializeField]
        bool m_MigrateFromOldCrossPipelineSG; // Keep track from which old SG master node we come from

        [SerializeField]
        ShaderGraphVersion m_HDSubTargetVersion; // copied from systemData.m_Version

        [SerializeField]
        int m_SubTargetSpecificVersion; // eg subtarget-private versioning, used by plugin subtargets

        [SerializeField]
        bool m_HasVertexModificationInMotionVector;

        [SerializeField]
        bool m_IsVFXCompatible;

        public ShaderID shaderID
        {
            get => m_ShaderID;
            set => m_ShaderID = value;
        }

        public GUID subTargetGuid
        {
            get
            {
                if (!string.IsNullOrEmpty(m_SubTargetGuidString) && GUID.TryParse(m_SubTargetGuidString, out GUID guid))
                    return guid;
                else
                    return new GUID();
            }
            set => m_SubTargetGuidString = value.ToString();
        }

        public bool migrateFromOldCrossPipelineSG
        {
            get => m_MigrateFromOldCrossPipelineSG;
            set => m_MigrateFromOldCrossPipelineSG = value;
        }

        public ShaderGraphVersion hdSubTargetVersion
        {
            get => m_HDSubTargetVersion;
            set => m_HDSubTargetVersion = value;
        }

        public int subTargetSpecificVersion
        {
            get => m_SubTargetSpecificVersion;
            set => m_SubTargetSpecificVersion = value;
        }

        public bool hasVertexModificationInMotionVector
        {
            get => m_HasVertexModificationInMotionVector;
            set => m_HasVertexModificationInMotionVector = value;
        }

        public bool isVFXCompatible
        {
            get => m_IsVFXCompatible;
            set => m_IsVFXCompatible = value;
        }
    }
}
