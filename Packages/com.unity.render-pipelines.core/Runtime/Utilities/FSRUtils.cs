using System;
using Unity.Collections;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Utility functions relating to FidelityFX Super Resolution (FSR)
    ///
    /// These functions are expected to be used in conjuction with the helper functions provided by FSRCommon.hlsl.
    /// </summary>
    public static class FSRUtils
    {
        /// Shader constant ids used to communicate with the FSR shader implementation
        static class ShaderConstants
        {
            // EASU
            public static readonly int _FsrEasuConstants0 = Shader.PropertyToID("_FsrEasuConstants0");
            public static readonly int _FsrEasuConstants1 = Shader.PropertyToID("_FsrEasuConstants1");
            public static readonly int _FsrEasuConstants2 = Shader.PropertyToID("_FsrEasuConstants2");
            public static readonly int _FsrEasuConstants3 = Shader.PropertyToID("_FsrEasuConstants3");

            // RCAS
            public static readonly int _FsrRcasConstants = Shader.PropertyToID("_FsrRcasConstants");
        }

        /// <summary>
        /// Sets the constant values required by the FSR EASU shader on the provided command buffer
        ///
        /// Logic ported from "FsrEasuCon()" in Runtime/PostProcessing/Shaders/ffx/ffx_fsr1.hlsl
        /// </summary>
        /// <param name="cmd">Command buffer to modify</param>
        /// <param name="inputViewportSizeInPixels">This the rendered image resolution being upscaled</param>
        /// <param name="inputImageSizeInPixels">This is the resolution of the resource containing the input image (useful for dynamic resolution)</param>
        /// <param name="outputImageSizeInPixels">This is the display resolution which the input image gets upscaled to</param>
        public static void SetEasuConstants(CommandBuffer cmd, Vector2 inputViewportSizeInPixels, Vector2 inputImageSizeInPixels, Vector2 outputImageSizeInPixels)
        {
            Vector4 constants0;
            Vector4 constants1;
            Vector4 constants2;
            Vector4 constants3;

            // Output integer position to a pixel position in viewport.
            constants0.x = (inputViewportSizeInPixels.x / outputImageSizeInPixels.x);
            constants0.y = (inputViewportSizeInPixels.y / outputImageSizeInPixels.y);
            constants0.z = (0.5f * inputViewportSizeInPixels.x / outputImageSizeInPixels.x - 0.5f);
            constants0.w = (0.5f * inputViewportSizeInPixels.y / outputImageSizeInPixels.y - 0.5f);

            // Viewport pixel position to normalized image space.
            // This is used to get upper-left of 'F' tap.
            constants1.x = (1.0f / inputImageSizeInPixels.x);
            constants1.y = (1.0f / inputImageSizeInPixels.y);

            // Centers of gather4, first offset from upper-left of 'F'.
            //      +---+---+
            //      |   |   |
            //      +--(0)--+
            //      | b | c |
            //  +---F---+---+---+
            //  | e | f | g | h |
            //  +--(1)--+--(2)--+
            //  | i | j | k | l |
            //  +---+---+---+---+
            //      | n | o |
            //      +--(3)--+
            //      |   |   |
            //      +---+---+
            constants1.z = (1.0f / inputImageSizeInPixels.x);
            constants1.w = (-1.0f / inputImageSizeInPixels.y);

            // These are from (0) instead of 'F'.
            constants2.x = (-1.0f / inputImageSizeInPixels.x);
            constants2.y = (2.0f / inputImageSizeInPixels.y);
            constants2.z = (1.0f / inputImageSizeInPixels.x);
            constants2.w = (2.0f / inputImageSizeInPixels.y);

            constants3.x = (0.0f / inputImageSizeInPixels.x);
            constants3.y = (4.0f / inputImageSizeInPixels.y);

            // Fill the last constant with zeros to avoid using uninitialized memory
            constants3.z = 0.0f;
            constants3.w = 0.0f;

            cmd.SetGlobalVector(ShaderConstants._FsrEasuConstants0, constants0);
            cmd.SetGlobalVector(ShaderConstants._FsrEasuConstants1, constants1);
            cmd.SetGlobalVector(ShaderConstants._FsrEasuConstants2, constants2);
            cmd.SetGlobalVector(ShaderConstants._FsrEasuConstants3, constants3);
        }

        /// <summary>
        /// Sets the constant values required by the FSR RCAS shader on the provided command buffer
        ///
        /// Logic ported from "FsrRcasCon()" in Runtime/PostProcessing/Shaders/ffx/ffx_fsr1.hlsl
        /// </summary>
        /// <param name="cmd">Command buffer to modify</param>
        /// <param name="sharpness">The scale is {0.0 := maximum, to N>0, where N is the number of stops(halving) of the reduction of sharpness</param>
        public static void SetRcasConstants(CommandBuffer cmd, float sharpness = 0.2f)
        {
            // Transform from stops to linear value.
            sharpness = Mathf.Pow(2.0f, -sharpness);

            Vector4 constants;

            ushort sharpnessAsHalf = Mathf.FloatToHalf(sharpness);
            uint packedSharpness = (uint)(sharpnessAsHalf | (sharpnessAsHalf << 16));
            float packedSharpnessAsFloat = BitConverter.ToSingle(BitConverter.GetBytes(packedSharpness));

            constants.x = sharpness;
            constants.y = packedSharpnessAsFloat;

            // Fill the last constant with zeros to avoid using uninitialized memory
            constants.z = 0.0f;
            constants.w = 0.0f;

            cmd.SetGlobalVector(ShaderConstants._FsrRcasConstants, constants);
        }

        /// <summary>
        /// Returns true if FidelityFX Super Resolution (FSR) is supported on the current system
        ///
        /// FSR requires the textureGather shader instruction which wasn't supported by OpenGL ES until version 3.1
        /// </summary>
        /// <returns>if FSR is supported on the current system</returns>
        public static bool IsSupported()
        {
            return SystemInfo.graphicsShaderLevel >= 45;
        }
    }
}
