namespace UnityEngine.Rendering.HighDefinition
{
    abstract class IBLFilterBSDF
    {
        // Material that convolves the cubemap using the profile
        protected Material m_convolveMaterial;
        protected Matrix4x4[] m_faceWorldToViewMatrixMatrices = new Matrix4x4[6];

        // Input data
        protected HDRenderPipeline m_RenderPipeline;
        protected MipGenerator m_MipGenerator;

        protected IBLFilterBSDF(HDRenderPipeline renderPipeline, MipGenerator mipGenerator)
        {
            m_RenderPipeline = renderPipeline;
            m_MipGenerator = mipGenerator;
        }

        abstract public bool IsInitialized();

        abstract public void Initialize(CommandBuffer cmd);

        abstract public void Cleanup();

        // Filters MIP map levels (other than 0) with GGX using BRDF importance sampling.
        abstract public void FilterCubemap(CommandBuffer cmd, Texture source, RenderTexture target);

        internal struct PlanarTextureFilteringParameters
        {
            // Flag that defines if we should be evaluating all the mip levels for the planar reflection
            public bool smoothPlanarReflection;
            // Depth buffer (oblique) that was produced
            public RenderTexture captureCameraDepthBuffer;
            // Inverse view projection matrix (oblique)
            public Matrix4x4 captureCameraIVP;
            // View projection matrix (non oblique)
            public Matrix4x4 captureCameraVP_NonOblique;
            // Inverse view projection matrix (non oblique)
            public Matrix4x4 captureCameraIVP_NonOblique;
            // Position of the capture camera
            public Vector3 captureCameraPosition;
            // Resolution of the capture camera
            public Vector4 captureCameraScreenSize;
            // Position of the probe
            public Vector3 probePosition;
            // Normal of the reflection probe
            public Vector3 probeNormal;
            // FOV of the capture camera
            public float captureFOV;
            // Near clipping plane of the capture camera
            public float captureNearPlane;
            // Far clipping plane of the capture camera
            public float captureFarPlane;
        };

        abstract public void FilterPlanarTexture(CommandBuffer cmd, RenderTexture source, ref PlanarTextureFilteringParameters planarTextureFilteringParameters, RenderTexture target);
        public abstract void FilterCubemapMIS(CommandBuffer cmd, Texture source, RenderTexture target, RenderTexture conditionalCdf, RenderTexture marginalRowCdf);
    }
}
