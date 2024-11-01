using System;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Interface to the Spatial-Temporal Post-Processing Upscaler (STP).
    /// This class allows users to configure and execute STP via render graph.
    /// </summary>
    public static class STP
    {
        /// <summary>
        /// Returns true if STP is supported on the current device. Otherwise, false.
        /// STP requires compute shaders
        /// </summary>
        /// <returns>True if supported</returns>
        public static bool IsSupported()
        {
            bool isSupported = true;

            // STP uses compute shaders as part of its implementation
            isSupported &= SystemInfo.supportsComputeShaders;

            // GLES has stricter rules than GL when it comes to image store format declarations and matching them with underlying image types.
            // STP's implementation uses several image formats that don't translate accurately from HLSL to GLSL which means a format mismatch will occur.
            // Image format mismatches result in undefined behavior on writes, so we disable STP support for GLES in order to avoid problems.
            // In most cases, hardware that meets the requirements for STP should be capable of running Vulkan anyways.
            isSupported &= (SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES3);

            return isSupported;
        }

        /// <summary>
        /// Helper function that calculates the STP-specific jitter pattern associated with the provided frame index.
        /// </summary>
        /// <param name="frameIndex">Index of the current frame</param>
        /// <returns>Jitter pattern for the provided frame index</returns>
        public static Vector2 Jit16(int frameIndex)
        {
            Vector2 result;
            result.x = HaltonSequence.Get(frameIndex, 2) - 0.5f;
            result.y = HaltonSequence.Get(frameIndex, 3) - 0.5f;

            return result;
        }

        // We use a constant to define the debug view arrays to guarantee that they're exactly the same length at compile time
        const int kNumDebugViews = 6;

        // We define a fixed array of GUIContent values here which map to supported debug views within the STP shader code
        static readonly GUIContent[] s_DebugViewDescriptions = new GUIContent[kNumDebugViews]
        {
            new GUIContent("Clipped Input Color", "Shows input color clipped to {0 to 1}"),
            new GUIContent("Log Input Depth", "Shows input depth in log scale"),
            new GUIContent("Reversible Tonemapped Input Color", "Shows input color after conversion to reversible tonemaped space"),
            new GUIContent("Shaped Absolute Input Motion", "Visualizes input motion vectors"),
            new GUIContent("Motion Reprojection {R=Prior G=This Sqrt Luma Feedback Diff, B=Offscreen}", "Visualizes reprojected frame difference"),
            new GUIContent("Sensitivity {G=No motion match, R=Responsive, B=Luma}", "Visualize pixel sensitivities"),
        };

        // Unfortunately we must maintain a sequence of index values that map to the supported debug view indices
        // if we want to be able to display the debug views as an enum field without allocating any garbage.
        static readonly int[] s_DebugViewIndices = new int[kNumDebugViews]
        {
            0,
            1,
            2,
            3,
            4,
            5,
        };

        /// <summary>
        /// Array of debug view descriptions expected to be used in the rendering debugger UI
        /// </summary>
        public static GUIContent[] debugViewDescriptions { get { return s_DebugViewDescriptions; } }

        /// <summary>
        /// Array of debug view indices expected to be used in the rendering debugger UI
        /// </summary>
        public static int[] debugViewIndices { get { return s_DebugViewIndices; } }

        /// <summary>
        /// STP configuration data that varies per rendered view
        /// </summary>
        public struct PerViewConfig
        {
            /// <summary>
            /// Non-Jittered projection matrix for the current frame
            /// Used by the static geometry reprojection feature
            /// </summary>
            public Matrix4x4 currentProj;

            /// <summary>
            /// Non-Jittered projection matrix for the previous frame
            /// Used by the static geometry reprojection feature
            /// </summary>
            public Matrix4x4 lastProj;

            /// <summary>
            /// Non-Jittered projection matrix for the frame before the previous frame
            /// Used by the static geometry reprojection feature
            /// </summary>
            public Matrix4x4 lastLastProj;

            /// <summary>
            /// View matrix for the current frame
            /// Used by the static geometry reprojection feature
            /// </summary>
            public Matrix4x4 currentView;

            /// <summary>
            /// View matrix for the previous frame
            /// Used by the static geometry reprojection feature
            /// </summary>
            public Matrix4x4 lastView;

            /// <summary>
            /// View matrix for the frame before the previous frame
            /// Used by the static geometry reprojection feature
            /// </summary>
            public Matrix4x4 lastLastView;
        }

        /// <summary>
        /// Maximum amount of supported per-view configurations
        /// </summary>
        const int kMaxPerViewConfigs = 2;

        /// <summary>
        /// Static allocation of per-view configurations
        /// </summary>
        static PerViewConfig[] s_PerViewConfigs = new PerViewConfig[kMaxPerViewConfigs];

        /// <summary>
        /// Static allocation of per-view configurations
        /// Users are expected to populate this during STP configuration and then assign it to the relevant
        /// configuration structure field(s) to avoid unnecessary allocations.
        /// </summary>
        public static PerViewConfig[] perViewConfigs
        {
            get { return s_PerViewConfigs; }
            set { s_PerViewConfigs = value; }
        }

        /// <summary>
        /// Top-level configuration structure required for STP execution
        /// </summary>
        public struct Config
        {
            /// <summary>
            /// Blue noise texture used in various parts of the upscaling logic
            /// </summary>
            public Texture2D noiseTexture;

            /// <summary>
            /// Input color texture to be upscaled
            /// </summary>
            public TextureHandle inputColor;

            /// <summary>
            /// Input depth texture which will be analyzed during upscaling
            /// </summary>
            public TextureHandle inputDepth;

            /// <summary>
            /// Input motion vector texture which is used to reproject information across frames
            /// </summary>
            public TextureHandle inputMotion;

            /// <summary>
            /// [Optional] Input stencil texture which is used to identify pixels that need special treatment such as particles or in-game screens
            /// </summary>
            public TextureHandle inputStencil;

            /// <summary>
            /// [Optional] Output debug view texture which STP can be configured to render debug visualizations into
            /// </summary>
            public TextureHandle debugView;

            /// <summary>
            /// Output color texture which will receive the final upscaled color result
            /// </summary>
            public TextureHandle destination;

            /// <summary>
            /// Input history context to use when executing STP
            /// </summary>
            public HistoryContext historyContext;

            /// <summary>
            /// Set to true if hardware dynamic resolution scaling is currently active
            /// </summary>
            public bool enableHwDrs;

            /// <summary>
            /// Set to true if the rendering environment is using 2d array textures (usually due to XR)
            /// </summary>
            public bool enableTexArray;

            /// <summary>
            /// Set to true to enable the motion scaling feature which attempts to compensate for variable frame timing when working with motion vectors
            /// </summary>
            public bool enableMotionScaling;

            /// <summary>
            /// Distance to the camera's near plane
            /// Used to encode depth values
            /// </summary>
            public float nearPlane;

            /// <summary>
            /// Distance to the camera's far plane
            /// Used to encode depth values
            /// </summary>
            public float farPlane;

            /// <summary>
            /// Index of the current frame
            /// Used to calculate jitter pattern
            /// </summary>
            public int frameIndex;

            /// <summary>
            /// True if the current frame has valid history information
            /// Used to prevent STP from producing invalid data
            /// </summary>
            public bool hasValidHistory;

            /// <summary>
            /// A mask value applied that determines which stencil bit is associated with the responsive feature
            /// Used to prevent STP from producing incorrect values on transparent pixels
            /// Set to 0 if no stencil data is present
            /// </summary>
            public int stencilMask;

            /// <summary>
            /// An index value that indicates which debug visualization to render in the debug view
            /// This value is only used when a valid debug view handle is provided
            /// </summary>
            public int debugViewIndex;

            /// <summary>
            /// Delta frame time for the current frame
            /// Used to compensate for inconsistent frame timings when working with motion vectors
            /// </summary>
            public float deltaTime;

            /// <summary>
            /// Delta frame time for the previous frame
            /// Used to compensate for inconsistent frame timings when working with motion vectors
            /// </summary>
            public float lastDeltaTime;

            /// <summary>
            /// Size of the current viewport in pixels
            /// Used to calculate image coordinate scaling factors
            /// </summary>
            public Vector2Int currentImageSize;

            /// <summary>
            /// Size of the previous viewport in pixels
            /// Used to calculate image coordinate scaling factors
            /// </summary>
            public Vector2Int priorImageSize;

            /// <summary>
            /// Size of the upscaled output image in pixels
            /// Used to calculate image coordinate scaling factors
            /// </summary>
            public Vector2Int outputImageSize;

            /// <summary>
            /// Number of active views in the perViewConfigs array
            /// </summary>
            public int numActiveViews;

            /// <summary>
            /// Configuration parameters that are unique per rendered view
            /// </summary>
            public PerViewConfig[] perViewConfigs;
        }

        /// <summary>
        /// Enumeration of unique types of history textures
        /// </summary>
        internal enum HistoryTextureType
        {
            DepthMotion,
            Luma,
            Convergence,
            Feedback,

            Count
        }

        /// <summary>
        /// Number of unique types of history textures used by STP
        /// </summary>
        const int kNumHistoryTextureTypes = (int)HistoryTextureType.Count;

        /// <summary>
        /// Describes the information needed to update the history context
        /// </summary>
        public struct HistoryUpdateInfo
        {
            /// <summary>
            /// Size of the target image before upscaling is applied
            /// </summary>
            public Vector2Int preUpscaleSize;

            /// <summary>
            /// Size of the target image after upscaling is applied
            /// </summary>
            public Vector2Int postUpscaleSize;

            /// <summary>
            /// True if hardware dynamic resolution scaling is active
            /// </summary>
            public bool useHwDrs;

            /// <summary>
            /// True if texture arrays are being used in the current rendering environment
            /// </summary>
            public bool useTexArray;
        }

        /// <summary>
        /// Computes a hash value that changes whenever the history context needs to be re-created
        /// </summary>
        /// <param name="hashParams">parameters used to calculate the history hash</param>
        /// <returns>A hash value that changes whenever the history context needs to be re-created</returns>
        static Hash128 ComputeHistoryHash(ref HistoryUpdateInfo info)
        {
            Hash128 hash = new Hash128();

            hash.Append(ref info.useHwDrs);
            hash.Append(ref info.useTexArray);
            hash.Append(ref info.postUpscaleSize);

            // The pre-upscale size only affects the history texture logic when hardware dynamic resolution scaling is disabled.
            if (!info.useHwDrs)
            {
                hash.Append(ref info.preUpscaleSize);
            }

            return hash;
        }

        /// <summary>
        /// Calculates the correct size for the STP low-frequency convergence texture
        /// </summary>
        /// <param name="historyTextureSize">size of the render-size history textures used by STP</param>
        /// <returns>size of the convergence texture</returns>
        static Vector2Int CalculateConvergenceTextureSize(Vector2Int historyTextureSize)
        {
            // The convergence texture is a 4x4 reduction of data computed at render size, but we must always make sure the size is rounded up.
            return new Vector2Int(CoreUtils.DivRoundUp(historyTextureSize.x, 4), CoreUtils.DivRoundUp(historyTextureSize.y, 4));
        }

        /// <summary>
        /// Opaque history information required by STP's implementation
        /// Users are expected to create their own persistent history context, update it once per frame, and provide it to
        /// STP's execution logic through the configuration structure.
        /// </summary>
        public sealed class HistoryContext : IDisposable
        {
            /// <summary>
            /// Array of history textures used by STP
            /// The array is subdivided into two sets. Each set represents the history textures for a single frame.
            /// </summary>
            RTHandle[] m_textures = new RTHandle[kNumHistoryTextureTypes * 2];

            /// <summary>
            /// Hash value that changes whenever the history context needs to be re-created
            /// </summary>
            Hash128 m_hash = Hash128.Compute(0);

            /// <summary>
            /// Updated the state of the history context based on the provided information
            /// This may result in re-allocation of resources internally if state has changed and is now incompatible or if this is the first update
            /// </summary>
            /// <param name="info">information required to update the history context</param>
            /// <returns>True if the internal history data within the context is valid after the update operation</returns>
            public bool Update(ref HistoryUpdateInfo info)
            {
                bool hasValidHistory = true;

                var hash = ComputeHistoryHash(ref info);

                if (hash != m_hash)
                {
                    hasValidHistory = false;

                    Dispose();

                    m_hash = hash;

                    // Allocate two new sets of history textures for STP based on the current settings

                    Vector2Int historyTextureSize = info.useHwDrs ? info.postUpscaleSize : info.preUpscaleSize;
                    TextureDimension texDimension = info.useTexArray ? TextureDimension.Tex2DArray : TextureDimension.Tex2D;
                    int numSlices = info.useTexArray ? TextureXR.slices : 1;

                    int width = 0;
                    int height = 0;
                    GraphicsFormat format = GraphicsFormat.None;
                    bool useDynamicScaleExplicit = false;
                    string name = "";

                    for (int historyTypeIndex = 0; historyTypeIndex < kNumHistoryTextureTypes; ++historyTypeIndex)
                    {
                        switch ((HistoryTextureType)historyTypeIndex)
                        {
                            case HistoryTextureType.DepthMotion:
                            {
                                width = historyTextureSize.x;
                                height = historyTextureSize.y;
                                format = GraphicsFormat.R32_UInt;
                                useDynamicScaleExplicit = info.useHwDrs;
                                name = "STP Depth & Motion";
                                break;
                            }
                            case HistoryTextureType.Luma:
                            {
                                width = historyTextureSize.x;
                                height = historyTextureSize.y;
                                format = GraphicsFormat.R8G8_UNorm;
                                useDynamicScaleExplicit = info.useHwDrs;
                                name = "STP Luma";
                                break;
                            }
                            case HistoryTextureType.Convergence:
                            {
                                Vector2Int convergenceSize = CalculateConvergenceTextureSize(historyTextureSize);

                                width = convergenceSize.x;
                                height = convergenceSize.y;
                                format = GraphicsFormat.R8_UNorm;
                                useDynamicScaleExplicit = info.useHwDrs;
                                name = "STP Convergence";
                                break;
                            }
                            case HistoryTextureType.Feedback:
                            {
                                width = info.postUpscaleSize.x;
                                height = info.postUpscaleSize.y;
                                format = GraphicsFormat.A2B10G10R10_UNormPack32;
                                useDynamicScaleExplicit = false;
                                name = "STP Feedback";
                                break;
                            }
                            default:
                            {
                                // Invalid history texture type
                                Debug.Assert(false);
                                break;
                            }
                        }

                        for (int frameIndex = 0; frameIndex < 2; ++frameIndex)
                        {
                            int offset = (frameIndex * kNumHistoryTextureTypes) + historyTypeIndex;

                            m_textures[offset] = RTHandles.Alloc(
                                width, height, format, numSlices, dimension: texDimension, enableRandomWrite: true,
                                name: name, useDynamicScaleExplicit: useDynamicScaleExplicit
                            );
                        }
                    }
                }

                return hasValidHistory;
            }

            internal RTHandle GetCurrentHistoryTexture(HistoryTextureType historyType, int frameIndex)
            {
                return m_textures[((frameIndex & 1) * (int)HistoryTextureType.Count) + (int)historyType];
            }

            internal RTHandle GetPreviousHistoryTexture(HistoryTextureType historyType, int frameIndex)
            {
                return m_textures[(((frameIndex & 1) ^ 1) * (int)HistoryTextureType.Count) + (int)historyType];
            }

            /// <summary>
            /// Releases the internal resources held within the history context
            /// Typically things like texture allocations
            /// </summary>
            public void Dispose()
            {
                for (int texIndex = 0; texIndex < m_textures.Length; ++texIndex)
                {
                    if (m_textures[texIndex] != null)
                    {
                        m_textures[texIndex].Release();
                        m_textures[texIndex] = null;
                    }
                }

                m_hash = Hash128.Compute(0);
            }
        }

        /// <summary>
        /// Returns a motion scaling ratio based on the difference in delta times across frames
        /// </summary>
        /// <param name="deltaTime">Time elapsed from the last frame to the current frame in seconds</param>
        /// <param name="lastDeltaTime">Time elapsed from the frame before the last frame to the last frame in seconds</param>
        /// <returns>Motion scale factor for the current frame</returns>
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

        /// <summary>
        /// Returns a matrix with the translation component removed
        /// This function is intended to be used with view matrices
        /// </summary>
        /// <param name="input">input view matrix</param>
        /// <returns>a matrix with the translation component removed</returns>
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
        /// Helper function that converts the provided Vector2 into a packed integer with two FP16 values
        /// </summary>
        /// <param name="value">input Vector2 value to be packed</param>
        /// <returns>an integer that contains the two vector components packed together as FP16 values</returns>
        static int PackVector2ToInt(Vector2 value)
        {
            uint xAsHalf = Mathf.FloatToHalf(value.x);
            uint yAsHalf = Mathf.FloatToHalf(value.y);
            return (int)(xAsHalf | (yAsHalf << 16));
        }

        /// <summary>
        /// Number of constants that contain per-view information for the setup pass
        /// NOTE: The name here is important as it's directly translated into HLSL
        /// </summary>
        [GenerateHLSL(PackingRules.Exact)]
        enum StpSetupPerViewConstants
        {
            Count = 8
        };

        /// <summary>
        /// Total number of constants used for per-view data in STP
        /// </summary>
        const int kTotalSetupViewConstantsCount = kMaxPerViewConfigs * ((int)StpSetupPerViewConstants.Count);

        /// <summary>
        /// Constant buffer layout used by STP
        /// NOTE: The name here is important as it's directly translated into HLSL
        /// </summary>
        [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
        unsafe struct StpConstantBufferData
        {
            public Vector4 _StpCommonConstant;

            public Vector4 _StpSetupConstants0;
            public Vector4 _StpSetupConstants1;
            public Vector4 _StpSetupConstants2;
            public Vector4 _StpSetupConstants3;
            public Vector4 _StpSetupConstants4;
            public Vector4 _StpSetupConstants5;

            [HLSLArray(kTotalSetupViewConstantsCount, typeof(Vector4))]
            public fixed float _StpSetupPerViewConstants[kTotalSetupViewConstantsCount * 4];

            public Vector4 _StpDilConstants0;

            public Vector4 _StpTaaConstants0;
            public Vector4 _StpTaaConstants1;
            public Vector4 _StpTaaConstants2;
            public Vector4 _StpTaaConstants3;
        }

        /// <summary>
        /// Produces constant buffer data in the format required by STP
        /// </summary>
        /// <param name="config">STP's configuration data</param>
        /// <param name="constants">constant buffer data structure required by STP</param>
        static void PopulateConstantData(ref Config config, ref StpConstantBufferData constants)
        {
            Assert.IsTrue(Mathf.IsPowerOfTwo(config.noiseTexture.width));

            //
            // Common
            //

            // [DebugViewIndex | StencilMask | HasValidHistory | (Width - 1)]
            int packedBlueNoiseWidthMinusOne = (config.noiseTexture.width - 1) & 0xFF;
            int packedHasValidHistory = (config.hasValidHistory ? 1 : 0) << 8;
            int packedStencilMask = (config.stencilMask & 0xFF) << 16;
            int packedDebugViewIndex = (config.debugViewIndex & 0xFF) << 24;

            int constant0 = packedStencilMask | packedHasValidHistory | packedBlueNoiseWidthMinusOne | packedDebugViewIndex;

            // Compute values used for linear depth conversion
            // These values are normally in the _ZBufferParams constant, but we re-compute them here since this constant is defined differently across SRPs
            float zBufferParamZ = (config.farPlane - config.nearPlane) / (config.nearPlane * config.farPlane);
            float zBufferParamW = 1.0f / config.farPlane;

            constants._StpCommonConstant = new Vector4(BitConverter.Int32BitsToSingle(constant0), zBufferParamZ, zBufferParamW, 0.0f);

            //
            // NOTE: The logic below is effectively a C# port of the HLSL constant setup logic found in Stp.hlsl
            //       The C# code attempts to be as close as possible to the HLSL in order to simplify maintenance.
            //

            //
            // Setup
            //

            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kRcpC := 1.0 / size of current input image in pixels.
            constants._StpSetupConstants0.x = (1.0f / config.currentImageSize.x);
            constants._StpSetupConstants0.y = (1.0f / config.currentImageSize.y);
            // StpF2 kHalfRcpC := 0.5 / size of current input image in pixels.
            constants._StpSetupConstants0.z = (0.5f / config.currentImageSize.x);
            constants._StpSetupConstants0.w = (0.5f / config.currentImageSize.y);
            //------------------------------------------------------------------------------------------------------------------------------
            // Grab jitter for current and prior frames.
            Vector2 jitP = Jit16(config.frameIndex - 1);
            Vector2 jitC = Jit16(config.frameIndex);
            // StpF2 kJitCRcpCUnjitPRcpP := Map current into prior frame.
            constants._StpSetupConstants1.x = (jitC.x / config.currentImageSize.x - jitP.x / config.priorImageSize.x);
            constants._StpSetupConstants1.y = (jitC.y / config.currentImageSize.y - jitP.y / config.priorImageSize.y);
            // StpF2 kJitCRcpC := Take {0 to 1} position in current image, and map back to {0 to 1} position in feedback (removes jitter).
            constants._StpSetupConstants1.z = jitC.x / config.currentImageSize.x;
            constants._StpSetupConstants1.w = jitC.y / config.currentImageSize.y;
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kF := size of feedback (aka output) in pixels.
            constants._StpSetupConstants2.x = config.outputImageSize.x;
            constants._StpSetupConstants2.y = config.outputImageSize.y;
            // StpF2 kDepth := Copied logic from StpZCon().
            float k0 = (1.0f / config.nearPlane);
            float k1 = (1.0f / Mathf.Log(k0 * config.farPlane, 2.0f));
            constants._StpSetupConstants2.z = k0;
            constants._StpSetupConstants2.w = k1;
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF4 kOS := Scale and bias to check for out of bounds (and kill feedback).
            // Scaled and biased output needs to {-1 out of bounds, >-1 in bounds, <1 in bounds, 1 out of bounds}.
            Vector2 s;
            // Undo 'pM' scaling, and multiply by 2 (as this needs to be -1 to 1 at edge of acceptable reprojection).
            s.x = 2.0f;
            s.y = 2.0f;
            // Scaling to push outside safe reprojection over 1.
            s.x *= (config.priorImageSize.x / (config.priorImageSize.x + 4.0f));
            s.y *= (config.priorImageSize.y / (config.priorImageSize.y + 4.0f));
            constants._StpSetupConstants3.x = s[0];
            constants._StpSetupConstants3.y = s[1];
            // Factor out subtracting off the mid point scaled by the multiply term.
            constants._StpSetupConstants3.z = (-0.5f * s[0]);
            constants._StpSetupConstants3.w = (-0.5f * s[1]);
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kUnDepth := Copied logic from StpZUnCon().
            constants._StpSetupConstants4.x = Mathf.Log(config.farPlane / config.nearPlane, 2.0f);
            constants._StpSetupConstants4.y = config.nearPlane;
            // kMotionMatch
            constants._StpSetupConstants4.z = config.enableMotionScaling ? CalculateMotionScale(config.deltaTime, config.lastDeltaTime) : 1.0f;
            // Unused for now.
            constants._StpSetupConstants4.w = 0.0f;
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kC := Size of current input image in pixels.
            constants._StpSetupConstants5.x = config.currentImageSize.x;
            constants._StpSetupConstants5.y = config.currentImageSize.y;
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kFS := scale factor used to convert from feedback uv space to reduction uv space
            constants._StpSetupConstants5.z = config.outputImageSize.x / (Mathf.Ceil(config.outputImageSize.x / 4.0f) * 4.0f);
            constants._StpSetupConstants5.w = config.outputImageSize.y / (Mathf.Ceil(config.outputImageSize.y / 4.0f) * 4.0f);

            // Per View
            for (uint viewIndex = 0; viewIndex < config.numActiveViews; ++viewIndex)
            {
                uint baseViewDataOffset = viewIndex * ((int)StpSetupPerViewConstants.Count) * 4;
                var perViewConfig = config.perViewConfigs[viewIndex];

                //------------------------------------------------------------------------------------------------------------------------------
                // See header docs in "STATIC GEOMETRY MOTION FORWARD PROJECTION".
                Vector4 prjPriABEF;
                prjPriABEF.x = perViewConfig.lastProj[0, 0];

                // NOTE: Unity flips the Y axis inside the projection matrix. STP requires a non-flipped Y axis, so we undo the flip here with abs
                prjPriABEF.y = Mathf.Abs(perViewConfig.lastProj[1, 1]);

                // TODO: We need to understand why we need to negate these values for the inverse projection in order to get correct results.
                prjPriABEF.z = -perViewConfig.lastProj[0, 2];
                prjPriABEF.w = -perViewConfig.lastProj[1, 2];

                Vector4 prjPriCDGH;
                prjPriCDGH.x = perViewConfig.lastProj[2, 2];
                prjPriCDGH.y = perViewConfig.lastProj[2, 3];
                prjPriCDGH.z = perViewConfig.lastProj[3, 2];
                prjPriCDGH.w = perViewConfig.lastProj[3, 3];

                Vector4 prjCurABEF;
                prjCurABEF.x = perViewConfig.currentProj[0, 0];

                // NOTE: Unity flips the Y axis inside the projection matrix. STP requires a non-flipped Y axis, so we undo the flip here with abs
                prjCurABEF.y = Mathf.Abs(perViewConfig.currentProj[1, 1]);

                prjCurABEF.z = perViewConfig.currentProj[0, 2];
                prjCurABEF.w = perViewConfig.currentProj[1, 2];

                Vector4 prjCurCDGH;
                prjCurCDGH.x = perViewConfig.currentProj[2, 2];
                prjCurCDGH.y = perViewConfig.currentProj[2, 3];
                prjCurCDGH.z = perViewConfig.currentProj[3, 2];
                prjCurCDGH.w = perViewConfig.currentProj[3, 3];

                Matrix4x4 forwardTransform = ExtractRotation(perViewConfig.currentView) *
                                             Matrix4x4.Translate(-perViewConfig.currentView.GetColumn(3)) *
                                             Matrix4x4.Translate(perViewConfig.lastView.GetColumn(3)) *
                                             ExtractRotation(perViewConfig.lastView).transpose;

                Vector4 forIJKL = forwardTransform.GetRow(0);
                Vector4 forMNOP = forwardTransform.GetRow(1);
                Vector4 forQRST = forwardTransform.GetRow(2);

                Vector4 prjPrvABEF;
                prjPrvABEF.x = perViewConfig.lastLastProj[0, 0];

                // NOTE: Unity flips the Y axis inside the projection matrix. STP requires a non-flipped Y axis, so we undo the flip here with abs
                prjPrvABEF.y = Mathf.Abs(perViewConfig.lastLastProj[1, 1]);

                prjPrvABEF.z = perViewConfig.lastLastProj[0, 2];
                prjPrvABEF.w = perViewConfig.lastLastProj[1, 2];

                Vector4 prjPrvCDGH;
                prjPrvCDGH.x = perViewConfig.lastLastProj[2, 2];
                prjPrvCDGH.y = perViewConfig.lastLastProj[2, 3];
                prjPrvCDGH.z = perViewConfig.lastLastProj[3, 2];
                prjPrvCDGH.w = perViewConfig.lastLastProj[3, 3];

                Matrix4x4 backwardTransform = ExtractRotation(perViewConfig.lastLastView) *
                                              Matrix4x4.Translate(-perViewConfig.lastLastView.GetColumn(3)) *
                                              Matrix4x4.Translate(perViewConfig.lastView.GetColumn(3)) *
                                              ExtractRotation(perViewConfig.lastView).transpose;

                Vector4 bckIJKL = backwardTransform.GetRow(0);
                Vector4 bckMNOP = backwardTransform.GetRow(1);
                Vector4 bckQRST = backwardTransform.GetRow(2);

                unsafe
                {
                    // Forwards

                    // k0123
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 0] = prjPriCDGH.z / prjPriABEF.x;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 1] = prjPriCDGH.w / prjPriABEF.x;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 2] = prjPriABEF.z / prjPriABEF.x;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 3] = prjPriCDGH.z / prjPriABEF.y;
                    // k4567
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 4] = prjPriCDGH.w / prjPriABEF.y;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 5] = prjPriABEF.w / prjPriABEF.y;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 6] = forIJKL.x * prjCurABEF.x + forQRST.x * prjCurABEF.z;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 7] = forIJKL.y * prjCurABEF.x + forQRST.y * prjCurABEF.z;
                    // k89AB
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 8] = forIJKL.z * prjCurABEF.x + forQRST.z * prjCurABEF.z;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 9] = forIJKL.w * prjCurABEF.x + forQRST.w * prjCurABEF.z;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 10] = forMNOP.x * prjCurABEF.y + forQRST.x * prjCurABEF.w;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 11] = forMNOP.y * prjCurABEF.y + forQRST.y * prjCurABEF.w;
                    // kCDEF
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 12] = forMNOP.z * prjCurABEF.y + forQRST.z * prjCurABEF.w;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 13] = forMNOP.w * prjCurABEF.y + forQRST.w * prjCurABEF.w;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 14] = forQRST.x * prjCurCDGH.z;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 15] = forQRST.y * prjCurCDGH.z;
                    // kGHIJ
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 16] = forQRST.z * prjCurCDGH.z;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 17] = forQRST.w * prjCurCDGH.z + prjCurCDGH.w;

                    // Backwards

                    constants._StpSetupPerViewConstants[baseViewDataOffset + 18] = bckIJKL.x * prjPrvABEF.x + bckQRST.x * prjPrvABEF.z;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 19] = bckIJKL.y * prjPrvABEF.x + bckQRST.y * prjPrvABEF.z;
                    // kKLMN
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 20] = bckIJKL.z * prjPrvABEF.x + bckQRST.z * prjPrvABEF.z;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 21] = bckIJKL.w * prjPrvABEF.x + bckQRST.w * prjPrvABEF.z;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 22] = bckMNOP.x * prjPrvABEF.y + bckQRST.x * prjPrvABEF.w;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 23] = bckMNOP.y * prjPrvABEF.y + bckQRST.y * prjPrvABEF.w;
                    // kOPQR
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 24] = bckMNOP.z * prjPrvABEF.y + bckQRST.z * prjPrvABEF.w;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 25] = bckMNOP.w * prjPrvABEF.y + bckQRST.w * prjPrvABEF.w;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 26] = bckQRST.x * prjPrvCDGH.z;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 27] = bckQRST.y * prjPrvCDGH.z;
                    // kST
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 28] = bckQRST.z * prjPrvCDGH.z;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 29] = bckQRST.w * prjPrvCDGH.z + prjPrvCDGH.w;
                    // Unused
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 30] = 0.0f;
                    constants._StpSetupPerViewConstants[baseViewDataOffset + 31] = 0.0f;
                }
            }
            //------------------------------------------------------------------------------------------------------------------------------

            //
            // Dilation
            //
            // StpF2 kRcpR := 4/size of current input image in pixels.
            constants._StpDilConstants0.x = 4.0f / config.currentImageSize.x;
            constants._StpDilConstants0.y = 4.0f / config.currentImageSize.y;
            // StpU2 kR := size/4 of the current input image in pixels.
            // Used for pass merging (DIL and SAA), since convergence is 1/16 area of input, must check position.
            constants._StpDilConstants0.z = BitConverter.Int32BitsToSingle(config.currentImageSize.x >> 2);
            constants._StpDilConstants0.w = BitConverter.Int32BitsToSingle(config.currentImageSize.y >> 2);

            //
            // TAA
            //

            //------------------------------------------------------------------------------------------------------------------------------
            // Conversion from integer pix position to center pix float pixel position in image for current input.
            //  xy := multiply term (M) --- Scale by 1/imgF to get to {0 to 1}.
            //  zw := addition term (A) --- Add 0.5*M to get to center of pixel, then subtract jitC to undo jitter.
            // StpF2 kCRcpF.
            constants._StpTaaConstants0.x = (((float)config.currentImageSize.x) / config.outputImageSize.x);
            constants._StpTaaConstants0.y = (((float)config.currentImageSize.y) / config.outputImageSize.y);
            // StpF2 kHalfCRcpFUnjitC.
            constants._StpTaaConstants0.z = (0.5f * config.currentImageSize.x / config.outputImageSize.x - jitC.x);
            constants._StpTaaConstants0.w = (0.5f * config.currentImageSize.y / config.outputImageSize.y - jitC.y);
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kRcpC := 1/size of current input image in pixels.
            constants._StpTaaConstants1.x = (1.0f / config.currentImageSize.x);
            constants._StpTaaConstants1.y = (1.0f / config.currentImageSize.y);
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kRcpF := 1/size of feedback image (aka output) in pixels.
            constants._StpTaaConstants1.z = (1.0f / config.outputImageSize.x);
            constants._StpTaaConstants1.w = (1.0f / config.outputImageSize.y);
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kHalfRcpF := 0.5/size of feedback image (aka output) in pixels.
            constants._StpTaaConstants2.x = (0.5f / config.outputImageSize.x);
            constants._StpTaaConstants2.y = (0.5f / config.outputImageSize.y);
            //------------------------------------------------------------------------------------------------------------------------------
            // Conversion from a {0 to 1} position in current input to feedback.
            // StpH3 kJitCRcpC0 := jitC / image image size in pixels + {-0.5/size, +0.5/size} of current input image in pixels.
            constants._StpTaaConstants2.z = jitC.x / config.currentImageSize.x - 0.5f / config.currentImageSize.x;
            constants._StpTaaConstants2.w = jitC.y / config.currentImageSize.y + 0.5f / config.currentImageSize.y;
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kHalfRcpC := 0.5/size of current input image in pixels.
            constants._StpTaaConstants3.x = 0.5f / config.currentImageSize.x;
            constants._StpTaaConstants3.y = 0.5f / config.currentImageSize.y;
            //------------------------------------------------------------------------------------------------------------------------------
            // StpF2 kF := size of feedback image in pixels.
            constants._StpTaaConstants3.z = config.outputImageSize.x;
            constants._StpTaaConstants3.w = config.outputImageSize.y;
        }

        /// <summary>
        /// Shader resource ids used to communicate with the STP shader implementation
        /// </summary>
        static class ShaderResources
        {
            public static readonly int _StpConstantBufferData = Shader.PropertyToID("StpConstantBufferData");
            public static readonly int _StpBlueNoiseIn = Shader.PropertyToID("_StpBlueNoiseIn");
            public static readonly int _StpDebugOut = Shader.PropertyToID("_StpDebugOut");
            public static readonly int _StpInputColor = Shader.PropertyToID("_StpInputColor");
            public static readonly int _StpInputDepth = Shader.PropertyToID("_StpInputDepth");
            public static readonly int _StpInputMotion = Shader.PropertyToID("_StpInputMotion");
            public static readonly int _StpInputStencil = Shader.PropertyToID("_StpInputStencil");
            public static readonly int _StpIntermediateColor = Shader.PropertyToID("_StpIntermediateColor");
            public static readonly int _StpIntermediateConvergence = Shader.PropertyToID("_StpIntermediateConvergence");
            public static readonly int _StpIntermediateWeights = Shader.PropertyToID("_StpIntermediateWeights");
            public static readonly int _StpPriorLuma = Shader.PropertyToID("_StpPriorLuma");
            public static readonly int _StpLuma = Shader.PropertyToID("_StpLuma");
            public static readonly int _StpPriorDepthMotion = Shader.PropertyToID("_StpPriorDepthMotion");
            public static readonly int _StpDepthMotion = Shader.PropertyToID("_StpDepthMotion");
            public static readonly int _StpPriorFeedback = Shader.PropertyToID("_StpPriorFeedback");
            public static readonly int _StpFeedback = Shader.PropertyToID("_StpFeedback");
            public static readonly int _StpPriorConvergence = Shader.PropertyToID("_StpPriorConvergence");
            public static readonly int _StpConvergence = Shader.PropertyToID("_StpConvergence");
            public static readonly int _StpOutput = Shader.PropertyToID("_StpOutput");
        }

        /// <summary>
        /// Shader keyword strings used to configure the STP shader implementation
        /// </summary>
        static class ShaderKeywords
        {
            public static readonly string EnableDebugMode = "ENABLE_DEBUG_MODE";
            public static readonly string EnableLargeKernel = "ENABLE_LARGE_KERNEL";
            public static readonly string EnableStencilResponsive = "ENABLE_STENCIL_RESPONSIVE";
            public static readonly string DisableTexture2DXArray = "DISABLE_TEXTURE2D_X_ARRAY";
        }

        /// <summary>
        /// Contains the compute shaders used during STP's passes
        /// </summary>
        [Serializable]
        [SupportedOnRenderPipeline]
        [Categorization.CategoryInfo(Name = "R: STP", Order = 1000)]
        [Categorization.ElementInfo(Order = 0), HideInInspector]
        internal class RuntimeResources : IRenderPipelineResources
        {
            public int version => 0;

            [SerializeField, ResourcePath("Runtime/STP/StpSetup.compute")]
            private ComputeShader m_setupCS;

            public ComputeShader setupCS
            {
                get => m_setupCS;
                set => this.SetValueAndNotify(ref m_setupCS, value);
            }

            [SerializeField, ResourcePath("Runtime/STP/StpPreTaa.compute")]
            private ComputeShader m_preTaaCS;

            public ComputeShader preTaaCS
            {
                get => m_preTaaCS;
                set => this.SetValueAndNotify(ref m_preTaaCS, value);
            }

            [SerializeField, ResourcePath("Runtime/STP/StpTaa.compute")]
            private ComputeShader m_taaCS;

            public ComputeShader taaCS
            {
                get => m_taaCS;
                set => this.SetValueAndNotify(ref m_taaCS, value);
            }
        }

        /// <summary>
        /// Profiling identifiers associated with STP's passes
        /// </summary>
        enum ProfileId
        {
            StpSetup,
            StpPreTaa,
            StpTaa
        }

        /// <summary>
        /// Integer value used to identify when STP is running on a Qualcomm GPU
        /// </summary>
        static readonly int kQualcommVendorId = 0x5143;

        /// <summary>
        /// Information required for STP's setup pass
        /// </summary>
        class SetupData
        {
            public ComputeShader cs;
            public int kernelIndex;
            public int viewCount;
            public Vector2Int dispatchSize;

            public StpConstantBufferData constantBufferData;

            // Common
            public TextureHandle noiseTexture;
            public TextureHandle debugView;

            // Inputs
            public TextureHandle inputColor;
            public TextureHandle inputDepth;
            public TextureHandle inputMotion;
            public TextureHandle inputStencil;

            // Intermediates
            public TextureHandle intermediateColor;
            public TextureHandle intermediateConvergence;

            // History
            public TextureHandle priorDepthMotion;
            public TextureHandle depthMotion;
            public TextureHandle priorLuma;
            public TextureHandle luma;
            public TextureHandle priorFeedback;
            public TextureHandle priorConvergence;
        }

        /// <summary>
        /// Information required for STP's Pre-TAA pass
        /// </summary>
        class PreTaaData
        {
            public ComputeShader cs;
            public int kernelIndex;
            public int viewCount;
            public Vector2Int dispatchSize;

            // Common
            public TextureHandle noiseTexture;
            public TextureHandle debugView;

            // Inputs
            public TextureHandle intermediateConvergence;

            // Intermediates
            public TextureHandle intermediateWeights;

            // History
            public TextureHandle luma;
            public TextureHandle convergence;
        }

        /// <summary>
        /// Information required for STP's TAA pass
        /// </summary>
        class TaaData
        {
            public ComputeShader cs;
            public int kernelIndex;
            public int viewCount;
            public Vector2Int dispatchSize;

            // Common
            public TextureHandle noiseTexture;
            public TextureHandle debugView;

            // Inputs
            public TextureHandle intermediateColor;
            public TextureHandle intermediateWeights;

            // History
            public TextureHandle priorFeedback;
            public TextureHandle depthMotion;
            public TextureHandle convergence;

            // Outputs
            public TextureHandle feedback;
            public TextureHandle output;
        }

        // Internal helper function used to streamline usage of the render graph API
        static TextureHandle UseTexture(IBaseRenderGraphBuilder builder, TextureHandle texture, AccessFlags flags = AccessFlags.Read)
        {
            builder.UseTexture(texture, flags);
            return texture;
        }

        /// <summary>
        /// Executes the STP technique using the provided configuration in the target render graph
        /// </summary>
        /// <param name="renderGraph">render graph to execute STP within</param>
        /// <param name="config">configuration parameters for STP</param>
        /// <returns>Texture handle that contains the upscaled color output</returns>
        public static TextureHandle Execute(RenderGraph renderGraph, ref Config config)
        {
            var runtimeResources = GraphicsSettings.GetRenderPipelineSettings<RuntimeResources>();

            // Temporarily wrap the noise texture in an RTHandle so it can be imported into render graph
            var noiseTexture = config.noiseTexture;
            RTHandleStaticHelpers.SetRTHandleStaticWrapper(noiseTexture);
            var noiseTextureRtHandle = RTHandleStaticHelpers.s_RTHandleWrapper;

            RenderTargetInfo noiseTextureInfo;
            noiseTextureInfo.width = noiseTexture.width;
            noiseTextureInfo.height = noiseTexture.height;
            noiseTextureInfo.volumeDepth = 1;
            noiseTextureInfo.msaaSamples = 1;
            noiseTextureInfo.format = noiseTexture.graphicsFormat;
            noiseTextureInfo.bindMS = false;

            TextureHandle noiseTextureHandle = renderGraph.ImportTexture(noiseTextureRtHandle, noiseTextureInfo);

            var priorDepthMotion = config.historyContext.GetPreviousHistoryTexture(HistoryTextureType.DepthMotion, config.frameIndex);
            var priorLuma = config.historyContext.GetPreviousHistoryTexture(HistoryTextureType.Luma, config.frameIndex);
            var priorConvergence = config.historyContext.GetPreviousHistoryTexture(HistoryTextureType.Convergence, config.frameIndex);
            var priorFeedback = config.historyContext.GetPreviousHistoryTexture(HistoryTextureType.Feedback, config.frameIndex);

            var depthMotion = config.historyContext.GetCurrentHistoryTexture(HistoryTextureType.DepthMotion, config.frameIndex);
            var luma = config.historyContext.GetCurrentHistoryTexture(HistoryTextureType.Luma, config.frameIndex);
            var convergence = config.historyContext.GetCurrentHistoryTexture(HistoryTextureType.Convergence, config.frameIndex);
            var feedback = config.historyContext.GetCurrentHistoryTexture(HistoryTextureType.Feedback, config.frameIndex);

            // Resize the current render-size history textures if hardware dynamic scaling is enabled
            if (config.enableHwDrs)
            {
                depthMotion.rt.ApplyDynamicScale();
                luma.rt.ApplyDynamicScale();
                convergence.rt.ApplyDynamicScale();
            }

            Vector2Int intermediateSize = config.enableHwDrs ? config.outputImageSize : config.currentImageSize;

            // Enable the large 128 wide kernel whenever STP runs on Qualcomm GPUs.
            // These GPUs require larger compute work groups in order to reach maximum FP16 ALU efficiency
            bool enableLargeKernel = SystemInfo.graphicsDeviceVendorID == kQualcommVendorId;

            Vector2Int kernelSize = new Vector2Int(8, enableLargeKernel ? 16 : 8);

            SetupData setupData;

            using (var builder = renderGraph.AddComputePass<SetupData>("STP Setup", out var passData, ProfilingSampler.Get(ProfileId.StpSetup)))
            {
                passData.cs = runtimeResources.setupCS;
                passData.cs.shaderKeywords = null;

                if (enableLargeKernel)
                    passData.cs.EnableKeyword(ShaderKeywords.EnableLargeKernel);

                if (!config.enableTexArray)
                    passData.cs.EnableKeyword(ShaderKeywords.DisableTexture2DXArray);

                // Populate the constant buffer data structure in the render graph pass data
                // This data will be uploaded to the GPU when the node executes later in the frame
                PopulateConstantData(ref config, ref passData.constantBufferData);

                passData.noiseTexture = UseTexture(builder, noiseTextureHandle);

                if (config.debugView.IsValid())
                {
                    passData.cs.EnableKeyword(ShaderKeywords.EnableDebugMode);
                    passData.debugView = UseTexture(builder, config.debugView, AccessFlags.WriteAll);
                }

                passData.kernelIndex = passData.cs.FindKernel("StpSetup");
                passData.viewCount = config.numActiveViews;
                passData.dispatchSize = new Vector2Int(
                    CoreUtils.DivRoundUp(config.currentImageSize.x, kernelSize.x),
                    CoreUtils.DivRoundUp(config.currentImageSize.y, kernelSize.y)
                );

                passData.inputColor = UseTexture(builder, config.inputColor);
                passData.inputDepth = UseTexture(builder, config.inputDepth);
                passData.inputMotion = UseTexture(builder, config.inputMotion);

                if (config.inputStencil.IsValid())
                {
                    passData.cs.EnableKeyword(ShaderKeywords.EnableStencilResponsive);
                    passData.inputStencil = UseTexture(builder, config.inputStencil);
                }

                passData.intermediateColor = UseTexture(builder, renderGraph.CreateTexture(new TextureDesc(intermediateSize.x, intermediateSize.y, config.enableHwDrs, config.enableTexArray)
                {
                    name = "STP Intermediate Color",
                    format = GraphicsFormat.A2B10G10R10_UNormPack32,
                    enableRandomWrite = true
                }), AccessFlags.WriteAll);

                Vector2Int convergenceSize = CalculateConvergenceTextureSize(intermediateSize);
                passData.intermediateConvergence = UseTexture(builder, renderGraph.CreateTexture(new TextureDesc(convergenceSize.x, convergenceSize.y, config.enableHwDrs, config.enableTexArray)
                {
                    name = "STP Intermediate Convergence",
                    format = GraphicsFormat.R8_UNorm,
                    enableRandomWrite = true
                }), AccessFlags.WriteAll);

                passData.priorDepthMotion = UseTexture(builder, renderGraph.ImportTexture(priorDepthMotion));
                passData.depthMotion = UseTexture(builder, renderGraph.ImportTexture(depthMotion), AccessFlags.WriteAll);
                passData.priorLuma = UseTexture(builder, renderGraph.ImportTexture(priorLuma));
                passData.luma = UseTexture(builder, renderGraph.ImportTexture(luma), AccessFlags.WriteAll);

                passData.priorFeedback = UseTexture(builder, renderGraph.ImportTexture(priorFeedback));
                passData.priorConvergence = UseTexture(builder, renderGraph.ImportTexture(priorConvergence));

                builder.SetRenderFunc(
                    (SetupData data, ComputeGraphContext ctx) =>
                    {
                        // Update the constant buffer data on the GPU
                        // TODO: Fix usage of m_WrappedCommandBuffer here once NRP support is added to ConstantBuffer.cs
                        ConstantBuffer.UpdateData(ctx.cmd.m_WrappedCommandBuffer, data.constantBufferData);

                        ConstantBuffer.Set<StpConstantBufferData>(data.cs, ShaderResources._StpConstantBufferData);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpBlueNoiseIn, data.noiseTexture);

                        if (data.debugView.IsValid())
                            ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpDebugOut, data.debugView);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpInputColor, data.inputColor);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpInputDepth, data.inputDepth);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpInputMotion, data.inputMotion);

                        if (data.inputStencil.IsValid())
                            ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpInputStencil, data.inputStencil, 0, RenderTextureSubElement.Stencil);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpIntermediateColor, data.intermediateColor);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpIntermediateConvergence, data.intermediateConvergence);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpPriorDepthMotion, data.priorDepthMotion);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpDepthMotion, data.depthMotion);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpPriorLuma, data.priorLuma);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpLuma, data.luma);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpPriorFeedback, data.priorFeedback);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpPriorConvergence, data.priorConvergence);

                        ctx.cmd.DispatchCompute(data.cs, data.kernelIndex, data.dispatchSize.x, data.dispatchSize.y, data.viewCount);
                    });

                setupData = passData;
            }

            PreTaaData preTaaData;

            using (var builder = renderGraph.AddComputePass<PreTaaData>("STP Pre-TAA", out var passData, ProfilingSampler.Get(ProfileId.StpPreTaa)))
            {
                passData.cs = runtimeResources.preTaaCS;
                passData.cs.shaderKeywords = null;

                if (enableLargeKernel)
                    passData.cs.EnableKeyword(ShaderKeywords.EnableLargeKernel);

                if (!config.enableTexArray)
                    passData.cs.EnableKeyword(ShaderKeywords.DisableTexture2DXArray);

                passData.noiseTexture = UseTexture(builder, noiseTextureHandle);

                if (config.debugView.IsValid())
                {
                    passData.cs.EnableKeyword(ShaderKeywords.EnableDebugMode);
                    passData.debugView = UseTexture(builder, config.debugView, AccessFlags.ReadWrite);
                }

                passData.kernelIndex = passData.cs.FindKernel("StpPreTaa");
                passData.viewCount = config.numActiveViews;
                passData.dispatchSize = new Vector2Int(
                    CoreUtils.DivRoundUp(config.currentImageSize.x, kernelSize.x),
                    CoreUtils.DivRoundUp(config.currentImageSize.y, kernelSize.y)
                );

                passData.intermediateConvergence = UseTexture(builder, setupData.intermediateConvergence);

                passData.intermediateWeights = UseTexture(builder, renderGraph.CreateTexture(new TextureDesc(intermediateSize.x, intermediateSize.y, config.enableHwDrs, config.enableTexArray)
                {
                    name = "STP Intermediate Weights",
                    format = GraphicsFormat.R8_UNorm,
                    enableRandomWrite = true
                }), AccessFlags.WriteAll);

                passData.luma = UseTexture(builder, renderGraph.ImportTexture(luma));
                passData.convergence = UseTexture(builder, renderGraph.ImportTexture(convergence), AccessFlags.WriteAll);

                builder.SetRenderFunc(
                    (PreTaaData data, ComputeGraphContext ctx) =>
                    {
                        ConstantBuffer.Set<StpConstantBufferData>(data.cs, ShaderResources._StpConstantBufferData);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpBlueNoiseIn, data.noiseTexture);

                        if (data.debugView.IsValid())
                            ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpDebugOut, data.debugView);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpIntermediateConvergence, data.intermediateConvergence);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpIntermediateWeights, data.intermediateWeights);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpLuma, data.luma);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpConvergence, data.convergence);

                        ctx.cmd.DispatchCompute(data.cs, data.kernelIndex, data.dispatchSize.x, data.dispatchSize.y, data.viewCount);
                    });

                preTaaData = passData;
            }

            TaaData taaData;

            using (var builder = renderGraph.AddComputePass<TaaData>("STP TAA", out var passData, ProfilingSampler.Get(ProfileId.StpTaa)))
            {
                passData.cs = runtimeResources.taaCS;
                passData.cs.shaderKeywords = null;

                if (enableLargeKernel)
                    passData.cs.EnableKeyword(ShaderKeywords.EnableLargeKernel);

                if (!config.enableTexArray)
                    passData.cs.EnableKeyword(ShaderKeywords.DisableTexture2DXArray);

                passData.noiseTexture = UseTexture(builder, noiseTextureHandle);

                if (config.debugView.IsValid())
                {
                    passData.cs.EnableKeyword(ShaderKeywords.EnableDebugMode);
                    passData.debugView = UseTexture(builder, config.debugView, AccessFlags.ReadWrite);
                }

                passData.kernelIndex = passData.cs.FindKernel("StpTaa");
                passData.viewCount = config.numActiveViews;
                passData.dispatchSize = new Vector2Int(
                    CoreUtils.DivRoundUp(config.outputImageSize.x, kernelSize.x),
                    CoreUtils.DivRoundUp(config.outputImageSize.y, kernelSize.y)
                );

                passData.intermediateColor = UseTexture(builder, setupData.intermediateColor);
                passData.intermediateWeights = UseTexture(builder, preTaaData.intermediateWeights);

                passData.priorFeedback = UseTexture(builder, renderGraph.ImportTexture(priorFeedback));
                passData.depthMotion = UseTexture(builder, renderGraph.ImportTexture(depthMotion));
                passData.convergence = UseTexture(builder, renderGraph.ImportTexture(convergence));

                passData.feedback = UseTexture(builder, renderGraph.ImportTexture(feedback), AccessFlags.WriteAll);

                passData.output = UseTexture(builder, config.destination, AccessFlags.WriteAll);

                builder.SetRenderFunc(
                    (TaaData data, ComputeGraphContext ctx) =>
                    {
                        ConstantBuffer.Set<StpConstantBufferData>(data.cs, ShaderResources._StpConstantBufferData);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpBlueNoiseIn, data.noiseTexture);

                        if (data.debugView.IsValid())
                            ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpDebugOut, data.debugView);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpIntermediateColor, data.intermediateColor);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpIntermediateWeights, data.intermediateWeights);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpPriorFeedback, data.priorFeedback);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpDepthMotion, data.depthMotion);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpConvergence, data.convergence);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpFeedback, data.feedback);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StpOutput, data.output);

                        ctx.cmd.DispatchCompute(data.cs, data.kernelIndex, data.dispatchSize.x, data.dispatchSize.y, data.viewCount);
                    });

                taaData = passData;
            }

            return taaData.output;
        }
    }
}
