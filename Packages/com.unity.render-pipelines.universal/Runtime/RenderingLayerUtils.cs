using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Helper class for handling rendering layers.
    /// </summary>
    internal static class RenderingLayerUtils
    {
        public enum Event
        {
            DepthNormalPrePass,
            Opaque,
        }

        public enum MaskSize
        {
            Bits8,
            Bits16,
            Bits24,
            Bits32,
        }

        public static void CombineRendererEvents(bool isDeferred, int msaaSampleCount, Event rendererEvent, ref Event combinedEvent)
        {
            // Rendering layers can not use MSAA resolve, because it encodes integer
            if (msaaSampleCount > 1 && !isDeferred)
                combinedEvent = Event.DepthNormalPrePass;

            // Otherwise we combine them by selecting the min of the two...
            else
                combinedEvent = Combine(combinedEvent, rendererEvent);
        }

        /// <summary>
        /// Returns True if <see cref="UniversalRenderer"/> will require rendering layers texture.
        /// </summary>
        /// <param name="universalRenderer"></param>
        /// <param name="rendererFeatures">List of renderer features used by the renderer</param>
        /// <param name="msaaSampleCount">MSAA sample count</param>
        /// <param name="combinedEvent">Event at which rendering layers texture needs to be created</param>
        /// <param name="combinedMaskSize">The mask size of rendering layers texture</param>
        public static bool RequireRenderingLayers(UniversalRenderer universalRenderer, List<ScriptableRendererFeature> rendererFeatures, int msaaSampleCount, out Event combinedEvent, out MaskSize combinedMaskSize)
        {
            RenderingMode renderingMode = universalRenderer.renderingModeActual;
            bool accurateGBufferNormals = universalRenderer.accurateGbufferNormals;
            return RequireRenderingLayers(rendererFeatures, renderingMode, accurateGBufferNormals, msaaSampleCount,
                out combinedEvent, out combinedMaskSize);
        }

        internal static bool RequireRenderingLayers(List<ScriptableRendererFeature> rendererFeatures, RenderingMode renderingMode, bool accurateGbufferNormals, int msaaSampleCount, out Event combinedEvent, out MaskSize combinedMaskSize)
        {
            combinedEvent = Event.Opaque;
            combinedMaskSize = MaskSize.Bits8;

            bool isDeferred = renderingMode == RenderingMode.Deferred || renderingMode == RenderingMode.DeferredPlus;
            bool result = false;
            foreach (var rendererFeature in rendererFeatures)
            {
                if (rendererFeature.isActive)
                {
                    result |= rendererFeature.RequireRenderingLayers(isDeferred, accurateGbufferNormals, out Event rendererEvent, out MaskSize rendererMaskSize);
                    combinedEvent = Combine(combinedEvent, rendererEvent);
                    combinedMaskSize = Combine(combinedMaskSize, rendererMaskSize);
                }
            }

            // Rendering layers can not use MSAA resolve, because it encodes integer
            if (msaaSampleCount > 1 && combinedEvent == Event.Opaque)
                combinedEvent = Event.DepthNormalPrePass;

            // Make sure texture has enough bits to encode all rendering layers in urp global settings
            if (UniversalRenderPipelineGlobalSettings.instance)
            {
                int count =  RenderingLayerMask.GetRenderingLayerCount();
                MaskSize maskSize = GetMaskSize(count);
                combinedMaskSize = Combine(combinedMaskSize, maskSize);
            }

            return result;
        }

        /// <summary>
        /// Setups properties that are needed for accessing rendering layers texture.
        /// </summary>
        /// <param name="cmd">Used command buffer</param>
        /// <param name="maskSize">The mask size of rendering layers texture</param>
        public static void SetupProperties(CommandBuffer cmd, MaskSize maskSize) { SetupProperties(CommandBufferHelpers.GetRasterCommandBuffer(cmd), maskSize); }
        internal static void SetupProperties(RasterCommandBuffer cmd, MaskSize maskSize)
        {
            int bits = GetBits(maskSize);

            // Pre-computes properties used for packing/unpacking
            uint maxInt = bits != 32 ? (1u << bits) - 1u : uint.MaxValue;
            float rcpMaxInt = Unity.Mathematics.math.rcp(maxInt);
            cmd.SetGlobalInt(ShaderPropertyId.renderingLayerMaxInt, (int)maxInt);
            cmd.SetGlobalFloat(ShaderPropertyId.renderingLayerRcpMaxInt, rcpMaxInt);
        }

        /// <summary>
        /// Converts rendering layers texture mask size to graphics format.
        /// </summary>
        public static GraphicsFormat GetFormat(MaskSize maskSize)
        {
            switch (maskSize)
            {
                case MaskSize.Bits8:
                    return GraphicsFormat.R8_UNorm;
                case MaskSize.Bits16:
                {
                        //webgpu does not support r16_unorm as a render target format
#if UNITY_2023_2_OR_NEWER
                        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.WebGPU)
                        {
                            return GraphicsFormat.R32_SFloat;
                        }
#endif
                        return GraphicsFormat.R16_UNorm;
                }
                case MaskSize.Bits24:
                case MaskSize.Bits32:
                    return GraphicsFormat.R32_SFloat;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Masks rendering layers with those that available in urp global settings.
        /// </summary>
        public static uint ToValidRenderingLayers(uint renderingLayers)
        {
            if (UniversalRenderPipelineGlobalSettings.instance)
            {
                uint validRenderingLayers = RenderingLayerMask.GetDefinedRenderingLayersCombinedMaskValue();
                return validRenderingLayers & renderingLayers;
            }
            return renderingLayers;
        }

        static MaskSize GetMaskSize(int bits)
        {
            int bytes = (bits + 7) / 8;
            switch (bytes)
            {
                case 0:
                    return MaskSize.Bits8;
                case 1:
                    return MaskSize.Bits8;
                case 2:
                    return MaskSize.Bits16;
                case 3:
                    return MaskSize.Bits24;
                case 4:
                    return MaskSize.Bits32;
                default:
                    return MaskSize.Bits32;
            }
        }

        static int GetBits(MaskSize maskSize)
        {
            switch (maskSize)
            {
                case MaskSize.Bits8:
                    return 8;
                case MaskSize.Bits16:
                    return 16;
                case MaskSize.Bits24:
                    return 24;
                case MaskSize.Bits32:
                    return 32;
                default:
                    throw new NotImplementedException();
            }
        }

        static Event Combine(Event a, Event b)
        {
            return (Event)Mathf.Min((int)a, (int)b);
        }

        static MaskSize Combine(MaskSize a, MaskSize b)
        {
            return (MaskSize)Mathf.Max((int)a, (int)b);
        }
    }
}
