using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Variable rate shading lookup table. Use to convert shading rate fragment size and color back and forth.
    /// </summary>
    [Serializable]
    public class VrsLut
    {
        /// <summary>
        /// Get a new instance of default VrsLut
        /// </summary>
        /// <returns>New instance of default VrsLut</returns>
        public static VrsLut CreateDefault()
        {
            return new VrsLut()
            {
                [ShadingRateFragmentSize.FragmentSize1x1] = new Color(0.785f, 0.23f, 0.20f, 1), // Red
                [ShadingRateFragmentSize.FragmentSize1x2] = new Color(1.00f, 0.80f, 0.80f, 1),  // Light Red
                [ShadingRateFragmentSize.FragmentSize2x1] = new Color(0.40f, 0.20f, 0.20f, 1),  // Dark Red
                [ShadingRateFragmentSize.FragmentSize2x2] = new Color(0.51f, 0.80f, 0.60f, 1),  // Green
                [ShadingRateFragmentSize.FragmentSize1x4] = new Color(0.60f, 0.80f, 1.00f, 1),  // Light Blue
                [ShadingRateFragmentSize.FragmentSize4x1] = new Color(0.20f, 0.40f, 0.60f, 1),  // Medium Blue
                [ShadingRateFragmentSize.FragmentSize2x4] = new Color(0.80f, 1.00f, 0.80f, 1),  // Light Green
                [ShadingRateFragmentSize.FragmentSize4x2] = new Color(0.20f, 0.40f, 0.20f, 1),  // Dark Green
                [ShadingRateFragmentSize.FragmentSize4x4] = new Color(0.125f, 0.22f, 0.36f, 1), // Dark Blue
            };
        }

        [SerializeField]
        Color[] m_Data = new Color[Vrs.shadingRateFragmentSizeCount];

        /// <summary>
        /// Indexing data with ShadingRateFragmentSize enum.
        /// </summary>
        /// <param name="fragmentSize">Shading rate fragment size to set/get</param>
        public Color this[ShadingRateFragmentSize fragmentSize]
        {
            get => m_Data[(int)fragmentSize];
            set => m_Data[(int)fragmentSize] = value;
        }

        /// <summary>
        /// Create a compute buffer from the lookup table.
        /// </summary>
        /// <param name="forVisualization">If true, the buffer will be created with the visualization shader in mind</param>
        /// <returns>Graphics buffer representing this lookup table</returns>
        public GraphicsBuffer CreateBuffer(bool forVisualization = false)
        {
            GraphicsBuffer buffer;
            Color[] bufferData;

            if (forVisualization)
            {
                // lookup table will be used to map shading rate native values to colors
                var fragmentSizes = Enum.GetValues(typeof(ShadingRateFragmentSize));
                // Get the encoded binary value associated of the max shading rate supported by our LUT.
                // The encoded value will not be sequential. For example, 4x4 is encoded as 0b1010 = 10.
                // We do this manually as ShadingRateInfo.QueryNativeValue will return 0 for rates that are
                // not supported, which can lead to overflow on devices that support only up to 2x2.
                var maxNativeValue = MapFragmentShadingRateToBinary(ShadingRateFragmentSize.FragmentSize4x4);

                bufferData = new Color[maxNativeValue + 1];

                for (int i = fragmentSizes.Length - 1; i >= 0; --i)
                {
                    var fragmentSize = (ShadingRateFragmentSize)fragmentSizes.GetValue(i);
                    var nativeValue = ShadingRateInfo.QueryNativeValue(fragmentSize);
                    bufferData[nativeValue] = m_Data[(int) fragmentSize].linear;
                }
            }
            else
            {
                // lookup table will be used to map colors to shading rate index
                bufferData = new Color[m_Data.Length];
                for (int i = 0; i < m_Data.Length; ++i)
                {
                    bufferData[i] = m_Data[i].linear;
                }
            }

            buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bufferData.Length, Marshal.SizeOf(typeof(Color)));
            buffer.SetData(bufferData);

            return buffer;
        }

        private const uint Rate1x = 0;
        private const uint Rate2x = 1;
        private const uint Rate4x = 2;
        private uint MapFragmentShadingRateToBinary(ShadingRateFragmentSize fs)
        {
            switch (fs)
            {
            default:
            case ShadingRateFragmentSize.FragmentSize1x1:
                return EncodeShadingRate(Rate1x, Rate1x);
            case ShadingRateFragmentSize.FragmentSize1x2:
                return EncodeShadingRate(Rate1x, Rate2x);
            case ShadingRateFragmentSize.FragmentSize2x1:
                return EncodeShadingRate(Rate2x, Rate1x);
            case ShadingRateFragmentSize.FragmentSize2x2:
                return EncodeShadingRate(Rate2x, Rate2x);
            case ShadingRateFragmentSize.FragmentSize1x4:
                return EncodeShadingRate(Rate1x, Rate4x);
            case ShadingRateFragmentSize.FragmentSize4x1:
                return EncodeShadingRate(Rate4x, Rate1x);
            case ShadingRateFragmentSize.FragmentSize2x4:
                return EncodeShadingRate(Rate2x, Rate4x);
            case ShadingRateFragmentSize.FragmentSize4x2:
                return EncodeShadingRate(Rate4x, Rate2x);
            case ShadingRateFragmentSize.FragmentSize4x4:
                return EncodeShadingRate(Rate4x, Rate4x);
            }
        }

        private uint EncodeShadingRate(uint x, uint y)
        {
            return ((x << 2) | (y));
        }
    }
}
