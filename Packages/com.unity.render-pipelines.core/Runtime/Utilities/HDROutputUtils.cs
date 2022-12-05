using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Utility class for outputting to an HDR display.
    /// </summary>
    public static class HDROutputUtils
    {
        /// <summary> HDR color operations that the shader applies. </summary>
        [Flags]
        public enum Operation
        {
            /// <summary> Do not perform operations specific to HDR output. </summary>
            None = 0,
            /// <summary> Convert colors to the color space of the HDR display. </summary>
            ColorConversion = 1 << 0,
            /// <summary> Encode colors with the transfer function corresponding to the HDR display. </summary>
            ColorEncoding = 1 << 1
        }

        /// <summary>Shader keywords for communicating with the HDR Output shader implementation.</summary>
        static class ShaderKeywords
        {
            /// <summary>Keyword for converting to the correct output color space. </summary>
            public static readonly ShaderKeyword HDRColorSpaceConversion = new ShaderKeyword("HDR_COLORSPACE_CONVERSION");

            /// <summary>Keyword for the Rec.709 color space (Rec.709 color primaries, D65 white point).</summary>
            public static readonly ShaderKeyword HDRColorSpaceRec709 = new ShaderKeyword("HDR_COLORSPACE_REC709");

            /// <summary>Keyword for the Rec.2020 color space (Rec.2020 color primaries, D65 white point). </summary>
            public static readonly ShaderKeyword HDRColorSpaceRec2020 = new ShaderKeyword("HDR_COLORSPACE_REC2020");

            /// <summary>Keyword for the linear encoding (1 = SDR reference white nits).</summary>
            public static readonly ShaderKeyword HDREncodingLinear = new ShaderKeyword("HDR_ENCODING_LINEAR");

            /// <summary>Keyword for the ST 2084 PQ encoding.</summary>
            public static readonly ShaderKeyword HDREncodingPQ = new ShaderKeyword("HDR_ENCODING_PQ");
        }

        private static bool GetColorSpaceKeyword(ColorGamut gamut, out string keyword)
        {
            ColorPrimaries primaries = ColorGamutUtility.GetColorPrimaries(gamut);
            switch (primaries)
            {
                case ColorPrimaries.Rec709:
                    keyword = ShaderKeywords.HDRColorSpaceRec709.name;
                    return true;

                case ColorPrimaries.Rec2020:
                    keyword = ShaderKeywords.HDRColorSpaceRec2020.name;
                    return true;

                default:
                    Debug.LogWarningFormat("{0} color space is currently unsupported for outputting to HDR.", gamut.ToString());
                    keyword = null;
                    return false;
            }
        }

        private static bool GetColorEncodingKeyword(ColorGamut gamut, out string keyword)
        {
            TransferFunction transferFunction = ColorGamutUtility.GetTransferFunction(gamut);
            switch (transferFunction)
            {
                case TransferFunction.Linear:
                    keyword = ShaderKeywords.HDREncodingLinear.name;
                    return true;

                case TransferFunction.PQ:
                    keyword = ShaderKeywords.HDREncodingPQ.name;
                    return true;

                default:
                    Debug.LogWarningFormat("{0} color encoding is currently unsupported for outputting to HDR.", gamut.ToString());
                    keyword = null;
                    return false;
            }
        }

        /// <summary>
        /// Configures the Material keywords to use HDR output parameters.
        /// </summary>
        /// <param name="material">The Material used with HDR output.</param>
        /// <param name="gamut">Color gamut (a combination of color space and encoding) queried from the device.</param>
        /// <param name="operations">HDR color operations the shader applies.</param>
        public static void ConfigureHDROutput(Material material, ColorGamut gamut, Operation operations)
        {
            if (operations == Operation.None)
                return;

            string colorSpaceKeyword;
            bool hasValidColorSpace = GetColorSpaceKeyword(gamut, out colorSpaceKeyword);
            if (!hasValidColorSpace)
                return;

            if (operations.HasFlag(Operation.ColorConversion))
            {
                material.EnableKeyword(ShaderKeywords.HDRColorSpaceConversion.name);
                material.EnableKeyword(colorSpaceKeyword);
            }

            if (operations.HasFlag(Operation.ColorEncoding))
            {
                string encodingKeyword;
                if (GetColorEncodingKeyword(gamut, out encodingKeyword))
                {
                    material.EnableKeyword(encodingKeyword);
                    material.EnableKeyword(colorSpaceKeyword);
                }
            }
        }

        /// <summary>
        /// Configures the compute shader keywords to use HDR output parameters.
        /// </summary>
        /// <param name="computeShader">The compute shader used with HDR output.</param>
        /// <param name="gamut"> Color gamut (a combination of color space and encoding) queried from the device.</param>
        /// <param name="operations">HDR color operations the shader applies.</param>
        public static void ConfigureHDROutput(ComputeShader computeShader, ColorGamut gamut, Operation operations)
        {
            if (operations == Operation.None)
                return;

            string colorSpaceKeyword;
            bool hasValidColorSpace = GetColorSpaceKeyword(gamut, out colorSpaceKeyword);
            if (!hasValidColorSpace)
                return;

            if (operations.HasFlag(Operation.ColorConversion))
            {
                computeShader.EnableKeyword(ShaderKeywords.HDRColorSpaceConversion.name);
                computeShader.EnableKeyword(colorSpaceKeyword);
            }

            if (operations.HasFlag(Operation.ColorEncoding))
            {
                string encodingKeyword;
                if (GetColorEncodingKeyword(gamut, out encodingKeyword))
                {
                    computeShader.EnableKeyword(encodingKeyword);
                    computeShader.EnableKeyword(colorSpaceKeyword);
                }
            }
        }

        /// <summary>
        /// Returns true if the given set of keywords is valid for HDR output.
        /// </summary>
        /// <param name="shaderKeywordSet">Shader keywords combination that represents a shader variant.</param>
        /// <param name="isHDREnabled">Whether HDR output shader variants are required.</param>
        /// <returns>True if the shader variant is valid and should not be stripped.</returns>
        public static bool IsShaderVariantValid(ShaderKeywordSet shaderKeywordSet, bool isHDREnabled)
        {
            bool isHDREncoding = shaderKeywordSet.IsEnabled(ShaderKeywords.HDREncodingLinear) || shaderKeywordSet.IsEnabled(ShaderKeywords.HDREncodingPQ);
            bool isHDRConversion = shaderKeywordSet.IsEnabled(ShaderKeywords.HDRColorSpaceConversion);
            bool hasColorSpaceKeyword = shaderKeywordSet.IsEnabled(ShaderKeywords.HDRColorSpaceRec2020) || shaderKeywordSet.IsEnabled(ShaderKeywords.HDRColorSpaceRec709);

            // If we don't plan to enable HDR, remove all HDR Output variants
            if (!isHDREnabled && (isHDRConversion || isHDREncoding || hasColorSpaceKeyword))
                return false;

            // HDR color encoding or conversion must specify an HDR colorspace keyword
            // And an HDR colorspace keyword must specify either HDR color encoding or conversion
            bool isValidShader = (isHDREncoding || isHDRConversion) == hasColorSpaceKeyword;
            if (!isValidShader)
                return false;

            return true;
        }
    }
}
