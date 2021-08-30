namespace UnityEngine.Rendering
{
    // Has to be kept in sync with PhysicalCamera.hlsl

    /// <summary>
    /// A set of color manipulation utilities.
    /// </summary>
    public static class ColorUtils
    {
        /// <summary>
        /// Calibration constant (K) used for our virtual reflected light meter. Modifying this will lead to a change on how average scene luminance
        /// gets mapped to exposure.
        /// </summary>
        static public float s_LightMeterCalibrationConstant = 12.5f;

        /// <summary>
        /// Factor used for our lens system w.r.t. exposure calculation. Modifying this will lead to a change on how linear exposure
        /// multipliers are computed from EV100 values (and viceversa). s_LensAttenuation models transmission attenuation and lens vignetting.
        /// Note that according to the standard ISO 12232, a lens saturates at s_LensAttenuation = 0.78f (under ISO 100).
        /// </summary>
        static public float s_LensAttenuation = 0.65f;

        /// <summary>
        /// Scale applied to exposure caused by lens imperfection. It is computed from s_LensAttenuation as follow:
        ///  (78 / ( S * q )) where S = 100 and q = s_LensAttenuation
        /// </summary>
        static public float lensImperfectionExposureScale
        {
            get => (78.0f / (100.0f * s_LensAttenuation));
        }

        /// <summary>
        /// An analytical model of chromaticity of the standard illuminant, by Judd et al.
        /// http://en.wikipedia.org/wiki/Standard_illuminant#Illuminant_series_D
        /// Slightly modifed to adjust it with the D65 white point (x=0.31271, y=0.32902).
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static float StandardIlluminantY(float x) => 2.87f * x - 3f * x * x - 0.27509507f;

        /// <summary>
        /// CIE xy chromaticity to CAT02 LMS.
        /// http://en.wikipedia.org/wiki/LMS_color_space#CAT02
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Vector3 CIExyToLMS(float x, float y)
        {
            float Y = 1f;
            float X = Y * x / y;
            float Z = Y * (1f - x - y) / y;

            float L = 0.7328f * X + 0.4296f * Y - 0.1624f * Z;
            float M = -0.7036f * X + 1.6975f * Y + 0.0061f * Z;
            float S = 0.0030f * X + 0.0136f * Y + 0.9834f * Z;

            return new Vector3(L, M, S);
        }

        /// <summary>
        /// Converts white balancing parameter to LMS coefficients.
        /// </summary>
        /// <param name="temperature">A temperature offset, in range [-100;100].</param>
        /// <param name="tint">A tint offset, in range [-100;100].</param>
        /// <returns>LMS coefficients.</returns>
        public static Vector3 ColorBalanceToLMSCoeffs(float temperature, float tint)
        {
            // Range ~[-1.5;1.5] works best
            float t1 = temperature / 65f;
            float t2 = tint / 65f;

            // Get the CIE xy chromaticity of the reference white point.
            // Note: 0.31271 = x value on the D65 white point
            float x = 0.31271f - t1 * (t1 < 0f ? 0.1f : 0.05f);
            float y = StandardIlluminantY(x) + t2 * 0.05f;

            // Calculate the coefficients in the LMS space.
            var w1 = new Vector3(0.949237f, 1.03542f, 1.08728f); // D65 white point
            var w2 = CIExyToLMS(x, y);
            return new Vector3(w1.x / w2.x, w1.y / w2.y, w1.z / w2.z);
        }

        /// <summary>
        /// Pre-filters shadows, midtones and highlights trackball values for shader use.
        /// </summary>
        /// <param name="inShadows">A color used for shadows.</param>
        /// <param name="inMidtones">A color used for midtones.</param>
        /// <param name="inHighlights">A color used for highlights.</param>
        /// <returns>The three input colors pre-filtered for shader use.</returns>
        public static (Vector4, Vector4, Vector4) PrepareShadowsMidtonesHighlights(in Vector4 inShadows, in Vector4 inMidtones, in Vector4 inHighlights)
        {
            float weight;

            var shadows = inShadows;
            shadows.x = Mathf.GammaToLinearSpace(shadows.x);
            shadows.y = Mathf.GammaToLinearSpace(shadows.y);
            shadows.z = Mathf.GammaToLinearSpace(shadows.z);
            weight = shadows.w * (Mathf.Sign(shadows.w) < 0f ? 1f : 4f);
            shadows.x = Mathf.Max(shadows.x + weight, 0f);
            shadows.y = Mathf.Max(shadows.y + weight, 0f);
            shadows.z = Mathf.Max(shadows.z + weight, 0f);
            shadows.w = 0f;

            var midtones = inMidtones;
            midtones.x = Mathf.GammaToLinearSpace(midtones.x);
            midtones.y = Mathf.GammaToLinearSpace(midtones.y);
            midtones.z = Mathf.GammaToLinearSpace(midtones.z);
            weight = midtones.w * (Mathf.Sign(midtones.w) < 0f ? 1f : 4f);
            midtones.x = Mathf.Max(midtones.x + weight, 0f);
            midtones.y = Mathf.Max(midtones.y + weight, 0f);
            midtones.z = Mathf.Max(midtones.z + weight, 0f);
            midtones.w = 0f;

            var highlights = inHighlights;
            highlights.x = Mathf.GammaToLinearSpace(highlights.x);
            highlights.y = Mathf.GammaToLinearSpace(highlights.y);
            highlights.z = Mathf.GammaToLinearSpace(highlights.z);
            weight = highlights.w * (Mathf.Sign(highlights.w) < 0f ? 1f : 4f);
            highlights.x = Mathf.Max(highlights.x + weight, 0f);
            highlights.y = Mathf.Max(highlights.y + weight, 0f);
            highlights.z = Mathf.Max(highlights.z + weight, 0f);
            highlights.w = 0f;

            return (shadows, midtones, highlights);
        }

        /// <summary>
        /// Pre-filters lift, gamma and gain trackball values for shader use.
        /// </summary>
        /// <param name="inLift">A color used for lift.</param>
        /// <param name="inGamma">A color used for gamma.</param>
        /// <param name="inGain">A color used for gain.</param>
        /// <returns>The three input colors pre-filtered for shader use.</returns>
        public static (Vector4, Vector4, Vector4) PrepareLiftGammaGain(in Vector4 inLift, in Vector4 inGamma, in Vector4 inGain)
        {
            var lift = inLift;
            lift.x = Mathf.GammaToLinearSpace(lift.x) * 0.15f;
            lift.y = Mathf.GammaToLinearSpace(lift.y) * 0.15f;
            lift.z = Mathf.GammaToLinearSpace(lift.z) * 0.15f;

            float lumLift = Luminance(lift);
            lift.x = lift.x - lumLift + lift.w;
            lift.y = lift.y - lumLift + lift.w;
            lift.z = lift.z - lumLift + lift.w;
            lift.w = 0f;

            var gamma = inGamma;
            gamma.x = Mathf.GammaToLinearSpace(gamma.x) * 0.8f;
            gamma.y = Mathf.GammaToLinearSpace(gamma.y) * 0.8f;
            gamma.z = Mathf.GammaToLinearSpace(gamma.z) * 0.8f;

            float lumGamma = Luminance(gamma);
            gamma.w += 1f;
            gamma.x = 1f / Mathf.Max(gamma.x - lumGamma + gamma.w, 1e-03f);
            gamma.y = 1f / Mathf.Max(gamma.y - lumGamma + gamma.w, 1e-03f);
            gamma.z = 1f / Mathf.Max(gamma.z - lumGamma + gamma.w, 1e-03f);
            gamma.w = 0f;

            var gain = inGain;
            gain.x = Mathf.GammaToLinearSpace(gain.x) * 0.8f;
            gain.y = Mathf.GammaToLinearSpace(gain.y) * 0.8f;
            gain.z = Mathf.GammaToLinearSpace(gain.z) * 0.8f;

            float lumGain = Luminance(gain);
            gain.w += 1f;
            gain.x = gain.x - lumGain + gain.w;
            gain.y = gain.y - lumGain + gain.w;
            gain.z = gain.z - lumGain + gain.w;
            gain.w = 0f;

            return (lift, gamma, gain);
        }

        /// <summary>
        /// Pre-filters colors used for the split toning effect.
        /// </summary>
        /// <param name="inShadows">A color used for shadows.</param>
        /// <param name="inHighlights">A color used for highlights.</param>
        /// <param name="balance">The balance between the shadow and highlight colors, in range [-100;100].</param>
        /// <returns>The two input colors pre-filtered for shader use.</returns>
        public static (Vector4, Vector4) PrepareSplitToning(in Vector4 inShadows, in Vector4 inHighlights, float balance)
        {
            // As counter-intuitive as it is, to make split-toning work the same way it does in
            // Adobe products we have to do all the maths in sRGB... So do not convert these to
            // linear before sending them to the shader, this isn't a bug!
            var shadows = inShadows;
            var highlights = inHighlights;

            // Balance is stored in `shadows.w`
            shadows.w = balance / 100f;
            highlights.w = 0f;

            return (shadows, highlights);
        }

        /// <summary>
        /// Returns the luminance of the specified color. The input is considered to be in linear
        /// space with sRGB primaries and a D65 white point.
        /// </summary>
        /// <param name="color">The color to compute the luminance for.</param>
        /// <returns>A luminance value.</returns>
        public static float Luminance(in Color color) => color.r * 0.2126729f + color.g * 0.7151522f + color.b * 0.072175f;

        /// <summary>
        /// Computes an exposure value (EV100) from physical camera settings.
        /// </summary>
        /// <param name="aperture">The camera aperture.</param>
        /// <param name="shutterSpeed">The camera exposure time.</param>
        /// <param name="ISO">The camera sensor sensitivity.</param>
        /// <returns>An exposure value, in EV100.</returns>
        public static float ComputeEV100(float aperture, float shutterSpeed, float ISO)
        {
            // References:
            // "Moving Frostbite to PBR" (Sébastien Lagarde & Charles de Rousiers)
            //   https://seblagarde.files.wordpress.com/2015/07/course_notes_moving_frostbite_to_pbr_v32.pdf
            // "Implementing a Physically Based Camera" (Padraic Hennessy)
            //   https://placeholderart.wordpress.com/2014/11/16/implementing-a-physically-based-camera-understanding-exposure/

            // EV number is defined as:
            //   2^ EV_s = N^2 / t and EV_s = EV_100 + log2 (S /100)
            // This gives
            //   EV_s = log2 (N^2 / t)
            //   EV_100 + log2 (S /100) = log2 (N^2 / t)
            //   EV_100 = log2 (N^2 / t) - log2 (S /100)
            //   EV_100 = log2 (N^2 / t . 100 / S)
            return Mathf.Log((aperture * aperture) / shutterSpeed * 100f / ISO, 2f);
        }

        /// <summary>
        /// Converts an exposure value (EV100) to a linear multiplier.
        /// </summary>
        /// <param name="EV100">The exposure value to convert, in EV100.</param>
        /// <returns>A linear multiplier.</returns>
        public static float ConvertEV100ToExposure(float EV100)
        {
            // Compute the maximum luminance possible with H_sbs sensitivity
            // maxLum = 78 / ( S * q ) * N^2 / t
            //        = 78 / ( S * q ) * 2^ EV_100
            //        = 78 / (100 * s_LensAttenuation) * 2^ EV_100
            //        = lensImperfectionExposureScale * 2^ EV
            // Reference: http://en.wikipedia.org/wiki/Film_speed
            float maxLuminance = lensImperfectionExposureScale * Mathf.Pow(2.0f, EV100);
            return 1.0f / maxLuminance;
        }

        /// <summary>
        /// Converts a linear multiplier to an exposure value (EV100).
        /// </summary>
        /// <param name="exposure">A linear multiplier.</param>
        /// <returns>An exposure value, in EV100.</returns>
        public static float ConvertExposureToEV100(float exposure)
        {
            // Compute the maximum luminance possible with H_sbs sensitivity
            // EV_100 = log2(   S * q    / (78 * exposure) )
            //        = log2( 100 * s_LensAttenuation / (78 * exposure) )
            //        = log2(    1.0f    / (lensImperfectionExposureScale * exposure) )
            // Reference: http://en.wikipedia.org/wiki/Film_speed
            return Mathf.Log(1.0f / (lensImperfectionExposureScale * exposure), 2.0f);
        }

        /// <summary>
        /// Computes an exposure value (EV100) from an average luminance value.
        /// </summary>
        /// <param name="avgLuminance">An average luminance value.</param>
        /// <returns>An exposure value, in EV100.</returns>
        public static float ComputeEV100FromAvgLuminance(float avgLuminance)
        {
            // The middle grey used will be determined by the s_LightMeterCalibrationConstant.
            // The suggested (ISO 2720) range  is 10.64 to 13.4. Common values used by
            // manufacturers range from 11.37 to 14. Ref: https://en.wikipedia.org/wiki/Light_meter
            // The default is 12.5% as it is the closest to 12.7% in order to have
            // a middle gray at 18% with a sqrt(2) room for specular highlights
            // Note that this gives equivalent results as using an incident light meter
            // with a calibration constant of C=314.
            float K = s_LightMeterCalibrationConstant;
            return Mathf.Log(avgLuminance * 100f / K, 2f);
        }

        /// <summary>
        /// Computes the required ISO to reach <paramref name="targetEV100"/>.
        /// </summary>
        /// <param name="aperture">The camera aperture.</param>
        /// <param name="shutterSpeed">The camera exposure time.</param>
        /// <param name="targetEV100">The target exposure value (EV100) to reach.</param>
        /// <returns>The required sensor sensitivity (ISO).</returns>
        public static float ComputeISO(float aperture, float shutterSpeed, float targetEV100) => ((aperture * aperture) * 100f) / (shutterSpeed * Mathf.Pow(2f, targetEV100));

        /// <summary>
        /// Converts a color value to its 32-bit hexadecimal representation.
        /// </summary>
        /// <param name="c">The color to convert.</param>
        /// <returns>A 32-bit hexadecimal representation of the color.</returns>
        public static uint ToHex(Color c) => ((uint)(c.a * 255) << 24) | ((uint)(c.r * 255) << 16) | ((uint)(c.g * 255) << 8) | (uint)(c.b * 255);

        /// <summary>
        /// Converts a 32-bit hexadecimal value to a color value.
        /// </summary>
        /// <param name="hex">A 32-bit hexadecimal value.</param>
        /// <returns>A color value.</returns>
        public static Color ToRGBA(uint hex) => new Color(((hex >> 16) & 0xff) / 255f, ((hex >> 8) & 0xff) / 255f, (hex & 0xff) / 255f, ((hex >> 24) & 0xff) / 255f);
    }
}
