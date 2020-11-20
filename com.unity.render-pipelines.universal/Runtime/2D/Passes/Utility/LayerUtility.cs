using Unity.Mathematics;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal struct LayerBatch
    {
        public int startLayerID;
        public int endLayerValue;
        public SortingLayerRange layerRange;
        public LightStats lightStats;
        private unsafe fixed int renderTargetIds[4];
        private unsafe fixed bool renderTargetUsed[4];

        public void InitRTIds(int index)
        {
            for (var i = 0; i < 4; i++)
            {
                unsafe
                {
                    renderTargetUsed[i] = false;
                    renderTargetIds[i] = Shader.PropertyToID($"_LightTexture_{index}_{i}");
                }
            }
        }

        public RenderTargetIdentifier GetRTId(CommandBuffer cmd, RenderTextureDescriptor desc, int index)
        {
            unsafe
            {
                if (!renderTargetUsed[index])
                {
                    cmd.GetTemporaryRT(renderTargetIds[index], desc, FilterMode.Bilinear);
                    renderTargetUsed[index] = true;
                }
                return new RenderTargetIdentifier(renderTargetIds[index]);
            }
        }

        public void ReleaseRT(CommandBuffer cmd)
        {
            for (var i = 0; i < 4; i++)
            {
                unsafe
                {
                    if (!renderTargetUsed[i])
                        continue;

                    cmd.ReleaseTemporaryRT(renderTargetIds[i]);
                    renderTargetUsed[i] = false;
                }
            }
        }
    }

    internal static class LayerUtility
    {
        private static LayerBatch[] s_LayerBatches;
        public static uint maxTextureCount { get; private set; }

        public static void InitializeBudget(uint maxTextureCount)
        {
            LayerUtility.maxTextureCount = math.max(4, maxTextureCount);
        }

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
            for (var i = startLayerIndex + 1; i < sortingLayers.Length; i++)
            {
                if (!CompareLightsInLayer(startLayerIndex, i, sortingLayers, lightCullResult))
                    return i - 1;
            }
            return sortingLayers.Length - 1;
        }

        private static void InitializeBatchInfos(SortingLayer[] cachedSortingLayers)
        {
            var count = cachedSortingLayers.Length;
            var needInit = s_LayerBatches == null;
            if (s_LayerBatches is null)
            {
                s_LayerBatches = new LayerBatch[count];
            }

#if UNITY_EDITOR
            // we should fix. Make a non allocating version of this
            if (!Application.isPlaying && s_LayerBatches.Length != count)
            {
                s_LayerBatches = new LayerBatch[count];
                needInit = true;
            }
#endif
            if (needInit)
            {
                for (var i = 0; i < s_LayerBatches.Length; i++)
                {
                    ref var layerBatch = ref s_LayerBatches[i];
                    layerBatch.InitRTIds(i);
                }
            }
        }

        public static LayerBatch[] CalculateBatches(ILight2DCullResult lightCullResult, out int batchCount)
        {
            var cachedSortingLayers = Light2DManager.GetCachedSortingLayer();
            InitializeBatchInfos(cachedSortingLayers);

            batchCount = 0;
            for (var i = 0; i < cachedSortingLayers.Length;)
            {
                var layerToRender = cachedSortingLayers[i].id;
                var lightStats = lightCullResult.GetLightStatsByLayer(layerToRender);
                ref var layerBatch = ref s_LayerBatches[batchCount++];

                // Find the highest layer that share the same set of lights as this layer.
                var upperLayerInBatch = FindUpperBoundInBatch(i, cachedSortingLayers, lightCullResult);

                // Some renderers override their sorting layer value with short.MinValue or short.MaxValue.
                // When drawing the first sorting layer, we should include the range from short.MinValue to layerValue.
                // Similarly, when drawing the last sorting layer, include the range from layerValue to short.MaxValue.
                var startLayerValue = (short)cachedSortingLayers[i].value;
                var lowerBound = (i == 0) ? short.MinValue : startLayerValue;
                var endLayerValue = (short)cachedSortingLayers[upperLayerInBatch].value;
                var upperBound = (upperLayerInBatch == cachedSortingLayers.Length - 1) ? short.MaxValue : endLayerValue;

                // Renderer within this range share the same set of lights so they should be rendered together.
                var sortingLayerRange = new SortingLayerRange(lowerBound, upperBound);

                layerBatch.startLayerID = layerToRender;
                layerBatch.endLayerValue = endLayerValue;
                layerBatch.layerRange = sortingLayerRange;
                layerBatch.lightStats = lightStats;

                i = upperLayerInBatch + 1;
            }

            return s_LayerBatches;
        }
    }
}
