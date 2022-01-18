using System;

namespace UnityEngine.Rendering
{
    public class FfxCasUtils
    {
        /// Shader constant ids used to communicate with the FFX CAS shader implementation
        static class ShaderConstants
        {
            public static readonly int _CasConstants0 = Shader.PropertyToID("_CasConstants0");
            public static readonly int _CasConstants1 = Shader.PropertyToID("_CasConstants1");
            public static readonly int _CasOptions = Shader.PropertyToID("_CasOptions");
        }

        // Call to setup required constant values.
        public static void CasSetup(CommandBuffer cmd,
            float sharpness,
            float inputSizeInPixelsX, float inputSizeInPixelsY,
            float outputSizeInPixelsX, float outputSizeInPixelsY,
            bool lowQuality)
        {
            Vector4 constants0;
            constants0.x = inputSizeInPixelsX / outputSizeInPixelsX;
            constants0.y = inputSizeInPixelsY / outputSizeInPixelsY;
            constants0.z = 0.5f * inputSizeInPixelsX / outputSizeInPixelsX - 0.5f;
            constants0.w = 0.5f * inputSizeInPixelsY / outputSizeInPixelsY - 0.5f;
            cmd.SetGlobalVector(ShaderConstants._CasConstants0, constants0);

            Vector4 constants1;
            if (sharpness < 0.0f) sharpness = 0.0f;
            if (sharpness > 1.0f) sharpness = 1.0f;
            sharpness = 8.0f + (5.0f - 8.0f) * sharpness;
            sharpness = -1.0f / sharpness;
            constants1.x = sharpness;
            uint sharpnessAsHalf = Mathf.FloatToHalf(sharpness);
            int packedSharpness = (int)(sharpnessAsHalf | (sharpnessAsHalf << 16));
            constants1.y = BitConverter.Int32BitsToSingle(packedSharpness);
            constants1.z = 8.0f * inputSizeInPixelsX / outputSizeInPixelsX;
            constants1.w = 0.0f;
            cmd.SetGlobalVector(ShaderConstants._CasConstants1, constants1);

            bool sharpenOnly = inputSizeInPixelsX >= outputSizeInPixelsX || inputSizeInPixelsY >= outputSizeInPixelsY;

            Vector4 options;
            options.x = sharpenOnly ? 1.0f : 0.0f;
            options.y = lowQuality ? 1.0f : 0.0f;
            options.z = outputSizeInPixelsX;
            options.w = outputSizeInPixelsY;
            cmd.SetGlobalVector(ShaderConstants._CasOptions, options);
        }
    }
}
