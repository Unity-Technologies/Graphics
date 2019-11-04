namespace UnityEngine.Rendering
{
    // Has to be kept in sync with PhysicalCamera.hlsl
    public static class ColorUtils
    {
        // An analytical model of chromaticity of the standard illuminant, by Judd et al.
        // http://en.wikipedia.org/wiki/Standard_illuminant#Illuminant_series_D
        // Slightly modifed to adjust it with the D65 white point (x=0.31271, y=0.32902).
        public static float StandardIlluminantY(float x) => 2.87f * x - 3f * x * x - 0.27509507f;

        // CIE xy chromaticity to CAT02 LMS.
        // http://en.wikipedia.org/wiki/LMS_color_space#CAT02
        public static Vector3 CIExyToLMS(float x, float y)
        {
            float Y = 1f;
            float X = Y * x / y;
            float Z = Y * (1f - x - y) / y;

            float L =  0.7328f * X + 0.4296f * Y - 0.1624f * Z;
            float M = -0.7036f * X + 1.6975f * Y + 0.0061f * Z;
            float S =  0.0030f * X + 0.0136f * Y + 0.9834f * Z;

            return new Vector3(L, M, S);
        }

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

        // RGB in linear space with sRGB primaries and D65 white point
        public static float Luminance(in Color color) => color.r * 0.2126729f + color.g * 0.7151522f + color.b * 0.072175f;

        // References:
        // "Moving Frostbite to PBR" (Sébastien Lagarde & Charles de Rousiers)
        //   https://seblagarde.files.wordpress.com/2015/07/course_notes_moving_frostbite_to_pbr_v32.pdf
        // "Implementing a Physically Based Camera" (Padraic Hennessy)
        //   https://placeholderart.wordpress.com/2014/11/16/implementing-a-physically-based-camera-understanding-exposure/
        public static float ComputeEV100(float aperture, float shutterSpeed, float ISO)
        {
            // EV number is defined as:
            //   2^ EV_s = N^2 / t and EV_s = EV_100 + log2 (S /100)
            // This gives
            //   EV_s = log2 (N^2 / t)
            //   EV_100 + log2 (S /100) = log2 (N^2 / t)
            //   EV_100 = log2 (N^2 / t) - log2 (S /100)
            //   EV_100 = log2 (N^2 / t . 100 / S)
            return Mathf.Log((aperture * aperture) / shutterSpeed * 100f / ISO, 2f);
        }

        public static float ConvertEV100ToExposure(float EV100)
        {
            // Compute the maximum luminance possible with H_sbs sensitivity
            // maxLum = 78 / ( S * q ) * N^2 / t
            //        = 78 / ( S * q ) * 2^ EV_100
            //        = 78 / (100 * 0.65) * 2^ EV_100
            //        = 1.2 * 2^ EV
            // Reference: http://en.wikipedia.org/wiki/Film_speed
            float maxLuminance = 1.2f * Mathf.Pow(2f, EV100);
            return 1f / maxLuminance;
        }

        public static float ConvertExposureToEV100(float exposure)
        {
            const float k = 1f / 1.2f;
            return -Mathf.Log(exposure / k, 2f);
        }

        public static float ComputeEV100FromAvgLuminance(float avgLuminance)
        {
            // We later use the middle gray at 12.7% in order to have
            // a middle gray at 18% with a sqrt(2) room for specular highlights
            // But here we deal with the spot meter measuring the middle gray
            // which is fixed at 12.5 for matching standard camera
            // constructor settings (i.e. calibration constant K = 12.5)
            // Reference: http://en.wikipedia.org/wiki/Film_speed
            const float K = 12.5f; // Reflected-light meter calibration constant
            return Mathf.Log(avgLuminance * 100f / K, 2f);
        }

        // Compute the required ISO to reach the target EV100
        public static float ComputeISO(float aperture, float shutterSpeed, float targetEV100) => ((aperture * aperture) * 100f) / (shutterSpeed * Mathf.Pow(2f, targetEV100));

        public static uint ToHex(Color c) => ((uint)(c.a * 255) << 24) | ((uint)(c.r * 255) << 16) | ((uint)(c.g * 255) << 8) | (uint)(c.b * 255);

        public static Color ToRGBA(uint hex) => new Color(((hex >> 16) & 0xff) / 255f, ((hex >>  8) & 0xff) / 255f, (hex & 0xff) / 255f, ((hex >> 24) & 0xff) / 255f);
    }
}
