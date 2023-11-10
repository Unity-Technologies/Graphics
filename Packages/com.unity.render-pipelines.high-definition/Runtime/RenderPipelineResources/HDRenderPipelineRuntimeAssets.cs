using System;
using System.ComponentModel;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    [HideInInspector]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [Category("Resources/Runtime Assets")]
    class HDRenderPipelineRuntimeAssets : IRenderPipelineResources
    {
        public int version => 0;

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        // Default Diffusion Profile
        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/defaultDiffusionProfile.asset")]
        private DiffusionProfileSettings m_DefaultDiffusionProfile;
        public DiffusionProfileSettings defaultDiffusionProfile
        {
            get => m_DefaultDiffusionProfile;
            set => this.SetValueAndNotify(ref m_DefaultDiffusionProfile, value);
        }

        // Compute Material Library
        [SerializeField] [ResourcePath("Runtime/RenderPipelineResources/ComputeMaterialLibrary.asset")]
        private ComputeMaterialLibrary m_ComputeMaterialLibrary;
        public ComputeMaterialLibrary computeMaterialLibrary
        {
            get => m_ComputeMaterialLibrary;
            set => this.SetValueAndNotify(ref m_ComputeMaterialLibrary, value, nameof(m_ComputeMaterialLibrary));
        }

        // Area Light Emissive Meshes
        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Mesh/Cylinder.fbx")]
        private Mesh m_EmissiveCylinderMesh;
        public Mesh emissiveCylinderMesh
        {
            get => m_EmissiveCylinderMesh;
            set => this.SetValueAndNotify(ref m_EmissiveCylinderMesh, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Mesh/Quad.fbx")]
        private Mesh m_EmissiveQuadMesh;
        public Mesh emissiveQuadMesh
        {
            get => m_EmissiveQuadMesh;
            set => this.SetValueAndNotify(ref m_EmissiveQuadMesh, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Mesh/Sphere.fbx")]
        private Mesh m_SphereMesh;
        public Mesh sphereMesh
        {
            get => m_SphereMesh;
            set => this.SetValueAndNotify(ref m_SphereMesh, value);
        }

        // APV Sampling Debug Mesh
        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Mesh/ProbeSamplingDebugMesh.fbx")]
        private Mesh m_ProbeSamplingDebugMesh;
        public Mesh probeSamplingDebugMesh
        {
            get => m_ProbeSamplingDebugMesh;
            set => this.SetValueAndNotify(ref m_ProbeSamplingDebugMesh, value);
        }
    }
}
