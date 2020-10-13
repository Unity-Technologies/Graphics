using System.Collections.Generic;
using System.Net;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal struct LayerBatch
    {
        public int startLayerID;
        public int startLayerValue;
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

        private static void InitializeBatchInfos(SortingLayer[] cachedSortingLayers)
        {
            var count = cachedSortingLayers.Length;
            var needInit = s_LayerBatches == null;
            s_LayerBatches ??= new LayerBatch[count];

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

                layerBatch.startLayerID = layerToRender;
                layerBatch.startLayerValue = startLayerValue;
				layerBatch.endLayerValue = endLayerValue;
                layerBatch.layerRange = sortingLayerRange;
                layerBatch.lightStats = lightStats;

                i = upperLayerInBatch + 1;
            }

            return s_LayerBatches;
        }
    }

//     internal static class LightTextureManager
//     {
//         private struct TextureInfo
//         {
//             public int key;
//             public bool allocated;
//         }
//
//         private static List<int> s_AllNameIDs;
//         private static List<TextureInfo> s_AllTextureInfo;
//
//         private static RenderTextureDescriptor s_Desc;
//         private static long s_MemoryBudget;
//         private static int s_ActiveTextureCount;
//
//         public static void Initialize(ref RenderTextureDescriptor desc, long memoryBudget)
//         {
//             var maxTextureCount = Light2DManager.GetCachedSortingLayer().Length * 4;
//             if (s_AllNameIDs == null || s_AllNameIDs.Capacity != maxTextureCount)
//             {
//                 s_AllNameIDs = new List<int>(maxTextureCount);
//                 for (int i = 0; i < s_AllNameIDs.Capacity; ++i)
//                     s_AllNameIDs.Add(Shader.PropertyToID("_LightTexture" + i));
//
//                 s_AllTextureInfo = new List<TextureInfo>(maxTextureCount);
//             }
//
//             if (s_MemoryBudget == memoryBudget && s_Desc.width == desc.width && s_Desc.height == desc.height)
//                 return;
//
//             s_Desc = desc;
//             s_MemoryBudget = memoryBudget;
//
//             var dummyTexture = RenderTexture.GetTemporary(desc);
//             dummyTexture.Create();
//             var textureCount = (int)(memoryBudget / Profiler.GetRuntimeMemorySizeLong(dummyTexture));
//             RenderTexture.ReleaseTemporary(dummyTexture);
//             textureCount = Mathf.Clamp(textureCount, 4, maxTextureCount);
//
//             s_AllTextureInfo.Clear();
//             for (var i = 0; i < textureCount; ++i)
//                 s_AllTextureInfo.Add(new TextureInfo());
//         }
//
//         public static bool HasBudgetFor(int textureCount)
//         {
//             return s_AllTextureInfo.Count - s_ActiveTextureCount >= textureCount;
//         }
//
//         public static void ResetKeys()
//         {
//             for (var i = 0; i < s_AllTextureInfo.Count; ++i)
//             {
//                 var textureInfo = s_AllTextureInfo[i];
//                 textureInfo.key = -1;
//                 s_AllTextureInfo[i] = textureInfo;
//             }
//
//             s_ActiveTextureCount = 0;
//         }
//
//         public static RenderTargetIdentifier GetLightTextureIdentifier(CommandBuffer cmd, int key)
//         {
//             for (var i = 0; i < s_AllTextureInfo.Count; ++i)
//             {
//                 var textureInfo = s_AllTextureInfo[i];
//
//                 if (textureInfo.key == -1 || textureInfo.key == key)
//                 {
//                     if (textureInfo.key == -1)
//                     {
//                         textureInfo.key = key;
//                         s_ActiveTextureCount++;
//                     }
//
//                     var nameID = s_AllNameIDs[i];
//
//                     if (!textureInfo.allocated)
//                     {
//                         cmd.GetTemporaryRT(nameID, s_Desc, FilterMode.Bilinear);
//                         textureInfo.allocated = true;
//                     }
//
//                     s_AllTextureInfo[i] = textureInfo;
//
//                     return nameID;
//                 }
//             }
//
//             return BuiltinRenderTextureType.None;
//         }
//
//         public static void ReleaseLightTextures(CommandBuffer cmd)
//         {
//             for (var i = 0; i < s_AllTextureInfo.Count; ++i)
//             {
//                 var textureInfo = s_AllTextureInfo[i];
//
//                 if (!textureInfo.allocated)
//                     continue;
//
//                 cmd.ReleaseTemporaryRT(s_AllNameIDs[i]);
//                 textureInfo.allocated = false;
//
//                 s_AllTextureInfo[i] = textureInfo;
//             }
//         }
//     }
//
//     internal static class TemporaryRTManager
//     {
//         private static int[] textureIDs;
//         private static int top = 0;
//
//         public static void Begin()
//         {
//             var layers = Light2DManager.GetCachedSortingLayer();
//             var initIDs = textureIDs == null;
//             textureIDs ??= new int[layers.Length * 4];
//
// #if UNITY_EDITOR
//             if(textureIDs.Length != layers.Length * 4)
//             {
//                 textureIDs = new int[layers.Length * 4];
//                 initIDs = true;
//             }
// #endif
//             if (initIDs)
//             {
//                 for (var i = 0; i < textureIDs.Length; i++)
//                 {
//                     textureIDs[top] = Shader.PropertyToID($"_LightTexture_{i}");
//                 }
//             }
//
//             top = 0;
//         }
//
//         public static int GetTempID()
//         {
//             return textureIDs[top++];
//         }
//
//         public static void End(CommandBuffer cmd)
//         {
//             for (var i = 0; i < top; i++)
//             {
//                 cmd.ReleaseTemporaryRT(textureIDs[i]);
//             }
//         }
//
//     }
}
