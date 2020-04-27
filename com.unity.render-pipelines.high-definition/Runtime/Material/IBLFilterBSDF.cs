namespace UnityEngine.Rendering.HighDefinition
{
    abstract class IBLFilterBSDF
    {
        // Material that convolves the cubemap using the profile
        protected Material m_convolveMaterial;
        protected Matrix4x4[] m_faceWorldToViewMatrixMatrices = new Matrix4x4[6];

        // Input data
        protected RenderPipelineResources m_RenderPipelineResources;
        protected MipGenerator m_MipGenerator;

        abstract public bool IsInitialized();

        abstract public void Initialize(CommandBuffer cmd);

        abstract public void Cleanup();

        // Filters MIP map levels (other than 0) with GGX using BRDF importance sampling.
        abstract public void FilterCubemap(CommandBuffer cmd, Texture source, RenderTexture target);

        internal struct PlanarTextureFilteringParameters
        {
            public RenderTexture captureCameraDepthBuffer;
            public Matrix4x4 captureCameraIVP;
            public Matrix4x4 captureCameraVP;
            public Matrix4x4 captureCameraWorldToView;
            public Matrix4x4 _CaptureCameraVP;
            public Vector3 captureCameraPosition;
            public Vector3 captureCameraUp;
            public Vector3 captureCameraRight;
            public Vector4 captureCameraScreenSize;
            public Vector3 probePosition;
            public Vector3 probeNormal;
            public float captureFOV;
        };

        abstract public void FilterPlanarTexture(CommandBuffer cmd, RenderTexture source, ref PlanarTextureFilteringParameters planarTextureFilteringParameters, RenderTexture target);
        public abstract void FilterCubemapMIS(CommandBuffer cmd, Texture source, RenderTexture target, RenderTexture conditionalCdf, RenderTexture marginalRowCdf);
    }
}
