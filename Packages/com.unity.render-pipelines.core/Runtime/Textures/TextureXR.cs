using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Utility class providing default textures compatible in any XR setup.
    /// </summary>
    public static class TextureXR
    {
        // Property set by XRSystem
        private static int m_MaxViews = 1;
        /// <summary>
        /// Maximum number of views handled by the XR system.
        /// </summary>
        public static int maxViews
        {
            set
            {
                m_MaxViews = value;
            }
        }

        // Property accessed when allocating a render target
        /// <summary>
        /// Number of slices used by the XR system.
        /// </summary>
        public static int slices { get => m_MaxViews; }

        // Must be in sync with shader define in TextureXR.hlsl
        /// <summary>
        /// Returns true if the XR system uses texture arrays.
        /// </summary>
        public static bool useTexArray
        {
            get
            {
                switch (SystemInfo.graphicsDeviceType)
                {
                    case GraphicsDeviceType.Direct3D11:
                    case GraphicsDeviceType.Direct3D12:
                    case GraphicsDeviceType.PlayStation4:
                    case GraphicsDeviceType.PlayStation5:
                    case GraphicsDeviceType.PlayStation5NGGC:
                    case GraphicsDeviceType.Vulkan:
                    case GraphicsDeviceType.Metal:
                        return true;

                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Dimension of XR textures.
        /// </summary>
        public static TextureDimension dimension
        {
            get
            {
                // TEXTURE2D_X macros will now expand to TEXTURE2D or TEXTURE2D_ARRAY
                return useTexArray ? TextureDimension.Tex2DArray : TextureDimension.Tex2D;
            }
        }

        // Need to keep both the Texture and the RTHandle in order to be able to track lifetime properly.
        static Texture m_BlackUIntTexture2DArray;
        static Texture m_BlackUIntTexture;
        static RTHandle m_BlackUIntTexture2DArrayRTH;
        static RTHandle m_BlackUIntTextureRTH;
        /// <summary>
        /// Default black unsigned integer texture.
        /// </summary>
        /// <returns>The default black unsigned integer texture.</returns>
        public static RTHandle GetBlackUIntTexture() { return useTexArray ? m_BlackUIntTexture2DArrayRTH : m_BlackUIntTextureRTH; }

        static Texture2DArray m_ClearTexture2DArray;
        static Texture2D m_ClearTexture;
        static RTHandle m_ClearTexture2DArrayRTH;
        static RTHandle m_ClearTextureRTH;
        /// <summary>
        /// Default clear color (0, 0, 0, 1) texture.
        /// </summary>
        /// <returns>The default clear color texture.</returns>
        public static RTHandle GetClearTexture() { return useTexArray ? m_ClearTexture2DArrayRTH : m_ClearTextureRTH; }

        static Texture2DArray m_MagentaTexture2DArray;
        static Texture2D m_MagentaTexture;
        static RTHandle m_MagentaTexture2DArrayRTH;
        static RTHandle m_MagentaTextureRTH;
        /// <summary>
        /// Default magenta texture.
        /// </summary>
        /// <returns>The default magenta texture.</returns>
        public static RTHandle GetMagentaTexture() { return useTexArray ? m_MagentaTexture2DArrayRTH : m_MagentaTextureRTH; }

        static Texture2D m_BlackTexture;
        static Texture3D m_BlackTexture3D;
        static Texture2DArray m_BlackTexture2DArray;
        static RTHandle m_BlackTexture2DArrayRTH;
        static RTHandle m_BlackTextureRTH;
        static RTHandle m_BlackTexture3DRTH;
        /// <summary>
        /// Default black texture.
        /// </summary>
        /// <returns>The default black texture.</returns>
        public static RTHandle GetBlackTexture() { return useTexArray ? m_BlackTexture2DArrayRTH : m_BlackTextureRTH; }
        /// <summary>
        /// Default black texture array.
        /// </summary>
        /// <returns>The default black texture array.</returns>
        public static RTHandle GetBlackTextureArray() { return m_BlackTexture2DArrayRTH; }
        /// <summary>
        /// Default black texture 3D.
        /// </summary>
        /// <returns>The default black texture 3D.</returns>
        public static RTHandle GetBlackTexture3D() { return m_BlackTexture3DRTH; }

        static Texture2DArray m_WhiteTexture2DArray;
        static RTHandle m_WhiteTexture2DArrayRTH;
        static RTHandle m_WhiteTextureRTH;
        /// <summary>
        /// Default white texture.
        /// </summary>
        /// <returns>The default white texture.</returns>
        public static RTHandle GetWhiteTexture() { return useTexArray ? m_WhiteTexture2DArrayRTH : m_WhiteTextureRTH; }

        /// <summary>
        /// Initialize XR textures. Must be called at least once.
        /// </summary>
        /// <param name="cmd">Command Buffer used to initialize textures.</param>
        /// <param name="clearR32_UIntShader">Compute shader used to intitialize unsigned integer textures.</param>
        public static void Initialize(CommandBuffer cmd, ComputeShader clearR32_UIntShader)
        {
            if (m_BlackUIntTexture2DArray == null) // We assume that everything is invalid if one is invalid.
            {
                // Black UINT
                RTHandles.Release(m_BlackUIntTexture2DArrayRTH);
                m_BlackUIntTexture2DArray = CreateBlackUIntTextureArray(cmd, clearR32_UIntShader);
                m_BlackUIntTexture2DArrayRTH = RTHandles.Alloc(m_BlackUIntTexture2DArray);
                RTHandles.Release(m_BlackUIntTextureRTH);
                m_BlackUIntTexture = CreateBlackUintTexture(cmd, clearR32_UIntShader);
                m_BlackUIntTextureRTH = RTHandles.Alloc(m_BlackUIntTexture);

                // Clear
                RTHandles.Release(m_ClearTextureRTH);
                m_ClearTexture = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None) { name = "Clear Texture" };
                m_ClearTexture.SetPixel(0, 0, Color.clear);
                m_ClearTexture.Apply();
                m_ClearTextureRTH = RTHandles.Alloc(m_ClearTexture);
                RTHandles.Release(m_ClearTexture2DArrayRTH);
                m_ClearTexture2DArray = CreateTexture2DArrayFromTexture2D(m_ClearTexture, "Clear Texture2DArray");
                m_ClearTexture2DArrayRTH = RTHandles.Alloc(m_ClearTexture2DArray);

                // Magenta
                RTHandles.Release(m_MagentaTextureRTH);
                m_MagentaTexture = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None) { name = "Magenta Texture" };
                m_MagentaTexture.SetPixel(0, 0, Color.magenta);
                m_MagentaTexture.Apply();
                m_MagentaTextureRTH = RTHandles.Alloc(m_MagentaTexture);
                RTHandles.Release(m_MagentaTexture2DArrayRTH);
                m_MagentaTexture2DArray = CreateTexture2DArrayFromTexture2D(m_MagentaTexture, "Magenta Texture2DArray");
                m_MagentaTexture2DArrayRTH = RTHandles.Alloc(m_MagentaTexture2DArray);

                // Black
                RTHandles.Release(m_BlackTextureRTH);
                m_BlackTexture = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None) { name = "Black Texture" };
                m_BlackTexture.SetPixel(0, 0, Color.black);
                m_BlackTexture.Apply();
                m_BlackTextureRTH = RTHandles.Alloc(m_BlackTexture);
                RTHandles.Release(m_BlackTexture2DArrayRTH);
                m_BlackTexture2DArray = CreateTexture2DArrayFromTexture2D(m_BlackTexture, "Black Texture2DArray");
                m_BlackTexture2DArrayRTH = RTHandles.Alloc(m_BlackTexture2DArray);
                RTHandles.Release(m_BlackTexture3DRTH);
                m_BlackTexture3D = CreateBlackTexture3D("Black Texture3D");
                m_BlackTexture3DRTH = RTHandles.Alloc(m_BlackTexture3D);

                // White
                RTHandles.Release(m_WhiteTextureRTH);
                m_WhiteTextureRTH = RTHandles.Alloc(Texture2D.whiteTexture);
                RTHandles.Release(m_WhiteTexture2DArrayRTH);
                m_WhiteTexture2DArray = CreateTexture2DArrayFromTexture2D(Texture2D.whiteTexture, "White Texture2DArray");
                m_WhiteTexture2DArrayRTH = RTHandles.Alloc(m_WhiteTexture2DArray);
            }
        }

        static Texture2DArray CreateTexture2DArrayFromTexture2D(Texture2D source, string name)
        {
            Texture2DArray texArray = new Texture2DArray(source.width, source.height, slices, source.format, false) { name = name };
            for (int i = 0; i < slices; ++i)
                Graphics.CopyTexture(source, 0, 0, texArray, i, 0);

            return texArray;
        }

        static Texture CreateBlackUIntTextureArray(CommandBuffer cmd, ComputeShader clearR32_UIntShader)
        {
            RenderTexture blackUIntTexture2DArray = new RenderTexture(1, 1, 0, GraphicsFormat.R32_UInt)
            {
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = slices,
                useMipMap = false,
                autoGenerateMips = false,
                enableRandomWrite = true,
                name = "Black UInt Texture Array"
            };

            blackUIntTexture2DArray.Create();

            // Workaround because we currently can't create a Texture2DArray using an R32_UInt format
            // So we create a R32_UInt RenderTarget and clear it using a compute shader, because we can't
            // Clear this type of target on metal devices (output type nor compatible: float4 vs uint)
            int kernel = clearR32_UIntShader.FindKernel("ClearUIntTextureArray");
            cmd.SetComputeTextureParam(clearR32_UIntShader, kernel, "_TargetArray", blackUIntTexture2DArray);
            cmd.DispatchCompute(clearR32_UIntShader, kernel, 1, 1, slices);

            return blackUIntTexture2DArray as Texture;
        }

        static Texture CreateBlackUintTexture(CommandBuffer cmd, ComputeShader clearR32_UIntShader)
        {
            RenderTexture blackUIntTexture2D = new RenderTexture(1, 1, 0, GraphicsFormat.R32_UInt)
            {
                dimension = TextureDimension.Tex2D,
                volumeDepth = slices,
                useMipMap = false,
                autoGenerateMips = false,
                enableRandomWrite = true,
                name = "Black UInt Texture Array"
            };

            blackUIntTexture2D.Create();

            // Workaround because we currently can't create a Texture2DArray using an R32_UInt format
            // So we create a R32_UInt RenderTarget and clear it using a compute shader, because we can't
            // Clear this type of target on metal devices (output type nor compatible: float4 vs uint)
            int kernel = clearR32_UIntShader.FindKernel("ClearUIntTexture");
            cmd.SetComputeTextureParam(clearR32_UIntShader, kernel, "_Target", blackUIntTexture2D);
            cmd.DispatchCompute(clearR32_UIntShader, kernel, 1, 1, slices);

            return blackUIntTexture2D as Texture;
        }

        static Texture3D CreateBlackTexture3D(string name)
        {
            Texture3D texture3D = new Texture3D(width: 1, height: 1, depth: 1, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);
            texture3D.name = name;
            texture3D.SetPixel(0, 0, 0, Color.black, 0);
            texture3D.Apply(updateMipmaps: false);
            return texture3D;
        }
    }
}
