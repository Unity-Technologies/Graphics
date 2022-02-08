using System.Collections.Generic;

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
                    e = CombineEvents(e, rendererFeature.RequireRenderingLayers(isDeferred));
            }

            return e;
        }

        public static Event GetEvent(UniversalRenderer universalRenderer, List<ScriptableRendererFeature> rendererFeatures)
        {
            var e = Event.None;
            bool isDeferred = universalRenderer.renderingMode == RenderingMode.Deferred;

            foreach (var rendererFeature in rendererFeatures)
            {
                if (rendererFeature.isActive)
                    e = CombineEvents(e, rendererFeature.RequireRenderingLayers(isDeferred));
            }

            return e;
        }

        static Event CombineEvents(Event a, Event b)
        {
            return (Event)Mathf.Min((int)a, (int)b);
        }
    }
}
