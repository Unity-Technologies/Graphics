using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    public static class TextureXR
    {
        // Limit memory usage of default textures
        const int kMaxSliceCount = 2;

        // Must be in sync with shader define in TextureXR.hlsl
        public static bool useTexArray
        {
            get
            {
                // XRTODO: Vulkan, PSVR, Mac with metal only for OS 10.14+, etc
                switch (SystemInfo.graphicsDeviceType)
                {
                    // XRTODO: disabled until all SPI code is merged
                    case GraphicsDeviceType.Direct3D11:
                        return false;
                }

                return false;
            }
        }

        public static VRTextureUsage OverrideRenderTexture(bool xrInstancing, ref TextureDimension dimension, ref int slices)
        {
            if (xrInstancing && useTexArray)
            {
                // TEXTURE2D_X macros will now expand to TEXTURE2D_ARRAY
                dimension = TextureDimension.Tex2DArray;

                // XRTODO: need to also check if stereo is enabled in camera!
                if (XRGraphics.stereoRenderingMode == XRGraphics.StereoRenderingMode.SinglePassInstanced)
                {
                    // Add a new dimension
                    slices = slices * XRGraphics.eyeCount;

                    // XRTODO: useful? if yes, add validation, asserts
                    return XRGraphics.eyeTextureDesc.vrUsage;
                }
            }

            return VRTextureUsage.None;
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

        public static Texture GetWhiteTexture()
        {
            if (useTexArray)
                return whiteTexture2DArray;

            return Texture2D.whiteTexture;
        }

        private static Texture2DArray CreateTexture2DArrayFromTexture2D(Texture2D source, string name)
        {
            Texture2DArray texArray = new Texture2DArray(source.width, source.height, kMaxSliceCount, source.format, false) { name = name };
            for (int i = 0; i < kMaxSliceCount; ++i)
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

        static Texture2DArray m_ClearTexture2DArray;
        public static Texture2DArray clearTexture2DArray
        {
            get
            {
                if (m_ClearTexture2DArray == null)
                    m_ClearTexture2DArray = CreateTexture2DArrayFromTexture2D(clearTexture, "Clear Texture2DArray");

                Debug.Assert(XRGraphics.eyeCount <= m_ClearTexture2DArray.depth);
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

                Debug.Assert(XRGraphics.eyeCount <= m_BlackTexture2DArray.depth);
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

                Debug.Assert(XRGraphics.eyeCount <= m_WhiteTexture2DArray.depth);
                return m_WhiteTexture2DArray;
            }
        }
    }
}
