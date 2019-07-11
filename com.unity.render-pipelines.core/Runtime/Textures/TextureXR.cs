using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    public static class TextureXR
    {
        // Property set by XRSystem
        private static int m_MaxViews = 1;
        public static int maxViews
        {
            set
            {
                //if (value > HighDefinition.ShaderConfig.s_XrMaxViews)
                //    throw new System.NotImplementedException("Invalid XR setup for single-pass instancing: you must increase ShaderConfig.XrMaxViews");
                //else
                    m_MaxViews = value;
            }
        }

        // Property accessed when allocating a render target
        public static int slices { get => m_MaxViews; }

        // Must be in sync with shader define in TextureXR.hlsl
        public static bool useTexArray
        {
            get
            {
                switch (SystemInfo.graphicsDeviceType)
                {
                    case GraphicsDeviceType.Direct3D11:
                    case GraphicsDeviceType.Direct3D12:
                        return SystemInfo.graphicsDeviceType != GraphicsDeviceType.XboxOne;

                    case GraphicsDeviceType.PlayStation4:
                        return true;

                    case GraphicsDeviceType.Vulkan:
                        return true;
                }

                return false;
            }
        }

        public static TextureDimension dimension
        {
            get
            {
                // TEXTURE2D_X macros will now expand to TEXTURE2D or TEXTURE2D_ARRAY
                return useTexArray ? TextureDimension.Tex2DArray : TextureDimension.Tex2D;
            }
        }

        // Need to keep both the Texture and the RTHandle in order to be able to track lifetime properly.
        static Texture      m_BlackUIntTexture2DArray;
        static Texture      m_BlackUIntTexture;
        static RTHandle     m_BlackUIntTexture2DArrayRTH;
        static RTHandle     m_BlackUIntTextureRTH;
        public static RTHandle  GetBlackUIntTexture() { return useTexArray ? m_BlackUIntTexture2DArrayRTH : m_BlackUIntTextureRTH; }

        static Texture2DArray   m_ClearTexture2DArray;
        static Texture2D        m_ClearTexture;
        static RTHandle         m_ClearTexture2DArrayRTH;
        static RTHandle         m_ClearTextureRTH;
        public static RTHandle GetClearTexture() { return useTexArray ? m_ClearTexture2DArrayRTH : m_ClearTextureRTH; }

        static Texture2DArray   m_MagentaTexture2DArray;
        static Texture2D        m_MagentaTexture;
        static RTHandle         m_MagentaTexture2DArrayRTH;
        static RTHandle         m_MagentaTextureRTH;
        public static RTHandle GetMagentaTexture() { return useTexArray ? m_MagentaTexture2DArrayRTH : m_MagentaTextureRTH; }

        static Texture2DArray   m_BlackTexture2DArray;
        static RTHandle         m_BlackTexture2DArrayRTH;
        static RTHandle         m_BlackTextureRTH;
        public static RTHandle GetBlackTexture() { return useTexArray ? m_BlackTexture2DArrayRTH : m_BlackTextureRTH; }

        static Texture2DArray   m_WhiteTexture2DArray;
        static RTHandle         m_WhiteTexture2DArrayRTH;
        static RTHandle         m_WhiteTextureRTH;
        public static RTHandle GetWhiteTexture() { return useTexArray ? m_WhiteTexture2DArrayRTH : m_WhiteTextureRTH; }

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
                m_ClearTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false) { name = "Clear Texture" };
                m_ClearTexture.SetPixel(0, 0, Color.clear);
                m_ClearTexture.Apply();
                m_ClearTextureRTH = RTHandles.Alloc(m_ClearTexture);
                RTHandles.Release(m_ClearTexture2DArrayRTH);
                m_ClearTexture2DArray = CreateTexture2DArrayFromTexture2D(m_ClearTexture, "Clear Texture2DArray");
                m_ClearTexture2DArrayRTH = RTHandles.Alloc(m_ClearTexture2DArray);

                // Magenta
                RTHandles.Release(m_MagentaTextureRTH);
                m_MagentaTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false) { name = "Magenta Texture" };
                m_MagentaTexture.SetPixel(0, 0, Color.magenta);
                m_MagentaTexture.Apply();
                m_MagentaTextureRTH = RTHandles.Alloc(m_MagentaTexture);
                RTHandles.Release(m_MagentaTexture2DArrayRTH);
                m_MagentaTexture2DArray = CreateTexture2DArrayFromTexture2D(m_MagentaTexture, "Magenta Texture2DArray");
                m_MagentaTexture2DArrayRTH = RTHandles.Alloc(m_MagentaTexture2DArray);

                // Black
                RTHandles.Release(m_BlackTextureRTH);
                m_BlackTextureRTH = RTHandles.Alloc(Texture2D.blackTexture);
                RTHandles.Release(m_BlackTexture2DArrayRTH);
                m_BlackTexture2DArray = CreateTexture2DArrayFromTexture2D(Texture2D.blackTexture, "Black Texture2DArray");
                m_BlackTexture2DArrayRTH = RTHandles.Alloc(m_BlackTexture2DArray);

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
    }
}
