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
