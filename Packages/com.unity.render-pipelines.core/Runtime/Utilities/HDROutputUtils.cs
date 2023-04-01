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
        public static class ShaderKeywords
        {
            /// <summary>Keyword string for converting to the correct output color space. </summary>
            public const string HDR_COLORSPACE_CONVERSION = "HDR_COLORSPACE_CONVERSION";

            /// <summary>Keyword string for applying the color encoding. </summary>
            public const string HDR_ENCODING = "HDR_ENCODING";
            
            /// <summary>Keyword string for converting to the correct output color space and applying the color encoding. </summary>
            public const string HDR_COLORSPACE_CONVERSION_AND_ENCODING = "HDR_COLORSPACE_CONVERSION_AND_ENCODING";
            
            /// <summary>Keyword for converting to the correct output color space. </summary>
            internal static readonly ShaderKeyword HDRColorSpaceConversion = new ShaderKeyword(HDR_COLORSPACE_CONVERSION);
            
            /// <summary>Keyword for applying the color encoding. </summary>
            internal static readonly ShaderKeyword HDREncoding = new ShaderKeyword(HDR_ENCODING);
            
            /// <summary>Keyword for converting to the correct output color space and applying the color encoding. </summary>
            internal static readonly ShaderKeyword HDRColorSpaceConversionAndEncoding = new ShaderKeyword(HDR_COLORSPACE_CONVERSION_AND_ENCODING);
        }

        static class ShaderPropertyId
        {
            public static readonly int hdrColorSpace = Shader.PropertyToID("_HDRColorspace");
            public static readonly int hdrEncoding = Shader.PropertyToID("_HDREncoding");
        }

        private static bool GetColorSpaceForGamut(ColorGamut gamut, out int colorspace)
        {
            WhitePoint whitePoint = ColorGamutUtility.GetWhitePoint(gamut);
            if (whitePoint != WhitePoint.D65)
            {
                Debug.LogWarningFormat("{0} white point is currently unsupported for outputting to HDR.", gamut.ToString());
                colorspace = -1;
                return false;
            }

            ColorPrimaries primaries = ColorGamutUtility.GetColorPrimaries(gamut);
            switch (primaries)
            {
                case ColorPrimaries.Rec709:
                    colorspace = (int)HDRColorspace.Rec709;
                    return true;

                case ColorPrimaries.Rec2020:
                    colorspace = (int)HDRColorspace.Rec2020;
                    return true;

                default:
                    Debug.LogWarningFormat("{0} color space is currently unsupported for outputting to HDR.", gamut.ToString());
                    colorspace = -1;
                    return false;
            }
        }

        private static bool GetColorEncodingForGamut(ColorGamut gamut, out int encoding)
        {
            TransferFunction transferFunction = ColorGamutUtility.GetTransferFunction(gamut);
            switch (transferFunction)
            {
                case TransferFunction.Linear:
                    encoding = (int)HDREncoding.Linear;
                    return true;

                case TransferFunction.PQ:
                    encoding = (int)HDREncoding.PQ;
                    return true;

                default:
                    Debug.LogWarningFormat("{0} color encoding is currently unsupported for outputting to HDR.", gamut.ToString());
                    encoding = -1;
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

            int colorSpace;
            int encoding;
            if (!GetColorSpaceForGamut(gamut, out colorSpace) || !GetColorEncodingForGamut(gamut, out encoding))
                return;

            material.SetInteger(ShaderPropertyId.hdrColorSpace, colorSpace);
            material.SetInteger(ShaderPropertyId.hdrEncoding, encoding);

            if (operations.HasFlag(Operation.ColorConversion) && operations.HasFlag(Operation.ColorEncoding))
            {
                material.EnableKeyword(ShaderKeywords.HDRColorSpaceConversionAndEncoding.name);
            }
            else if (operations.HasFlag(Operation.ColorEncoding))
            {
                material.EnableKeyword(ShaderKeywords.HDREncoding.name);
            }
            else if (operations.HasFlag(Operation.ColorConversion))
            {
                material.EnableKeyword(ShaderKeywords.HDRColorSpaceConversion.name);
            }
        }

        /// <summary>
        /// Configures the compute shader keywords to use HDR output parameters.
        /// </summary>
        /// <param name="computeShader">The compute shader used with HDR output.</param>
        /// <param name="gamut">Color gamut (a combination of color space and encoding) queried from the device.</param>
        /// <param name="operations">HDR color operations the shader applies.</param>
        public static void ConfigureHDROutput(ComputeShader computeShader, ColorGamut gamut, Operation operations)
        {
            if (operations == Operation.None)
                return;

            int colorSpace;
            int encoding;
            if (!GetColorSpaceForGamut(gamut, out colorSpace) || !GetColorEncodingForGamut(gamut, out encoding))
                return;

            computeShader.SetInt(ShaderPropertyId.hdrColorSpace, colorSpace);
            computeShader.SetInt(ShaderPropertyId.hdrEncoding, encoding);

            if (operations.HasFlag(Operation.ColorConversion) && operations.HasFlag(Operation.ColorEncoding))
            {
                computeShader.EnableKeyword(ShaderKeywords.HDRColorSpaceConversionAndEncoding.name);
            }
            else if (operations.HasFlag(Operation.ColorEncoding))
            {
                computeShader.EnableKeyword(ShaderKeywords.HDREncoding.name);
            }
            else if (operations.HasFlag(Operation.ColorConversion))
            {
                computeShader.EnableKeyword(ShaderKeywords.HDRColorSpaceConversion.name);
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
            bool hasHDRKeywords = shaderKeywordSet.IsEnabled(ShaderKeywords.HDREncoding) || shaderKeywordSet.IsEnabled(ShaderKeywords.HDRColorSpaceConversion) || shaderKeywordSet.IsEnabled(ShaderKeywords.HDRColorSpaceConversionAndEncoding);
            
            // If we don't plan to enable HDR, remove all HDR Output variants
            if (!isHDREnabled && hasHDRKeywords)
                return false;

            return true;
        }
    }
}
