using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Unity.Rendering
{
    [Serializable]
    public struct LightMaps : IEquatable<LightMaps>
    {
        public Texture2DArray colors;
        public Texture2DArray directions;
        public Texture2DArray shadowMasks;

        public bool hasDirections => directions != null && directions.depth > 0;
        public bool hasShadowMask => shadowMasks != null && shadowMasks.depth > 0;

        public bool isValid => colors != null;

        public bool Equals(LightMaps other)
        {
            return
                colors == other.colors &&
                directions == other.directions &&
                shadowMasks == other.shadowMasks;
        }

        /// <summary>
        /// A representative hash code.
        /// </summary>
        /// <returns>A number that is guaranteed to be the same when generated from two objects that are the same.</returns>
        public override int GetHashCode()
        {
            int hash = 0;
            if (!ReferenceEquals(colors, null)) hash ^= colors.GetHashCode();
            if (!ReferenceEquals(directions, null)) hash ^= directions.GetHashCode();
            if (!ReferenceEquals(shadowMasks, null)) hash ^= shadowMasks.GetHashCode();
            return hash;
        }

        private static Texture2DArray CopyToTextureArray(List<Texture2D> source)
        {
            if (source == null || !source.Any())
                return null;

            var data = source.First();
            if (data == null)
                return null;

            bool isSRGB = GraphicsFormatUtility.IsSRGBFormat(data.graphicsFormat);
            var result = new Texture2DArray(data.width, data.height, source.Count, source[0].format, true, !isSRGB);
            result.filterMode = FilterMode.Trilinear;
            result.wrapMode = TextureWrapMode.Clamp;
            result.anisoLevel = 3;

            for (var sliceIndex = 0; sliceIndex < source.Count; sliceIndex++)
            {
                var lightMap = source[sliceIndex];
                Graphics.CopyTexture(lightMap, 0, result, sliceIndex);
            }

            return result;
        }

        public static LightMaps ConstructLightMaps(List<Texture2D> inColors, List<Texture2D> inDirections, List<Texture2D> inShadowMasks)
        {
            var result = new LightMaps
            {
                colors = CopyToTextureArray(inColors),
                directions = CopyToTextureArray(inDirections),
                shadowMasks = CopyToTextureArray(inShadowMasks)
            };
            return result;
        }
        
        [Flags]
        enum LightMappingFlags
        {
            None = 0,
            Lightmapped = 1,
            Directional = 2,
            ShadowMask = 4
        }
        
        struct MaterialLookupKey
        {
            public Material BaseMaterial;
            public LightMaps LightMaps;
            public LightMappingFlags Flags;
        }
        
        public static LightMaps GetLightmapsStruct()
        {
            var lightmapsbaked = LightmapSettings.lightmaps;

            var colors = new List<Texture2D>();
            var directions = new List<Texture2D>();
            var shadowMasks = new List<Texture2D>();

            for (var i = 0; i < lightmapsbaked.Length; i++)
            {
                var lightmapData = lightmapsbaked[i];
                colors.Add(lightmapData.lightmapColor);
                directions.Add(lightmapData.lightmapDir);
                shadowMasks.Add(lightmapData.shadowMask);
            }
            return ConstructLightMaps(colors, directions, shadowMasks);
        }
        
        private static Material GetLightMappedMaterial(Material baseMaterial, LightMaps lightMaps, Dictionary<MaterialLookupKey, Material> lightMappedMaterialCache)
        {
            var flags = LightMappingFlags.Lightmapped;
            if (lightMaps.hasDirections)
                flags |= LightMappingFlags.Directional;
            if (lightMaps.hasShadowMask)
                flags |= LightMappingFlags.ShadowMask;

            var key = new MaterialLookupKey
            {
                BaseMaterial = baseMaterial,
                LightMaps = lightMaps,
                Flags = flags
            };

            if (lightMappedMaterialCache.TryGetValue(key, out var lightMappedMaterial))
            {
                return lightMappedMaterial;
            }
            else
            {
                lightMappedMaterial = CreateLightMappedMaterial(baseMaterial, lightMaps);
                lightMappedMaterialCache[key] = lightMappedMaterial;
                return lightMappedMaterial;
            }
        }
        
        private static Material CreateLightMappedMaterial(Material material, LightMaps lightMaps)
        {
            var lightMappedMaterial = new Material(material);
            lightMappedMaterial.name = $"{lightMappedMaterial.name}_Lightmapped_";
            lightMappedMaterial.EnableKeyword("LIGHTMAP_ON");

            lightMappedMaterial.SetTexture("unity_Lightmaps", lightMaps.colors);
            lightMappedMaterial.SetTexture("unity_LightmapsInd", lightMaps.directions);
            lightMappedMaterial.SetTexture("unity_ShadowMasks", lightMaps.shadowMasks);

            if (lightMaps.hasDirections)
            {
                lightMappedMaterial.name = lightMappedMaterial.name + "_DIRLIGHTMAP";
                lightMappedMaterial.EnableKeyword("DIRLIGHTMAP_COMBINED");
            }

            if (lightMaps.hasShadowMask)
            {
                lightMappedMaterial.name = lightMappedMaterial.name + "_SHADOW_MASK";
                lightMappedMaterial.EnableKeyword("SHADOWS_SHADOWMASK");
            }

            return lightMappedMaterial;
        }
        
        // build a map of renderer / submesh -> Material.
        // create a new material if lightmappingis needed
        public static Dictionary<Tuple<Renderer, int>, Material> GenerateRenderersToMaterials(List<Renderer> renderers, LightMaps maps)
        {
            var returnMap = new Dictionary<Tuple<Renderer, int>, Material>();
            
            Dictionary<MaterialLookupKey, Material> lightMappedMaterialCache = new();

            // renderer
            foreach (var renderer in renderers)
            { 
                if(renderer == null)
                    continue;
                
                var lightmapIndex = renderer.lightmapIndex;
                var sharedMaterials = new List<Material>();
                renderer.GetSharedMaterials(sharedMaterials);
                
                // submesh
                for (var i = 0; i < sharedMaterials.Count; i++)
                {
                    Material material = null;
                    // if not valid for lightmapping - just use the current material
                    if (!maps.isValid
                        || renderer.materials[i] == null
                        || lightmapIndex < 0
                        || lightmapIndex == 65534
                        || lightmapIndex > maps.colors.depth)
                        material = sharedMaterials[i];
                    else
                    {
                        material = GetLightMappedMaterial(sharedMaterials[i], maps, lightMappedMaterialCache);
                    }

                    returnMap.Add(new Tuple<Renderer, int>(renderer, i), material);
                }
            }
            return returnMap;
        }
    }
}
