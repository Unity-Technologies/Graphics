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

        /// <summary>
        /// This struct Provides access to HDR display settings and information.
        /// </summary>
        public struct HDRDisplayInformation
        {
            /// <summary>
            /// Constructs HDR Display settings.
            /// </summary>
            /// <param name="maxFullFrameToneMapLuminance"></param>
            /// <param name="maxToneMapLuminance"></param>
            /// <param name="minToneMapLuminance"></param>
            /// <param name="hdrPaperWhiteNits"></param>
            public HDRDisplayInformation(int maxFullFrameToneMapLuminance, int maxToneMapLuminance, int minToneMapLuminance, float hdrPaperWhiteNits)
            {
                this.maxFullFrameToneMapLuminance = maxFullFrameToneMapLuminance;
                this.maxToneMapLuminance = maxToneMapLuminance;
                this.minToneMapLuminance = minToneMapLuminance;
                this.paperWhiteNits = hdrPaperWhiteNits;
            }

            /// <summary>Maximum input luminance at which gradation is preserved even when the entire screen is bright. </summary>
            public int maxFullFrameToneMapLuminance;

            /// <summary>Maximum input luminance at which gradation is preserved when 10% of the screen is bright. </summary>
            public int maxToneMapLuminance;

            /// <summary>Minimum input luminance at which gradation is identifiable. </summary>
            public int minToneMapLuminance;

            /// <summary>The base luminance of a white paper surface in nits or candela per square meter. </summary>
            public float paperWhiteNits;
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

            /// <summary>Keyword string to enable when a shader must be aware the input color space is in nits HDR range. </summary>
            public const string HDR_INPUT = "HDR_INPUT";

            /// <summary>Keyword for converting to the correct output color space. </summary>
            internal static readonly ShaderKeyword HDRColorSpaceConversion = new ShaderKeyword(HDR_COLORSPACE_CONVERSION);

            /// <summary>Keyword for applying the color encoding. </summary>
            internal static readonly ShaderKeyword HDREncoding = new ShaderKeyword(HDR_ENCODING);

            /// <summary>Keyword for converting to the correct output color space and applying the color encoding. </summary>
            internal static readonly ShaderKeyword HDRColorSpaceConversionAndEncoding = new ShaderKeyword(HDR_COLORSPACE_CONVERSION_AND_ENCODING);

            /// <summary>Keyword to enable when a shader must be aware the input color space is in nits HDR range. </summary>
            internal static readonly ShaderKeyword HDRInput = new ShaderKeyword(HDR_INPUT);
        }

        static class ShaderPropertyId
        {
            public static readonly int hdrColorSpace = Shader.PropertyToID("_HDRColorspace");
            public static readonly int hdrEncoding = Shader.PropertyToID("_HDREncoding");
        }

        /// <summary>
        /// Extracts the color space part of the ColorGamut
        /// </summary>
        /// <param name="gamut">Color gamut (a combination of color space and encoding) queried from the device.</param>
        /// <param name="colorspace">The HDRColorspace value the color gamut contains as an int.</param>
        /// <returns>Returns true if there was a valid HDRColorspace for the ColorGamut, false otherwise</returns>
        public static bool GetColorSpaceForGamut(ColorGamut gamut, out int colorspace)
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

                case ColorPrimaries.P3:
                    colorspace = (int)HDRColorspace.P3D65;
                    return true;

                default:
                    Debug.LogWarningFormat("{0} color space is currently unsupported for outputting to HDR.", gamut.ToString());
                    colorspace = -1;
                    return false;
            }
        }

        /// <summary>
        /// Extracts the encoding part of the ColorGamut
        /// </summary>
        /// <param name="gamut">Color gamut (a combination of color space and encoding) queried from the device.</param>
        /// <param name="encoding">The HDREncoding value the color gamut contains as an int.</param>
        /// <returns>Returns true if there was a valid HDREncoding for the ColorGamut, false otherwise</returns>
        public static bool GetColorEncodingForGamut(ColorGamut gamut, out int encoding)
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

                case TransferFunction.Gamma22:
                    encoding = (int)HDREncoding.Gamma22;
                    return true;

                case TransferFunction.sRGB:
                    encoding = (int)HDREncoding.sRGB;
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
            int colorSpace;
            int encoding;
            if (!GetColorSpaceForGamut(gamut, out colorSpace) || !GetColorEncodingForGamut(gamut, out encoding))
                return; // only exit here if there is an error or unsupported mode

            material.SetInteger(ShaderPropertyId.hdrColorSpace, colorSpace);
            material.SetInteger(ShaderPropertyId.hdrEncoding, encoding);

            CoreUtils.SetKeyword(material, ShaderKeywords.HDRColorSpaceConversionAndEncoding.name, operations.HasFlag(Operation.ColorConversion) && operations.HasFlag(Operation.ColorEncoding));
            CoreUtils.SetKeyword(material, ShaderKeywords.HDREncoding.name, operations.HasFlag(Operation.ColorEncoding) && !operations.HasFlag(Operation.ColorConversion));
            CoreUtils.SetKeyword(material, ShaderKeywords.HDRColorSpaceConversion.name, operations.HasFlag(Operation.ColorConversion) && !operations.HasFlag(Operation.ColorEncoding));

            // Optimizing shader variants: define HDR_INPUT only if HDR_COLORSPACE_CONVERSION and HDR_ENCODING were not previously defined
            CoreUtils.SetKeyword(material, ShaderKeywords.HDRInput.name, operations == Operation.None);
        }

        /// <summary>
        /// Configures the Material Property Block variables to use HDR output parameters.
        /// </summary>
        /// <param name="properties">The Material Property Block used with HDR output.</param>
        /// <param name="gamut">Color gamut (a combination of color space and encoding) queried from the device.</param>
        public static void ConfigureHDROutput(MaterialPropertyBlock properties, ColorGamut gamut)
        {
            int colorSpace;
            int encoding;
            if (!GetColorSpaceForGamut(gamut, out colorSpace) || !GetColorEncodingForGamut(gamut, out encoding))
                return;

            properties.SetInteger(ShaderPropertyId.hdrColorSpace, colorSpace);
            properties.SetInteger(ShaderPropertyId.hdrEncoding, encoding);
        }

        /// <summary>
        /// Configures the Material keywords to use HDR output parameters.
        /// </summary>
        /// <param name="material">The Material used with HDR output.</param>
        /// <param name="operations">HDR color operations the shader applies.</param>
        public static void ConfigureHDROutput(Material material, Operation operations)
        {
            CoreUtils.SetKeyword(material, ShaderKeywords.HDRColorSpaceConversionAndEncoding.name, operations.HasFlag(Operation.ColorConversion) && operations.HasFlag(Operation.ColorEncoding));
            CoreUtils.SetKeyword(material, ShaderKeywords.HDREncoding.name, operations.HasFlag(Operation.ColorEncoding) && !operations.HasFlag(Operation.ColorConversion));
            CoreUtils.SetKeyword(material, ShaderKeywords.HDRColorSpaceConversion.name, operations.HasFlag(Operation.ColorConversion) && !operations.HasFlag(Operation.ColorEncoding));

            // Optimizing shader variants: define HDR_INPUT only if HDR_COLORSPACE_CONVERSION and HDR_ENCODING were not previously defined
            CoreUtils.SetKeyword(material, ShaderKeywords.HDRInput.name, operations == Operation.None);
        }

        /// <summary>
        /// Configures the compute shader keywords to use HDR output parameters.
        /// </summary>
        /// <param name="computeShader">The compute shader used with HDR output.</param>
        /// <param name="gamut">Color gamut (a combination of color space and encoding) queried from the device.</param>
        /// <param name="operations">HDR color operations the shader applies.</param>
        public static void ConfigureHDROutput(ComputeShader computeShader, ColorGamut gamut, Operation operations)
        {
            int colorSpace;
            int encoding;
            if (!GetColorSpaceForGamut(gamut, out colorSpace) || !GetColorEncodingForGamut(gamut, out encoding))
                return; // only exit here if there is an error or unsupported mode

            computeShader.SetInt(ShaderPropertyId.hdrColorSpace, colorSpace);
            computeShader.SetInt(ShaderPropertyId.hdrEncoding, encoding);

            CoreUtils.SetKeyword(computeShader, ShaderKeywords.HDRColorSpaceConversionAndEncoding.name, operations.HasFlag(Operation.ColorConversion) && operations.HasFlag(Operation.ColorEncoding));
            CoreUtils.SetKeyword(computeShader, ShaderKeywords.HDREncoding.name, operations.HasFlag(Operation.ColorEncoding) && !operations.HasFlag(Operation.ColorConversion));
            CoreUtils.SetKeyword(computeShader, ShaderKeywords.HDRColorSpaceConversion.name, operations.HasFlag(Operation.ColorConversion) && !operations.HasFlag(Operation.ColorEncoding));

            // Optimizing shader variants: define HDR_INPUT only if HDR_COLORSPACE_CONVERSION and HDR_ENCODING were not previously defined
            CoreUtils.SetKeyword(computeShader, ShaderKeywords.HDRInput.name, operations == Operation.None);
        }

        /// <summary>
        /// Returns true if the given set of keywords is valid for HDR output.
        /// </summary>
        /// <param name="shaderKeywordSet">Shader keywords combination that represents a shader variant.</param>
        /// <param name="isHDREnabled">Whether HDR output shader variants are required.</param>
        /// <returns>True if the shader variant is valid and should not be stripped.</returns>
        public static bool IsShaderVariantValid(ShaderKeywordSet shaderKeywordSet, bool isHDREnabled)
        {
            bool hasHDRKeywords = shaderKeywordSet.IsEnabled(ShaderKeywords.HDREncoding) || shaderKeywordSet.IsEnabled(ShaderKeywords.HDRColorSpaceConversion) || shaderKeywordSet.IsEnabled(ShaderKeywords.HDRColorSpaceConversionAndEncoding) || shaderKeywordSet.IsEnabled(ShaderKeywords.HDRInput);
            
            // If we don't plan to enable HDR, remove all HDR Output variants
            if (!isHDREnabled && hasHDRKeywords)
                return false;

            return true;
        }
    }
}
