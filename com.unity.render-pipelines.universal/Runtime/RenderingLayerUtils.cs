using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Helper class for finding out if Rendering Layers Texture is required by Scriptable Renderer Features.
    /// </summary>
    internal static class RenderingLayerUtils
    {
        public enum Event
        {
            DepthNormalPrePass,
            ForwardOpaque,
            GBuffer,
            None,
        }

        public static Event GetEvent(UniversalRendererData universalRendererData)
        {
            var e = Event.None;
            bool isDeferred = universalRendererData.renderingMode == RenderingMode.Deferred;

            foreach (var rendererFeature in universalRendererData.rendererFeatures)
            {
                if (rendererFeature.isActive)
                    e = Combine(e, rendererFeature.RequireRenderingLayers(isDeferred));
            }

            return e;
        }

        public static Event GetEvent(UniversalRenderer universalRenderer, List<ScriptableRendererFeature> rendererFeatures)
        {
            var e = Event.None;
            bool isDeferred = universalRenderer.renderingModeActual == RenderingMode.Deferred;

            foreach (var rendererFeature in rendererFeatures)
            {
                if (rendererFeature.isActive)
                    e = Combine(e, rendererFeature.RequireRenderingLayers(isDeferred));
            }

            return e;
        }

        public static MaskSize GetMaskSize(UniversalRenderer universalRenderer, List<ScriptableRendererFeature> rendererFeatures)
        {
            var e = MaskSize.Bits8;
            bool isDeferred = universalRenderer.renderingModeActual == RenderingMode.Deferred;

            foreach (var rendererFeature in rendererFeatures)
            {
                if (rendererFeature.isActive)
                    e = Combine(e, rendererFeature.RequireRenderingLayerMaskSize(isDeferred));
            }

            return e;
        }

        public enum MaskSize
        {
            Bits8,
            Bits16,
            Bits24,
            Bits32,
        }

        public static int GetBits(GraphicsFormat format)
        {
            switch (format)
            {
                case GraphicsFormat.R8_UNorm:
                    return 8;
                case GraphicsFormat.R16_UNorm:
                    return 16;
                case GraphicsFormat.R32_SFloat:
                    return 31;
                default:
                    throw new NotImplementedException();
            }
        }

        public static int GetBits(MaskSize maskSize)
        {
            return GetBits(GetFormat(maskSize));
        }

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

        public static uint ToRenderingLayers(LightLayerEnum lightLayers)
        {
            return (uint)lightLayers;
        }

        public static uint ToRenderingLayers(DecalLayerEnum decalLayers)
        {
            return (uint)decalLayers << 8;
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
