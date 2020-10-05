using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal struct LayerBatch
    {
        public int firstLayerToRender;
        public int endLayerValue;
        public SortingLayerRange layerRange;
        public LightStats lightStats;
        public unsafe fixed int renderTargetIds[4];

        public void Init(int index)
        {
            for (var i = 0; i < 4; i++)
            {
                unsafe
                {
                    renderTargetIds[i] = Shader.PropertyToID($"_LightTexture_{index}_{i}");
                }
            }
        }
    }

    internal static class LayerUtility
    {
        private static List<LayerBatch> s_LayerBatches;

        private static bool CompareLightsInLayer(int layerIndex1, int layerIndex2, SortingLayer[] sortingLayers, ILight2DCullResult lightCullResult)
        {
            var layerId1 = sortingLayers[layerIndex1].id;
            var layerId2 = sortingLayers[layerIndex2].id;
            foreach (var light in lightCullResult.visibleLights)
            {
                if (light.IsLitLayer(layerId1) != light.IsLitLayer(layerId2))
                    return false;
            }
            return true;
        }

        private static int FindUpperBoundInBatch(int startLayerIndex, SortingLayer[] sortingLayers, ILight2DCullResult lightCullResult)
        {
            // start checking at the next layer
            for (var i = startLayerIndex+1; i < sortingLayers.Length; i++)
            {
                if(!CompareLightsInLayer(startLayerIndex, i, sortingLayers, lightCullResult))
                    return i-1;
            }
            return sortingLayers.Length-1;
        }

        public static List<LayerBatch> CalculateBatches(ILight2DCullResult lightCullResult)
        {
            var cachedSortingLayers = Light2DManager.GetCachedSortingLayer();
            var count = cachedSortingLayers.Length;
            s_LayerBatches ??= new List<LayerBatch>(count);

#if UNITY_EDITOR
            // we should fix. Make a non allocating version of this
            if (!Application.isPlaying && s_LayerBatches.Capacity != count)
                s_LayerBatches = new List<LayerBatch>(count);
#endif

            s_LayerBatches.Clear();

            for (var i = 0; i < cachedSortingLayers.Length;)
            {
                var layerToRender = cachedSortingLayers[i].id;
                var lightStats = lightCullResult.GetLightStatsByLayer(layerToRender);
                var layerBatch = new LayerBatch();
                layerBatch.Init(i);

                // find the highest layer that share the same set of lights as this layer
                var upperLayerInBatch = FindUpperBoundInBatch(i, cachedSortingLayers, lightCullResult);
                // Some renderers override their sorting layer value with short.MinValue or short.MaxValue.
                // When drawing the first sorting layer, we should include the range from short.MinValue to layerValue.
                // Similarly, when drawing the last sorting layer, include the range from layerValue to short.MaxValue.
                var startLayerValue = (short) cachedSortingLayers[i].value;
                var lowerBound = (i == 0) ? short.MinValue : startLayerValue;
                var endLayerValue = (short) cachedSortingLayers[upperLayerInBatch].value;
                var upperBound = (upperLayerInBatch == cachedSortingLayers.Length - 1) ? short.MaxValue : endLayerValue;
                // renderer within this range share the same set of lights so they should be rendered together
                var sortingLayerRange = new SortingLayerRange(lowerBound, upperBound);

                layerBatch.endLayerValue = endLayerValue;
                layerBatch.firstLayerToRender = layerToRender;
                layerBatch.layerRange = sortingLayerRange;
                layerBatch.lightStats = lightStats;

                s_LayerBatches.Add(layerBatch);

                i = upperLayerInBatch + 1;
            }

            return s_LayerBatches;
        }
    }
}
