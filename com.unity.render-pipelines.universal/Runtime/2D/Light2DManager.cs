using System.Collections.Generic;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor.Experimental.SceneManagement;
#endif

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal struct LayerBatch
    {
        public bool enabled;
        public int layerToRender;
        public SortingLayerRange layerRange;
        public LightStats lightStats;
        public unsafe fixed bool renderTargetUsed[4];
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

    internal static class Light2DManager
    {
        private static SortingLayer[] s_SortingLayers;
        private static LayerBatch[] s_LayerBatches;

        public static List<Light2D> lights { get; } = new List<Light2D>();

        // Called during OnEnable
        public static void RegisterLight(Light2D light)
        {
            Debug.Assert(!lights.Contains(light));
            lights.Add(light);
            ErrorIfDuplicateGlobalLight(light);
        }

        // Called during OnEnable
        public static void DeregisterLight(Light2D light)
        {
            Debug.Assert(lights.Contains(light));
            lights.Remove(light);
        }

        public static void ErrorIfDuplicateGlobalLight(Light2D light)
        {
            if (light.lightType != Light2D.LightType.Global)
                return;

            foreach (var sortingLayer in light.affectedSortingLayers)
            {
                // should this really trigger at runtime?
                if(ContainsDuplicateGlobalLight(sortingLayer, light.blendStyleIndex))
                    Debug.LogError("More than one global light on layer " + SortingLayer.IDToName(sortingLayer) + " for light blend style index " + light.blendStyleIndex);
            }
        }

        public static bool GetGlobalColor(int sortingLayerIndex, int blendStyleIndex, out Color color)
        {
            var  foundGlobalColor = false;
            color = Color.black;

            // This should be rewritten to search only global lights
            foreach(var light in lights)
            {
                if (light.lightType != Light2D.LightType.Global ||
                    light.blendStyleIndex != blendStyleIndex ||
                    !light.IsLitLayer(sortingLayerIndex))
                    continue;

                var inCurrentPrefabStage = true;
#if UNITY_EDITOR
                // If we found the first global light in our prefab stage
                inCurrentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage()?.IsPartOfPrefabContents(light.gameObject) ?? true;
#endif

                if (inCurrentPrefabStage)
                {
                    color = light.color * light.intensity;
                    return true;
                }
                else
                {
                    if (!foundGlobalColor)
                    {
                        color = light.color * light.intensity;
                        foundGlobalColor = true;
                    }
                }
            }

            return foundGlobalColor;
        }

        private static bool ContainsDuplicateGlobalLight(int sortingLayerIndex, int blendStyleIndex)
        {
            var globalLightCount = 0;

            // This should be rewritten to search only global lights
            foreach(var light in lights)
            {
                if (light.lightType == Light2D.LightType.Global &&
                    light.blendStyleIndex == blendStyleIndex &&
                    light.IsLitLayer(sortingLayerIndex))
                {
#if UNITY_EDITOR
                    // If we found the first global light in our prefab stage
                    if (PrefabStageUtility.GetPrefabStage(light.gameObject) == PrefabStageUtility.GetCurrentPrefabStage())
#endif
                    {
                        if (globalLightCount > 0)
                            return true;

                        globalLightCount++;
                    }
                }
            }

            return false;
        }



        public static SortingLayer[] GetCachedSortingLayer()
        {
            s_SortingLayers ??= SortingLayer.layers;
#if UNITY_EDITOR
            // we should fix. Make a non allocating version of this
            if(!Application.isPlaying)
                s_SortingLayers = SortingLayer.layers;
#endif
            return s_SortingLayers;
        }

        public static LayerBatch[] GetCachedLayerBatches()
        {
            var count = GetCachedSortingLayer().Length;
            var needInit = s_LayerBatches == null;
            s_LayerBatches ??= new LayerBatch[count];

#if UNITY_EDITOR
            // we should fix. Make a non allocating version of this
            if(!Application.isPlaying)
            {
                s_LayerBatches = new LayerBatch[count];
                needInit = true;
            }
#endif

            if (needInit)
            {
                for(var i = 0; i < s_LayerBatches.Length; i++)
                    s_LayerBatches[i].Init(i);
            }

            return s_LayerBatches;
        }

    }
}
