using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    public static class TextureXR
    {
        // Limit memory usage of default textures
        public const int kMaxSlices = 2;

        // Property set by XRSystem
        public static int maxViews { get; set; } = 1;

        // Property accessed when allocating a render texture
        public static int slices { get => maxViews; }

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

        public static Texture GetClearTexture()
        {
            if (useTexArray)
                return clearTexture2DArray;

            return clearTexture;
        }

        public static Texture GetBlackTexture()
        {
            if (useTexArray)
                return blackTexture2DArray;

            return Texture2D.blackTexture;
        }

        public static Texture GetBlackUIntTexture()
        {
            if (useTexArray)
                return blackUIntTexture2DArray;

            return blackUIntTexture;
        }

        public static Texture GetWhiteTexture()
        {
            if (useTexArray)
                return whiteTexture2DArray;

            return Texture2D.whiteTexture;
        }

        private static Texture2DArray CreateTexture2DArrayFromTexture2D(Texture2D source, string name)
        {
            Texture2DArray texArray = new Texture2DArray(source.width, source.height, kMaxSlices, source.format, false) { name = name };
            for (int i = 0; i < kMaxSlices; ++i)
                Graphics.CopyTexture(source, 0, 0, texArray, i, 0);

            return texArray;
        }

        private static Texture2D m_ClearTexture;
        private static Texture2D clearTexture
        {
            get
            {
                if (m_ClearTexture == null)
                {
                    m_ClearTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false) { name = "Clear Texture" };
                    m_ClearTexture.SetPixel(0, 0, Color.clear);
                    m_ClearTexture.Apply();
                }

                return m_ClearTexture;
            }
        }

        private static Texture m_BlackUIntTexture;
        private static Texture blackUIntTexture
        {
            get
            {
                if (m_BlackUIntTexture == null)
                {
                    // Uint textures can't be used in Sampling operations so we can't use the Texture2D class because
                    // it assumes that we will use the texture for sampling operations and crash because of invalid format.
                    m_BlackUIntTexture = new RenderTexture(1, 1, 0, GraphicsFormat.R32_UInt) { name = "Black UInt Texture" };
                    Graphics.Blit(Texture2D.blackTexture, m_BlackUIntTexture as RenderTexture);
                }

                return m_BlackUIntTexture;
            }
        }

        static Texture2DArray m_ClearTexture2DArray;
        public static Texture2DArray clearTexture2DArray
        {
            get
            {
                if (m_ClearTexture2DArray == null)
                    m_ClearTexture2DArray = CreateTexture2DArrayFromTexture2D(clearTexture, "Clear Texture2DArray");

                return m_ClearTexture2DArray;
            }
        }

        static Texture2DArray m_BlackTexture2DArray;
        public static Texture2DArray blackTexture2DArray
        {
            get
            {
                if (m_BlackTexture2DArray == null)
                    m_BlackTexture2DArray = CreateTexture2DArrayFromTexture2D(Texture2D.blackTexture, "Black Texture2DArray");

                return m_BlackTexture2DArray;
            }
        }

        static Texture2DArray m_WhiteTexture2DArray;
        public static Texture2DArray whiteTexture2DArray
        {
            get
            {
                if (m_WhiteTexture2DArray == null)
                    m_WhiteTexture2DArray = CreateTexture2DArrayFromTexture2D(Texture2D.whiteTexture, "White Texture2DArray");

                return m_WhiteTexture2DArray;
            }
        }

        static Texture m_BlackUIntTexture2DArray;
        public static Texture blackUIntTexture2DArray
        {
            get
            {
                if (m_BlackUIntTexture2DArray == null)
                {
                    // Uint textures can't be used in Sampling operations so we can't use the Texture2DArray class because
                    // it assumes that we will use the texture for sampling operations and crash because of invalid format.
                    m_BlackUIntTexture2DArray = new RenderTexture(1, 1, 0, GraphicsFormat.R32_UInt)
                    {
                        dimension = TextureDimension.Tex2DArray,
                        volumeDepth = kMaxSlices,
                        useMipMap = false,
                        autoGenerateMips = false,
                        enableRandomWrite = true,
                        name = "Black UInt Texture Array"
                    };

                    // Can't use CreateTexture2DArrayFromTexture2D here because we need to create the texture using GraphicsFormat
                    for (int i = 0; i < kMaxSlices; ++i)
                        Graphics.Blit(blackTexture2DArray, m_BlackUIntTexture2DArray as RenderTexture, i, i);
                }

                return m_BlackUIntTexture2DArray;
            }
        }
    }
}
