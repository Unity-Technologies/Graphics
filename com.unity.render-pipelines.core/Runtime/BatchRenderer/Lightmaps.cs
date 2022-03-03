using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
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

            bool isSrgb = GraphicsFormatUtility.IsSRGBFormat(data.graphicsFormat);
            var result = new Texture2DArray(data.width, data.height, source.Count, source[0].format, true, !isSrgb)
            {
                filterMode = data.filterMode,
                wrapMode = data.wrapMode,
                anisoLevel = data.anisoLevel,
                mipMapBias = data.mipMapBias
            };

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

        private static Tuple<LightMaps, Dictionary<int, int>> GetLightmapsStruct(List<int> indexesToConvert)
        {
            var lightmapsbaked = LightmapSettings.lightmaps;

            var colors = new List<Texture2D>();
            var directions = new List<Texture2D>();
            var shadowMasks = new List<Texture2D>();

            var remapTable = new Dictionary<int, int>();

            int added = 0;
            for (var i = 0; i < lightmapsbaked.Length; i++)
            {
                if (!indexesToConvert.Contains(i))
                    continue;

                remapTable[i] = added;
                added++;

                var lightmapData = lightmapsbaked[i];
                colors.Add(lightmapData.lightmapColor);
                directions.Add(lightmapData.lightmapDir);
                shadowMasks.Add(lightmapData.shadowMask);
            }
            return new Tuple<LightMaps, Dictionary<int, int>>(ConstructLightMaps(colors, directions, shadowMasks), remapTable);
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

        public struct LightMapInformation
        {
            public LightMaps lightmaps;
            public Dictionary<Tuple<Renderer, int>, Material> rendererToMaterialMap;
            public Dictionary<int, int> lightmapIndexRemap;
        }

        // build a map of renderer / submesh -> Material.
        // create a new material if lightmappingis needed
        public static LightMapInformation GenerateLightMappingData(List<MeshRenderer> renderers)
        {
            var returnMap = new Dictionary<Tuple<Renderer, int>, Material>();

            Dictionary<MaterialLookupKey, Material> lightMappedMaterialCache = new();

            List<int> usedIndices = new();
            foreach (var renderer in renderers)
                usedIndices.Add(renderer.lightmapIndex);

            var lightmaps = GetLightmapsStruct(usedIndices);
            var maps = lightmaps.Item1;

            // renderer
            foreach (var renderer in renderers)
            {
                if (renderer == null)
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
                        || sharedMaterials[i] == null
                        || lightmapIndex is < 0 or 65534
                        || lightmapIndex > maps.colors.depth)
                        material = sharedMaterials[i];
                    else
                    {
                        material = GetLightMappedMaterial(sharedMaterials[i], maps, lightMappedMaterialCache);
                    }

                    returnMap.Add(new Tuple<Renderer, int>(renderer, i), material);
                }
            }
            return new LightMapInformation
            {
                lightmaps = maps,
                lightmapIndexRemap = lightmaps.Item2,
                rendererToMaterialMap = returnMap
            };
        }

        public void Destroy()
        {
            if (colors != null)
                Object.Destroy(colors);
            if (directions != null)
                Object.Destroy(directions);
            if (shadowMasks != null)
                Object.Destroy(shadowMasks);
        }
    }
}
