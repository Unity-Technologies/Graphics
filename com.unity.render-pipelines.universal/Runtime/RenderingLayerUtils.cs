using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
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
            var requiresRenderingLayers = false;

            bool isDeferred = universalRendererData.renderingMode == RenderingMode.Deferred;

            foreach (var rendererFeature in universalRendererData.rendererFeatures)
            {
                var decalRendereFeature = rendererFeature as DecalRendererFeature;
                if (decalRendereFeature != null && decalRendereFeature.isActive)
                {
                    var technique = decalRendereFeature.GetTechnique(universalRendererData);
                    if (technique == DecalTechnique.DBuffer)
                        e = CombineEvents(e, Event.DepthNormalPrePass);
                    else
                        e = CombineEvents(e, isDeferred ? Event.GBuffer : Event.ForwardOpaque);

                    requiresRenderingLayers |= decalRendereFeature.requiresDecalLayers;
                }
            }

            return requiresRenderingLayers ? e : Event.None;
        }

        public static Event GetEvent(UniversalRenderer universalRenderer, List<ScriptableRendererFeature> rendererFeatures)
        {
            var e = Event.None;
            var requiresRenderingLayers = false;

            bool isDeferred = universalRenderer.renderingMode == RenderingMode.Deferred;

            foreach (var rendererFeature in rendererFeatures)
            {
                var decalRendereFeature = rendererFeature as DecalRendererFeature;
                if (decalRendereFeature != null && decalRendereFeature.isActive)
                {
                    var technique = decalRendereFeature.GetTechnique(universalRenderer);
                    if (technique == DecalTechnique.DBuffer)
                        e = CombineEvents(e, Event.DepthNormalPrePass);
                    else
                        e = CombineEvents(e, isDeferred ? Event.GBuffer : Event.ForwardOpaque);

                    requiresRenderingLayers |= decalRendereFeature.requiresDecalLayers;
                }
            }

            return requiresRenderingLayers ? e : Event.None;
        }

        static Event CombineEvents(Event a, Event b)
        {
            return (Event)Mathf.Min((int)a, (int)b);
        }
    }
}
