using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>Rendering Layers.</summary>
    [System.Flags]
    public enum RenderingLayerEnum
    {
        /// <summary>The rendering will no affect any object.</summary>
        Nothing = 0,   // Custom name for "Nothing" option
        /// <summary>Rendering Layer 0.</summary>
        RenderingLayerDefault = 1 << 0,
        /// <summary>Rendering Layer 1.</summary>
        RenderingLayer1 = 1 << 1,
        /// <summary>Rendering Layer 2.</summary>
        RenderingLayer2 = 1 << 2,
        /// <summary>Rendering Layer 3.</summary>
        RenderingLayer3 = 1 << 3,
        /// <summary>Rendering Layer 4.</summary>
        RenderingLayer4 = 1 << 4,
        /// <summary>Rendering Layer 5.</summary>
        RenderingLayer5 = 1 << 5,
        /// <summary>Rendering Layer 6.</summary>
        RenderingLayer6 = 1 << 6,
        /// <summary>Rendering Layer 7.</summary>
        RenderingLayer7 = 1 << 7,
        /// <summary>Everything.</summary>
        Everything = 0xFF, // Custom name for "Everything" option
    }

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
                    e = CombineEvents(e, rendererFeature.RequireRenderingLayers(isDeferred));
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
                    e = CombineEvents(e, rendererFeature.RequireRenderingLayers(isDeferred));
            }

            return e;
        }

        public static uint ToRenderingLayers(RenderingLayerEnum renderingLayers)
        {
            return (uint)renderingLayers;
        }

        public static uint ToRenderingLayers(LightLayerEnum lightLayers)
        {
            return (uint)lightLayers;
        }

        public static uint ToRenderingLayers(DecalLayerEnum decalLayers)
        {
            return (uint)decalLayers << 8;
        }

        static Event CombineEvents(Event a, Event b)
        {
            return (Event)Mathf.Min((int)a, (int)b);
        }
    }
}
