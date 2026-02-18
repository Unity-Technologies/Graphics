using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Utility class providing default textures compatible in any XR setup.
    /// </summary>
    public static class TextureXR
    {
        // Property set by XRSystem
        private static int s_MaxViews = 1;
        /// <summary>
        /// Maximum number of views handled by the XR system.
        /// </summary>
        public static int maxViews
        {
            set
            {
                s_MaxViews = value;
            }
        }

        // Property accessed when allocating a render target
        /// <summary>
        /// Number of slices used by the XR system.
        /// </summary>
        public static int slices { get => s_MaxViews; }

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
                    case GraphicsDeviceType.OpenGLES3:
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
        static Texture s_BlackUIntTexture2DArray;
        static Texture s_BlackUIntTexture;
        static RTHandle s_BlackUIntTexture2DArrayRTH;
        static RTHandle s_BlackUIntTextureRTH;
        /// <summary>
        /// Default black unsigned integer texture.
        /// </summary>
        /// <returns>The default black unsigned integer texture.</returns>
        public static RTHandle GetBlackUIntTexture() { return useTexArray ? s_BlackUIntTexture2DArrayRTH : s_BlackUIntTextureRTH; }

        static Texture2DArray s_ClearTexture2DArray;
        static Texture2D s_ClearTexture;
        static RTHandle s_ClearTexture2DArrayRTH;
        static RTHandle s_ClearTextureRTH;
        /// <summary>
        /// Default clear color (0, 0, 0, 1) texture.
        /// </summary>
        /// <returns>The default clear color texture.</returns>
        public static RTHandle GetClearTexture() { return useTexArray ? s_ClearTexture2DArrayRTH : s_ClearTextureRTH; }

        static Texture2DArray s_MagentaTexture2DArray;
        static Texture2D s_MagentaTexture;
        static RTHandle s_MagentaTexture2DArrayRTH;
        static RTHandle s_MagentaTextureRTH;
        /// <summary>
        /// Default magenta texture.
        /// </summary>
        /// <returns>The default magenta texture.</returns>
        public static RTHandle GetMagentaTexture() { return useTexArray ? s_MagentaTexture2DArrayRTH : s_MagentaTextureRTH; }

        static Texture2D s_BlackTexture;
        static Texture3D s_BlackTexture3D;
        static Texture2DArray s_BlackTexture2DArray;
        static RTHandle s_BlackTexture2DArrayRTH;
        static RTHandle s_BlackTextureRTH;
        static RTHandle s_BlackTexture3DRTH;
        /// <summary>
        /// Default black texture.
        /// </summary>
        /// <returns>The default black texture.</returns>
        public static RTHandle GetBlackTexture() { return useTexArray ? s_BlackTexture2DArrayRTH : s_BlackTextureRTH; }
        /// <summary>
        /// Default black texture array.
        /// </summary>
        /// <returns>The default black texture array.</returns>
        public static RTHandle GetBlackTextureArray() { return s_BlackTexture2DArrayRTH; }
        /// <summary>
        /// Default black texture 3D.
        /// </summary>
        /// <returns>The default black texture 3D.</returns>
        public static RTHandle GetBlackTexture3D() { return s_BlackTexture3DRTH; }

        static Texture2DArray s_WhiteTexture2DArray;
        static RTHandle s_WhiteTexture2DArrayRTH;
        static RTHandle s_WhiteTextureRTH;
        /// <summary>
        /// Default white texture.
        /// </summary>
        /// <returns>The default white texture.</returns>
        public static RTHandle GetWhiteTexture() { return useTexArray ? s_WhiteTexture2DArrayRTH : s_WhiteTextureRTH; }

        /// <summary>
        /// Is TextureXR initialized?
        /// </summary>
        public static bool initialized => s_BlackUIntTexture2DArray != null; // We assume that everything is valid if one is valid.

        /// <summary>
        /// Initialize XR textures. Must be called at least once.
        /// </summary>
        /// <param name="cmd">Command Buffer used to initialize textures.</param>
        /// <param name="clearR32_UIntShader">Compute shader used to intitialize unsigned integer textures.</param>
        public static void Initialize(CommandBuffer cmd, ComputeShader clearR32_UIntShader)
        {
            if (initialized)
                return;
            
            // Black UINT
            s_BlackUIntTexture2DArray = CreateBlackUIntTextureArray(cmd, clearR32_UIntShader);
            s_BlackUIntTexture2DArrayRTH = RTHandles.Alloc(s_BlackUIntTexture2DArray);
            s_BlackUIntTexture = CreateBlackUintTexture(cmd, clearR32_UIntShader);
            s_BlackUIntTextureRTH = RTHandles.Alloc(s_BlackUIntTexture);

            // Clear
            s_ClearTexture = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None) { name = "Clear Texture" };
            s_ClearTexture.SetPixel(0, 0, Color.clear);
            s_ClearTexture.Apply();
            s_ClearTextureRTH = RTHandles.Alloc(s_ClearTexture);
            s_ClearTexture2DArray = CreateTexture2DArrayFromTexture2D(s_ClearTexture, "Clear Texture2DArray");
            s_ClearTexture2DArrayRTH = RTHandles.Alloc(s_ClearTexture2DArray);

            // Magenta
            s_MagentaTexture = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None) { name = "Magenta Texture" };
            s_MagentaTexture.SetPixel(0, 0, Color.magenta);
            s_MagentaTexture.Apply();
            s_MagentaTextureRTH = RTHandles.Alloc(s_MagentaTexture);
            s_MagentaTexture2DArray = CreateTexture2DArrayFromTexture2D(s_MagentaTexture, "Magenta Texture2DArray");
            s_MagentaTexture2DArrayRTH = RTHandles.Alloc(s_MagentaTexture2DArray);

            // Black
            s_BlackTexture = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None) { name = "Black Texture" };
            s_BlackTexture.SetPixel(0, 0, Color.black);
            s_BlackTexture.Apply();
            s_BlackTextureRTH = RTHandles.Alloc(s_BlackTexture);
            s_BlackTexture2DArray = CreateTexture2DArrayFromTexture2D(s_BlackTexture, "Black Texture2DArray");
            s_BlackTexture2DArrayRTH = RTHandles.Alloc(s_BlackTexture2DArray);
            s_BlackTexture3D = CreateBlackTexture3D("Black Texture3D");
            s_BlackTexture3DRTH = RTHandles.Alloc(s_BlackTexture3D);

            // White
            s_WhiteTextureRTH = RTHandles.Alloc(Texture2D.whiteTexture);
            s_WhiteTexture2DArray = CreateTexture2DArrayFromTexture2D(Texture2D.whiteTexture, "White Texture2DArray");
            s_WhiteTexture2DArrayRTH = RTHandles.Alloc(s_WhiteTexture2DArray);

            //Auto cleanup if pipeline type is changed as all pipelines may not rely on TextureXR
            //By time PipelineChange is raised though, it may be too late. So do it at each pipeline disposal instead.
            RenderPipelineManager.activeRenderPipelineDisposed += Cleanup; 
        }

        static void Cleanup()
        {
            if (!initialized)
                return;
            
            // Black UINT
            RTHandles.Release(s_BlackUIntTexture2DArrayRTH);
            s_BlackUIntTexture2DArray = null;
            RTHandles.Release(s_BlackUIntTextureRTH);
            s_BlackUIntTexture = null;
            
            // Clear
            RTHandles.Release(s_ClearTextureRTH);
            s_ClearTexture = null;
            RTHandles.Release(s_ClearTexture2DArrayRTH);
            s_ClearTexture2DArray = null;
            
            // Magenta
            RTHandles.Release(s_MagentaTextureRTH);
            s_MagentaTexture = null;
            RTHandles.Release(s_MagentaTexture2DArrayRTH);
            s_MagentaTexture2DArray = null;
            
            // Black
            RTHandles.Release(s_BlackTextureRTH);
            s_BlackTexture = null;
            RTHandles.Release(s_BlackTexture2DArrayRTH);
            s_BlackTexture2DArray = null;
            RTHandles.Release(s_BlackTexture3DRTH);
            s_BlackTexture3D = null;
            
            // White
            RTHandles.Release(s_WhiteTextureRTH);
            s_WhiteTextureRTH = null;
            RTHandles.Release(s_WhiteTexture2DArrayRTH);
            s_WhiteTexture2DArray = null;

            RenderPipelineManager.activeRenderPipelineDisposed -= Cleanup;
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
                volumeDepth = 1,
                useMipMap = false,
                autoGenerateMips = false,
                enableRandomWrite = true,
                name = "Black UInt Texture"
            };

            blackUIntTexture2D.Create();

            // Workaround because we currently can't create a Texture2D using an R32_UInt format
            // So we create a R32_UInt RenderTarget and clear it using a compute shader, because we can't
            // Clear this type of target on metal devices (output type nor compatible: float4 vs uint)
            int kernel = clearR32_UIntShader.FindKernel("ClearUIntTexture");
            cmd.SetComputeTextureParam(clearR32_UIntShader, kernel, "_Target", blackUIntTexture2D);
            cmd.DispatchCompute(clearR32_UIntShader, kernel, 1, 1, 1);

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
