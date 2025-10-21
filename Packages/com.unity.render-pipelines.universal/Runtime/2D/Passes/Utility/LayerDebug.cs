#if UNITY_EDITOR
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace UnityEngine.Rendering.Universal
{
    static class LayerDebug
    {
#if UNITY_EDITOR
        static internal bool enabled => FrameDebugger.enabled || RenderGraph.isRenderGraphViewerActive;

        static internal string GetDebugLayerNames(LayerBatch layerBatch)
        {
            var debugNames = string.Empty;
            var sortingLayers = Light2DManager.GetCachedSortingLayer();

            foreach (var layer in sortingLayers)
            {
                if (layerBatch.IsValueWithinLayerRange(layer.value))
                {
                    if (debugNames == string.Empty)
                        debugNames = layer.name;
                    else
                        debugNames += " | " + layer.name;
                }
            }

            return debugNames;
        }
#endif

        static internal void FormatPassName(LayerBatch layerBatch, ref string passName)
        {
#if UNITY_EDITOR
            if (enabled)
                passName += " - " + GetDebugLayerNames(layerBatch);
#endif
        }

        static internal ProfilingSampler GetProfilingSampler(string passName, ProfilingSampler sampler)
        {
#if UNITY_EDITOR
            if (enabled)
                return new ProfilingSampler(passName);
#endif
            return sampler;
        }
    }
}

