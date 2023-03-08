using System;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    public static class StpUtils
    {
        /// Shader constant ids used to communicate with the FSR shader implementation
        static class ShaderConstants
        {
            // Common
            public static readonly int _StpCommonConstant = Shader.PropertyToID("_StpCommonConstant");
            public static readonly int _StpBlueNoiseIn = Shader.PropertyToID("_StpBlueNoiseIn");

            // Inline
            public static readonly int _StpInlineConstants0 = Shader.PropertyToID("_StpInlineConstants0");
            public static readonly int _StpInlineConstants1 = Shader.PropertyToID("_StpInlineConstants1");
            public static readonly int _StpInlineConstants2 = Shader.PropertyToID("_StpInlineConstants2");
            public static readonly int _StpInlineConstants3 = Shader.PropertyToID("_StpInlineConstants3");
            public static readonly int _StpInlineConstants4 = Shader.PropertyToID("_StpInlineConstants4");
            public static readonly int _StpInlineConstants5 = Shader.PropertyToID("_StpInlineConstants5");
            public static readonly int _StpInlineConstants6 = Shader.PropertyToID("_StpInlineConstants6");
            public static readonly int _StpInlineConstants7 = Shader.PropertyToID("_StpInlineConstants7");
            public static readonly int _StpInlineConstants8 = Shader.PropertyToID("_StpInlineConstants8");
            public static readonly int _StpInlineConstants9 = Shader.PropertyToID("_StpInlineConstants9");
            public static readonly int _StpInlineConstantsA = Shader.PropertyToID("_StpInlineConstantsA");
            public static readonly int _StpInlineConstantsB = Shader.PropertyToID("_StpInlineConstantsB");
            public static readonly int _StpInlineConstantsC = Shader.PropertyToID("_StpInlineConstantsC");
            public static readonly int _StpInlineConstantsD = Shader.PropertyToID("_StpInlineConstantsD");

            // TAA
            public static readonly int _StpTaaConstants0 = Shader.PropertyToID("_StpTaaConstants0");
            public static readonly int _StpTaaConstants1 = Shader.PropertyToID("_StpTaaConstants1");
            public static readonly int _StpTaaConstants2 = Shader.PropertyToID("_StpTaaConstants2");
            public static readonly int _StpTaaConstants3 = Shader.PropertyToID("_StpTaaConstants3");
            public static readonly int _StpTaaConstants4 = Shader.PropertyToID("_StpTaaConstants4");

            // Cleaner
            public static readonly int _StpCleanerConstants0 = Shader.PropertyToID("_StpCleanerConstants0");
            public static readonly int _StpCleanerConstants1 = Shader.PropertyToID("_StpCleanerConstants1");
        }

        /// <summary>
        /// Helper function that calculates the jittern pattern associated with the provided frame index
        /// </summary>
        /// <param name="frameIndex">Index of the current frame</param>
        /// <returns>Jitter pattern for the provided frame index</returns>
        public static Vector2 Jit16(int frameIndex)
        {
            // 4xMSAA.
            int frame0 = (frameIndex & 3) << 1;
            int ix = (0x2D >> frame0) & 3;
            int iy = (0xB4 >> frame0) & 3;
            Vector2 result;
            result.x = ((float)ix) * (1.0f / 4.0f) + (-3.0f / 8.0f);
            result.y = ((float)iy) * (1.0f / 4.0f) + (-3.0f / 8.0f);
            // Modified by the '+' offset in groups of 4 frames.
            frame0 = ((frameIndex >> 2) & 3) << 1;
            ix = (0x19 >> frame0) & 3;
            iy = (0x64 >> frame0) & 3;
            result.x += ((float)ix) * (1.0f / 8.0f) + (-1.0f / 8.0f);
            result.y += ((float)iy) * (1.0f / 8.0f) + (-1.0f / 8.0f);
            return result;
        }

        /// <summary>
        /// Information required to set up STP's shader constants
        /// </summary>
        public struct ConstantParams
        {
            /// Distance to the camera's near plane
            /// Used to encode depth values
            public float nearPlane;

            /// Distance to the camera's far plane
            /// Used to encode depth values
            public float farPlane;

            /// Index of the current frame
            /// Used to calculate jitter pattern
            public int frameIndex;

            /// True if the current frame has valid history information
            /// Used to prevent STP from producing invalid data
            public bool hasValidHistory;

            /// A mask value applied that determines which stencil bit is associated with the responsive feature
            /// Used to prevent STP from producing incorrect values on transparent pixels
            /// Set to 0 if no stencil data is present
            public int stencilMask;

            /// Delta time value for the current frame
            /// Used to calculate motion vector scaling factor
            public float currentDeltaTime;

            /// Delta time value for the previous frame
            /// Used to calculate motion vector scaling factor
            public float lastDeltaTime;

            /// Size of the current viewport in pixels
            /// Used to calculate image coordinate scaling factors
            public Vector2 currentImageSize;

            /// Size of the previous viewport in pixels
            /// Used to calculate image coordinate scaling factors
            public Vector2 priorImageSize;

            /// Size of the feedback image in pixels
            /// The feedback image is always the same size as the final output image
            /// Used to calculate image coordinate scaling factors
            public Vector2 feedbackImageSize;

            /// Blue noise texture
            /// Used by various dither calculations within the shader
            public Texture2D noiseTexture;

            /// Non-Jittered projection matrix for the current frame
            /// Used by the static geometry reprojection feature
            public Matrix4x4 currentProj;

            /// Non-Jittered projection matrix for the previous frame
            /// Used by the static geometry reprojection feature
            public Matrix4x4 lastProj;

            /// Non-Jittered projection matrix for the frame before the previous frame
            /// Used by the static geometry reprojection feature
            public Matrix4x4 lastLastProj;

            /// View matrix for the current frame
            /// Used by the static geometry reprojection feature
            public Matrix4x4 currentView;

            /// View matrix for the previous frame
            /// Used by the static geometry reprojection feature
            public Matrix4x4 lastView;

            /// View matrix for the frame before the previous frame
            /// Used by the static geometry reprojection feature
            public Matrix4x4 lastLastView;
        }

        /// <summary>
        /// Sets shader constants that are common to all STP passes on the provided command buffer
        /// </summary>
        /// <param name="cmd">The command buffer to set shader constants with</param>
        /// <param name="parameters">Information required to calculate STP shader constants</param>
        public static void SetCommonConstants(
            CommandBuffer cmd,
            ConstantParams parameters)
        {
            Assert.IsTrue(Mathf.IsPowerOfTwo(parameters.noiseTexture.width));

            // [StencilMask | HasValidHistory | (Width - 1)]
            int packedBlueNoiseWidthMinusOne = (parameters.noiseTexture.width - 1) & 0xFF;
            int packedHasValidHistory = (parameters.hasValidHistory ? 1 : 0) << 8;
            int packedStencilMask = (parameters.stencilMask & 0xFF) << 16;

            int constant = packedStencilMask | packedHasValidHistory | packedBlueNoiseWidthMinusOne;

            cmd.SetGlobalFloat(ShaderConstants._StpCommonConstant, BitConverter.Int32BitsToSingle(constant));
            cmd.SetGlobalTexture(ShaderConstants._StpBlueNoiseIn, parameters.noiseTexture);
        }

        /// Returns a motion scaling ratio based on the difference in delta times across frames
        static float CalculateMotionScale(float deltaTime, float lastDeltaTime)
        {
            float motionScale = 1.0f;
            float currentDeltaTime = deltaTime;
            float previousDeltaTime = lastDeltaTime;
            if (!Mathf.Approximately(previousDeltaTime, 0.0f))
            {
                motionScale = currentDeltaTime / previousDeltaTime;
            }

            return motionScale;
        }

        /// Returns a matrix with the translation component removed
        /// This function is intended to be used with view matrices
        static Matrix4x4 ExtractRotation(Matrix4x4 input)
        {
            Matrix4x4 output = input;

            output[0, 3] = 0.0f;
            output[1, 3] = 0.0f;
            output[2, 3] = 0.0f;
            output[3, 3] = 1.0f;

            return output;
        }

        /// <summary>
        /// Sets shader constants that are specific to the STP Inline pass on the provided command buffer
        /// </summary>
        /// <param name="cmd">The command buffer to set shader constants with</param>
        /// <param name="parameters">Information required to calculate STP shader constants</param>
        public static void SetInlineConstants(CommandBuffer cmd, ConstantParams parameters)
        {
            Vector4 constants0;
            Vector4 constants1;
            Vector4 constants2;
            Vector4 constants3;
            Vector4 constants4;
            Vector4 constants5;
            Vector4 constants6;
            Vector4 constants7;
            Vector4 constants8;
            Vector4 constants9;
            Vector4 constantsA;
            Vector4 constantsB;
            Vector4 constantsC;
            Vector4 constantsD;

            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kRcpC := 1.0 / size of current input image in pixels.
            constants0.x = (1.0f / parameters.currentImageSize.x);
            constants0.y = (1.0f / parameters.currentImageSize.y);
            // StpF2 kHalfRcpC := 0.5 / size of current input image in pixels.
            constants0.z = (0.5f / parameters.currentImageSize.x);
            constants0.w = (0.5f / parameters.currentImageSize.y);
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kC := Size of current input image in pixels.
            constants1.x = parameters.currentImageSize.x;
            constants1.y = parameters.currentImageSize.y;
            //------------------------------------------------------------------------------------------------------------------------------
            // Grab jitter for current and prior frames.
            Vector2 jitP = Jit16(parameters.frameIndex - 1);
            Vector2 jitC = Jit16(parameters.frameIndex);
            // StpF2 kJitCRcpCUnjitPRcpP := Map current into prior frame.
            constants1.z = (jitC.x / parameters.currentImageSize.x - jitP.x / parameters.priorImageSize.x);
            constants1.w = (jitC.y / parameters.currentImageSize.y - jitP.y / parameters.priorImageSize.y);
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kHalfRcpP := Half size of a pixel in the prior frame.
            constants2.x = (0.5f / parameters.priorImageSize.x);
            constants2.y = (0.5f / parameters.priorImageSize.y);
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kDepth := Copied logic from StpZCon().
            float k0 = (1.0f / parameters.nearPlane);
            float k1 = (1.0f / Mathf.Log(k0 * parameters.farPlane, 2.0f));
            constants2.z = k0;
            constants2.w = k1;
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kJitCRcpC := Take {0 to 1} position in current image, and map back to {0 to 1} position in feedback (removes jitter).
            constants3.x = jitC.x / parameters.currentImageSize.x;
            constants3.y = jitC.y / parameters.currentImageSize.y;
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kF := size of feedback (aka output) in pixels.
            constants3.z = parameters.feedbackImageSize.x;
            constants3.w = parameters.feedbackImageSize.y;
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF4 kOS := Scale and bias to check for out of bounds (and kill feedback).
            // Scaled and biased output needs to {-1 out of bounds, >-1 in bounds, <1 in bounds, 1 out of bounds}.
            Vector2 s;
            // Undo 'pM' scaling, and multiply by 2 (as this needs to be -1 to 1 at edge of acceptable reprojection).
            s.x = 2.0f;
            s.y = 2.0f;
            // Scaling to push outside safe reprojection over 1.
            s.x *= (parameters.priorImageSize.x / (parameters.priorImageSize.x + 4.0f));
            s.y *= (parameters.priorImageSize.y / (parameters.priorImageSize.y + 4.0f));
            constants4.x = s[0];
            constants4.y = s[1];
            // Factor out subtracting off the mid point scaled by the multiply term.
            constants4.z = (-0.5f * s[0]);
            constants4.w = (-0.5f * s[1]);

            //------------------------------------------------------------------------------------------------------------------------------
            // kSharp
            //  .x = mul term
            //  .y = add term
            // Add term amounts, (input/output for x)
            //  none ...... 1         -> 0.0
            //  2x area ... sqrt(1/2) -> 0.59
            //  4x area ... 1/2       -> 1.0
            Vector2 kSharp;
            kSharp.y = Mathf.Clamp(2.0f - 2.0f * (parameters.currentImageSize.x / parameters.feedbackImageSize.x), 0.0f, 1.0f);
            kSharp.x = 1.0f - kSharp.y;
            constants5.x = kSharp.x;
            constants5.y = kSharp.y;
            constants5.z = BitConverter.Int32BitsToSingle(PackVector2ToInt(kSharp));
            constants5.w = CalculateMotionScale(parameters.currentDeltaTime, parameters.lastDeltaTime);

            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kUnDepth := Copied logic from StpZUnCon().
            constants6.x = Mathf.Log(parameters.farPlane / parameters.nearPlane, 2.0f);
            constants6.y = parameters.nearPlane;
            // Unused for now.
            constants6.z = 0.0f;
            constants6.w = 0.0f;
            //------------------------------------------------------------------------------------------------------------------------------
            // See header docs in "STATIC GEOMETRY MOTION FORWARD PROJECTION".
            Vector2 prjPriAB;
            prjPriAB.x = parameters.lastProj[0, 0];

            // NOTE: Unity flips the Y axis inside the projection matrix. STP requires a non-flipped Y axis, so we undo the flip here with abs
            prjPriAB.y = Mathf.Abs(parameters.lastProj[1, 1]);

            Vector4 prjPriCDGH;
            prjPriCDGH.x = parameters.lastProj[2, 2];
            prjPriCDGH.y = parameters.lastProj[2, 3];
            prjPriCDGH.z = parameters.lastProj[3, 2];
            prjPriCDGH.w = parameters.lastProj[3, 3];

            Vector2 prjCurAB;
            prjCurAB.x = parameters.currentProj[0, 0];

            // NOTE: Unity flips the Y axis inside the projection matrix. STP requires a non-flipped Y axis, so we undo the flip here with abs
            prjCurAB.y = Mathf.Abs(parameters.currentProj[1, 1]);

            Vector4 prjCurCDGH;
            prjCurCDGH.x = parameters.currentProj[2, 2];
            prjCurCDGH.y = parameters.currentProj[2, 3];
            prjCurCDGH.z = parameters.currentProj[3, 2];
            prjCurCDGH.w = parameters.currentProj[3, 3];

            Matrix4x4 forwardTransform = ExtractRotation(parameters.currentView)                   *
                                         Matrix4x4.Translate(-parameters.currentView.GetColumn(3)) *
                                         Matrix4x4.Translate(parameters.lastView.GetColumn(3))     *
                                         ExtractRotation(parameters.lastView).transpose;

            Vector4 forIJKL = forwardTransform.GetRow(0);
            Vector4 forMNOP = forwardTransform.GetRow(1);
            Vector4 forQRST = forwardTransform.GetRow(2);

            // k0123
            constants7.x = prjPriCDGH.z / prjPriAB.x;
            constants7.y = prjPriCDGH.w / prjPriAB.x;
            constants7.z = prjPriCDGH.z / prjPriAB.y;
            constants7.w = prjPriCDGH.w / prjPriAB.y;
            // k4567
            constants8.x = forIJKL.x * prjCurAB.x;
            constants8.y = forIJKL.y * prjCurAB.x;
            constants8.z = forIJKL.z * prjCurAB.x;
            constants8.w = forIJKL.w * prjCurAB.x;
            // k89AB
            constants9.x = forMNOP.x * prjCurAB.y;
            constants9.y = forMNOP.y * prjCurAB.y;
            constants9.z = forMNOP.z * prjCurAB.y;
            constants9.w = forMNOP.w * prjCurAB.y;
            // kCDEF
            constantsA.x = forQRST.x * prjCurCDGH.z;
            constantsA.y = forQRST.y * prjCurCDGH.z;
            constantsA.z = forQRST.z * prjCurCDGH.z;
            constantsA.w = forQRST.w * prjCurCDGH.z + prjCurCDGH.w;

            Vector2 prjPrvAB;
            prjPrvAB.x = parameters.lastLastProj[0, 0];

            // NOTE: Unity flips the Y axis inside the projection matrix. STP requires a non-flipped Y axis, so we undo the flip here with abs
            prjPrvAB.y = Mathf.Abs(parameters.lastLastProj[1, 1]);
            
            Vector4 prjPrvCDGH;
            prjPrvCDGH.x = parameters.lastLastProj[2, 2];
            prjPrvCDGH.y = parameters.lastLastProj[2, 3];
            prjPrvCDGH.z = parameters.lastLastProj[3, 2];
            prjPrvCDGH.w = parameters.lastLastProj[3, 3];

            Matrix4x4 backwardTransform = ExtractRotation(parameters.lastLastView)                   *
                                          Matrix4x4.Translate(-parameters.lastLastView.GetColumn(3)) *
                                          Matrix4x4.Translate(parameters.lastView.GetColumn(3))      *
                                          ExtractRotation(parameters.lastView).transpose;

            Vector4 bckIJKL = backwardTransform.GetRow(0);
            Vector4 bckMNOP = backwardTransform.GetRow(1);
            Vector4 bckQRST = backwardTransform.GetRow(2);
            
            // kGHIJ
            constantsB.x = bckIJKL.x * prjPrvAB.x;
            constantsB.y = bckIJKL.y * prjPrvAB.x;
            constantsB.z = bckIJKL.z * prjPrvAB.x;
            constantsB.w = bckIJKL.w * prjPrvAB.x;
            // kKLMN
            constantsC.x = bckMNOP.x * prjPrvAB.y;
            constantsC.y = bckMNOP.y * prjPrvAB.y;
            constantsC.z = bckMNOP.z * prjPrvAB.y;
            constantsC.w = bckMNOP.w * prjPrvAB.y;
            // kOPQR
            constantsD.x = bckQRST.x * prjPrvCDGH.z;
            constantsD.y = bckQRST.y * prjPrvCDGH.z;
            constantsD.z = bckQRST.z * prjPrvCDGH.z;
            constantsD.w = bckQRST.w * prjPrvCDGH.z + prjPrvCDGH.w;

            cmd.SetGlobalVector(ShaderConstants._StpInlineConstants0, constants0);
            cmd.SetGlobalVector(ShaderConstants._StpInlineConstants1, constants1);
            cmd.SetGlobalVector(ShaderConstants._StpInlineConstants2, constants2);
            cmd.SetGlobalVector(ShaderConstants._StpInlineConstants3, constants3);
            cmd.SetGlobalVector(ShaderConstants._StpInlineConstants4, constants4);
            cmd.SetGlobalVector(ShaderConstants._StpInlineConstants5, constants5);
            cmd.SetGlobalVector(ShaderConstants._StpInlineConstants6, constants6);
            cmd.SetGlobalVector(ShaderConstants._StpInlineConstants7, constants7);
            cmd.SetGlobalVector(ShaderConstants._StpInlineConstants8, constants8);
            cmd.SetGlobalVector(ShaderConstants._StpInlineConstants9, constants9);
            cmd.SetGlobalVector(ShaderConstants._StpInlineConstantsA, constantsA);
            cmd.SetGlobalVector(ShaderConstants._StpInlineConstantsB, constantsB);
            cmd.SetGlobalVector(ShaderConstants._StpInlineConstantsC, constantsC);
            cmd.SetGlobalVector(ShaderConstants._StpInlineConstantsD, constantsD);
        }

        /// Returns the STP_KERNEL constant value defined in the STP shader
        static float KernelSize()
        {
            // This sets the amount of anti-aliasing and is proportional to output pixel size.
            //                         xxxxxxxxxx ... Actual radius.
            //                  xxx ................. PSinCos() returns {-1/4 to 1/4}.
            return (4.0f * (1.0f / 12.0f));
        }

        /// Helper function that converts the provided Vector2 into a packed integer with two FP16 values
        static int PackVector2ToInt(Vector2 value)
        {
            uint xAsHalf = Mathf.FloatToHalf(value.x);
            uint yAsHalf = Mathf.FloatToHalf(value.y);
            return (int)(xAsHalf | (yAsHalf << 16));
        }

        /// <summary>
        /// Sets shader constants that are specific to the STP TAA pass on the provided command buffer
        /// </summary>
        /// <param name="cmd">The command buffer to set shader constants with</param>
        /// <param name="parameters">Information required to calculate STP shader constants</param>
        public static void SetTaaConstants(CommandBuffer cmd, ConstantParams parameters)
        {
            Vector4 constants0;
            Vector4 constants1;
            Vector4 constants2;
            Vector4 constants3;
            Vector4 constants4;

            // We currently disable grain support at the shader level, but keep the CPU side constant logic alive
            // in order to prevent code divergence between HLSL and C#.
            float grain = 0.0f;

            //------------------------------------------------------------------------------------------------------------------------------
            // Grab jitter for current frame.
            Vector2 jitC = Jit16(parameters.frameIndex);
            //------------------------------------------------------------------------------------------------------------------------------
            // Conversion from integer pix position to center pix float pixel position in image for current input.
            //  xy := multiply term (M) --- Scale by 1/imgF to get to {0 to 1}.
            //  zw := addition term (A) --- Add 0.5*M to get to center of pixel, then subtract jitC to undo jitter.
            // StpF2 kCRcpF.
            constants0.x = (parameters.currentImageSize.x / parameters.feedbackImageSize.x);
            constants0.y = (parameters.currentImageSize.y / parameters.feedbackImageSize.y);
            // StpF2 kHalfCRcpFUnjitC.
            constants0.z = (0.5f * parameters.currentImageSize.x / parameters.feedbackImageSize.x - jitC.x);
            constants0.w = (0.5f * parameters.currentImageSize.y / parameters.feedbackImageSize.y - jitC.y);
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kRcpC := 1/size of current input image in pixels.
            constants1.x = (1.0f / parameters.currentImageSize.x);
            constants1.y = (1.0f / parameters.currentImageSize.y);
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF1 kDubRcpCX := 2 times kRcpC.x.
            constants1.z = (2.0f / parameters.currentImageSize.x);
            //------------------------------------------------------------------------------------------------------------------------------
            // StpH2 kKRcpF := STP_KERNEL/size of current output image in pixels.
            // Kernel is adaptive based on the amount of scaling ('c/f' term).
            Vector2 kKRcpF;
            kKRcpF.x = (KernelSize() * parameters.currentImageSize.x / (parameters.feedbackImageSize.x * parameters.feedbackImageSize.x));
            kKRcpF.y = (KernelSize() * parameters.currentImageSize.y / (parameters.feedbackImageSize.y * parameters.feedbackImageSize.y));
            constants1.w = BitConverter.Int32BitsToSingle(PackVector2ToInt(kKRcpF));
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kRcpF := 1/size of feedback image (aka output) in pixels.
            constants2.x = (1.0f / parameters.feedbackImageSize.x);
            constants2.y = (1.0f / parameters.feedbackImageSize.y);
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kHalfRcpF := 0.5/size of feedback image (aka output) in pixels.
            constants2.z = (0.5f / parameters.feedbackImageSize.x);
            constants2.w = (0.5f / parameters.feedbackImageSize.y);
            //------------------------------------------------------------------------------------------------------------------------------
            Vector2 kGrain;
            kGrain.x = Mathf.Pow(2.0f, -grain);
            kGrain.y = -0.5f * kGrain.x;
            constants3.x = kKRcpF.x;
            constants3.y = kKRcpF.y;
            constants3.z = BitConverter.Int32BitsToSingle(PackVector2ToInt(kGrain));
            constants3.w = 0.0f;

            //------------------------------------------------------------------------------------------------------------------------------
            constants4.x = kGrain.x;
            constants4.y = kGrain.y;

            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kF := size of feedback image in pixels.
            constants4.z = parameters.feedbackImageSize.x;
            constants4.w = parameters.feedbackImageSize.y;

            cmd.SetGlobalVector(ShaderConstants._StpTaaConstants0, constants0);
            cmd.SetGlobalVector(ShaderConstants._StpTaaConstants1, constants1);
            cmd.SetGlobalVector(ShaderConstants._StpTaaConstants2, constants2);
            cmd.SetGlobalVector(ShaderConstants._StpTaaConstants3, constants3);
            cmd.SetGlobalVector(ShaderConstants._StpTaaConstants4, constants4);
        }

        /// <summary>
        /// Sets shader constants that are specific to the STP Cleaner pass on the provided command buffer
        /// </summary>
        /// <param name="cmd">The command buffer to set shader constants with</param>
        /// <param name="parameters">Information required to calculate STP shader constants</param>
        /// <param name="sharp">Controls the amount of sharpening to apply. Set to 0.0 for default sharpness.</param>
        public static void SetCleanerConstants(CommandBuffer cmd, ConstantParams parameters, float sharp = 0.0f)
        {
            Vector4 constants0;
            Vector4 constants1;

            // We currently disable grain support at the shader level, but keep the CPU side constant logic alive
            // in order to prevent code divergence between HLSL and C#.
            float grain = 0.0f;

            //------------------------------------------------------------------------------------------------------------------------------
            // Baseline sharpening set to avoid something too unnatural since pre-cleaner STP is already sharp.
            sharp += 0.33333f;
            //------------------------------------------------------------------------------------------------------------------------------
            Vector2 kSharp;
            kSharp.x = Mathf.Pow(2.0f, -sharp);
            kSharp.y = 0.0f;
            Vector2 kGrain;
            kGrain.x = Mathf.Pow(2.0f, -grain);
            kGrain.y = -0.5f * kGrain.x;
            //------------------------------------------------------------------------------------------------------------------------------
            constants0.x = kGrain.x;
            constants0.y = kGrain.y;
            constants0.z = kSharp.x;
            constants0.w = 0.0f;
            //------------------------------------------------------------------------------------------------------------------------------
            constants1.x = BitConverter.Int32BitsToSingle(PackVector2ToInt(kGrain));
            constants1.y = BitConverter.Int32BitsToSingle(PackVector2ToInt(kSharp));
            constants1.z = 0.0f;
            constants1.w = 0.0f;

            cmd.SetGlobalVector(ShaderConstants._StpCleanerConstants0, constants0);
            cmd.SetGlobalVector(ShaderConstants._StpCleanerConstants1, constants1);
        }

        /// <summary>
        /// Returns true if STP is supported on the current system
        /// STP requires the textureGather shader instruction which wasn't supported by OpenGL ES until version 3.1
        /// </summary>
        /// <returns>True if supported</returns>
        public static bool IsSupported()
        {
            return SystemInfo.graphicsShaderLevel >= 45;
        }

        /// <summary>
        /// Returns the GPU memory usage of STP's internal textures at the provided viewport size
        /// </summary>
        /// <param name="viewportSize">Size of the rendered viewport</param>
        /// <returns>GPU memory usage of STP's internal textures in bytes</returns>
        public static long CalculateMemoryUsageForViewport(Vector2Int viewportSize)
        {
            long numPixels = viewportSize.x * viewportSize.y;

            // R32_UINT Current & Previous
            long depthMotionMemoryUsage = 2 * numPixels * 4;

            // R8_UINT Current & Previous
            long lumaMemoryUsage = 2 * numPixels;

            // RGBA8_UNORM Current
            long intermediateColorMemoryUsage = numPixels * 4;

            return depthMotionMemoryUsage + lumaMemoryUsage + intermediateColorMemoryUsage;
        }
    }
}
