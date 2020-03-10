using UnityEngine;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    internal class UniversalMeshTargetData : TargetImplementationData
    {
#region Fields
        [SerializeField]
        MaterialType m_MaterialType = MaterialType.Lit;

        [SerializeField]
        WorkflowMode m_WorkflowMode = WorkflowMode.Metallic;

        [SerializeField]
        SurfaceType m_SurfaceType;

        [SerializeField]
        AlphaMode m_AlphaMode;

        [SerializeField]
        bool m_TwoSided;

        [SerializeField]
        bool m_AlphaClip;

        [SerializeField]
        bool m_AddPrecomputedVelocity = false;

        [SerializeField]
        bool m_DOTSInstancing = false;

        [SerializeField]
        NormalDropOffSpace m_NormalDropOffSpace;
#endregion

#region Properties
        public MaterialType materialType
        {
            get => m_MaterialType;
            set => m_MaterialType = value;
        }

        public WorkflowMode workflowMode
        {
            get => m_WorkflowMode;
            set => m_WorkflowMode = value;
        }

        public SurfaceType surfaceType
        {
            get => m_SurfaceType;
            set => m_SurfaceType = value;
        }

        public AlphaMode alphaMode
        {
            get => m_AlphaMode;
            set => m_AlphaMode = value;
        }

        public bool twoSided
        {
            get => m_TwoSided;
            set => m_TwoSided = value;
        }

        public bool alphaClip
        {
            get => m_AlphaClip;
            set => m_AlphaClip = value;
        }

        public bool addPrecomputedVelocity
        {
            get => m_AddPrecomputedVelocity;
            set => m_AddPrecomputedVelocity = value;
        }

        public bool dotsInstancing
        {
            get => m_DOTSInstancing;
            set => m_DOTSInstancing = value;
        }

        public NormalDropOffSpace normalDropOffSpace
        {
            get => m_NormalDropOffSpace;
            set => m_NormalDropOffSpace = value;
        }
#endregion
    }
}
