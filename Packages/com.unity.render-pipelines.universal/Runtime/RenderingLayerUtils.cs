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

        /// <summary>
        /// Returns True if <see cref="UniversalRendererData"/> will require rendering layers texture.
        /// </summary>
        /// <param name="universalRendererData"></param>
        /// <param name="combinedEvent">Event at which rendering layers texture needs to be created</param>
        /// <param name="combinedMaskSize">The mask size of rendering layers texture</param>
        public static bool RequireRenderingLayers(UniversalRendererData universalRendererData, int msaaSampleCount,
            out Event combinedEvent, out MaskSize combinedMaskSize)
        {
            combinedEvent = Event.DepthNormalPrePass;
            combinedMaskSize = MaskSize.Bits8;

            bool isDeferred = universalRendererData.renderingMode == RenderingMode.Deferred;
            bool result = false;
            foreach (var rendererFeature in universalRendererData.rendererFeatures)
            {
                if (rendererFeature.isActive)
                {
                    result |= rendererFeature.RequireRenderingLayers(isDeferred, out Event rendererEvent, out MaskSize rendererMaskSize);
                    combinedEvent = Combine(combinedEvent, rendererEvent);
                    combinedMaskSize = Combine(combinedMaskSize, rendererMaskSize);
                }
            }

            // Rendering layers can not use MSSA resolve, because it encodes integer
            if (msaaSampleCount > 1 && combinedEvent == Event.Opaque)
                combinedEvent = Event.DepthNormalPrePass;

            return result;
        }

        /// <summary>
        /// Returns True if <see cref="UniversalRenderer"/> will require rendering layers texture.
        /// </summary>
        /// <param name="UniversalRenderer"></param>
        /// <param name="combinedEvent">Event at which rendering layers texture needs to be created</param>
        /// <param name="combinedMaskSize">The mask size of rendering layers texture</param>
        public static bool RequireRenderingLayers(UniversalRenderer universalRenderer, List<ScriptableRendererFeature> rendererFeatures, int msaaSampleCount,
            out Event combinedEvent, out MaskSize combinedMaskSize)
        {
            combinedEvent = Event.Opaque;
            combinedMaskSize = MaskSize.Bits8;

            bool isDeferred = universalRenderer.renderingModeActual == RenderingMode.Deferred;
            bool result = false;
            foreach (var rendererFeature in rendererFeatures)
            {
                if (rendererFeature.isActive)
                {
                    result |= rendererFeature.RequireRenderingLayers(isDeferred, out Event rendererEvent, out MaskSize rendererMaskSize);
                    combinedEvent = Combine(combinedEvent, rendererEvent);
                    combinedMaskSize = Combine(combinedMaskSize, rendererMaskSize);
                }
            }

            // Rendering layers can not use MSSA resolve, because it encodes integer
            if (msaaSampleCount > 1 && combinedEvent == Event.Opaque)
                combinedEvent = Event.DepthNormalPrePass;

            return result;
        }

        /// <summary>
        /// Setups properties that are needed for accessing rendering layers texture.
        /// </summary>
        /// <param name="cmd">Used command buffer</param>
        /// <param name="maskSize">The mask size of rendering layers texture</param>
        public static void SetupProperties(CommandBuffer cmd, MaskSize maskSize)
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
                    return GraphicsFormat.R16_UNorm;
                case MaskSize.Bits24:
                case MaskSize.Bits32:
                    return GraphicsFormat.R32_SFloat;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Converts light layers to rendering layers.
        /// </summary>
        public static uint ToRenderingLayers(LightLayerEnum lightLayers)
        {
            return (uint)lightLayers;
        }

        /// <summary>
        /// Converts decal layers to rendering layers.
        /// </summary>
        public static uint ToRenderingLayers(DecalLayerEnum decalLayers)
        {
            return (uint)decalLayers << 8;
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
