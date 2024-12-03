using System;

namespace UnityEngine.Rendering
{
    class VrsResources : IDisposable
    {
        internal ProfilingSampler conversionProfilingSampler = new ProfilingSampler("VrsConversion");
        internal ProfilingSampler visualizationProfilingSampler = new ProfilingSampler("VrsVisualization");
        internal GraphicsBuffer conversionLutBuffer;
        internal GraphicsBuffer visualizationLutBuffer;

        internal ComputeShader textureComputeShader;
        internal int textureReduceKernel = -1;
        internal int textureCopyKernel = -1;

        internal Vector2Int tileSize;

        internal GraphicsBuffer validatedShadingRateFragmentSizeBuffer;

        Shader m_VisualizationShader;
        Material m_VisualizationMaterial;
        internal Material visualizationMaterial
        {
            get
            {
                // explicit check here: handle case where m_Material is set to null when editor reloads
                if (m_VisualizationMaterial == null)
                    m_VisualizationMaterial = new Material(m_VisualizationShader);

                return m_VisualizationMaterial;
            }
        }

        internal VrsResources(VrsRenderPipelineRuntimeResources resources)
        {
            InitializeResources(resources);

#if UNITY_EDITOR
            GraphicsSettings.Unsubscribe<VrsRenderPipelineRuntimeResources>(OnVrsResourcesChanged);
            GraphicsSettings.Subscribe<VrsRenderPipelineRuntimeResources>(OnVrsResourcesChanged);
#endif
        }

#if UNITY_EDITOR
        void OnVrsResourcesChanged(VrsRenderPipelineRuntimeResources resources, string propertyName)
        {
            DisposeResources();
            InitializeResources(resources);
        }
#endif

        ~VrsResources()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
#if UNITY_EDITOR
            GraphicsSettings.Unsubscribe<VrsRenderPipelineRuntimeResources>(OnVrsResourcesChanged);
#endif
            DisposeResources();
        }

        void InitializeResources(VrsRenderPipelineRuntimeResources resources)
        {
            bool success = InitComputeShader(resources);
            if (!success)
            {
                DisposeResources();
                return;
            }

            m_VisualizationShader = resources.visualizationShader;
            conversionLutBuffer = resources.conversionLookupTable.CreateBuffer();
            visualizationLutBuffer = resources.visualizationLookupTable.CreateBuffer(true);
            AllocFragmentSizeBuffer();
        }

        void DisposeResources()
        {
            conversionLutBuffer?.Dispose();
            conversionLutBuffer = null;

            visualizationLutBuffer?.Dispose();
            visualizationLutBuffer = null;

            validatedShadingRateFragmentSizeBuffer?.Dispose();
            validatedShadingRateFragmentSizeBuffer = null;

            m_VisualizationShader = null;
            m_VisualizationMaterial = null;
        }

        void AllocFragmentSizeBuffer()
        {
            // Get available shading rate fragment sizes; unsupported ones
            // will be mapped to the closest supported one.
            var fragmentSize = new uint[Vrs.shadingRateFragmentSizeCount];

            var lastAvailableFragmentSize = ShadingRateFragmentSize.FragmentSize1x1;
            uint fragmentSizeNativeValue = ShadingRateInfo.QueryNativeValue(lastAvailableFragmentSize);

            foreach (var availableFragmentSize in ShadingRateInfo.availableFragmentSizes)
            {
                Array.Fill(fragmentSize,
                    fragmentSizeNativeValue,
                    (int)lastAvailableFragmentSize,
                    availableFragmentSize - lastAvailableFragmentSize + 1);

                lastAvailableFragmentSize = availableFragmentSize;
                fragmentSizeNativeValue = ShadingRateInfo.QueryNativeValue(lastAvailableFragmentSize);
            }

            Array.Fill(fragmentSize,
                fragmentSizeNativeValue,
                (int)lastAvailableFragmentSize,
                ShadingRateFragmentSize.FragmentSize4x4 - lastAvailableFragmentSize + 1);

            validatedShadingRateFragmentSizeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, fragmentSize.Length, sizeof(uint));
            validatedShadingRateFragmentSizeBuffer.SetData(fragmentSize);
        }

        bool InitComputeShader(VrsRenderPipelineRuntimeResources resources)
        {
            // This Compute Shader resource is used for converting an RGB texture to an R8 SRI
            // Don't initialize it if the device does not support image-based VRS
            if (!ShadingRateInfo.supportsPerImageTile)
            {
                return false;
            }

            if (!SystemInfo.supportsComputeShaders)
            {
                return false;
            }

            tileSize = ShadingRateInfo.imageTileSize;
            var tileSizeOk = tileSize.x == tileSize.y && (tileSize.x == 8 || tileSize.x == 16 || tileSize.x == 32);
            if (!tileSizeOk)
            {
                Debug.LogError($"VRS unsupported tile size: {tileSize.x}x{tileSize.y}.");
                return false;
            }

            // We expect keywords, if the shader was excluded/discarded then no keywords.
            if (resources.textureComputeShader?.keywordSpace.keywordCount <= 0)
            {
                // Invalidate kernel indices in case they were set (shader reload)
                textureReduceKernel = -1;
                textureCopyKernel = -1;
                return false;
            }

            textureComputeShader = resources.textureComputeShader;

            // this keyword need only be set once
            textureComputeShader.EnableKeyword($"{VrsShaders.k_TileSizePrefix}{tileSize.x}");

            // find kernel might fail
            textureReduceKernel = TryFindKernel(textureComputeShader, VrsShaders.k_KernelTextureReduce);
            textureCopyKernel = TryFindKernel(textureComputeShader, VrsShaders.k_KernelTextureReduce);

            if (textureReduceKernel == -1 || textureCopyKernel == -1)
                return false;

            return true;
        }

        static int TryFindKernel(ComputeShader computeShader, string name) => computeShader.HasKernel(name) ? computeShader.FindKernel(name) : -1;
    }
}
