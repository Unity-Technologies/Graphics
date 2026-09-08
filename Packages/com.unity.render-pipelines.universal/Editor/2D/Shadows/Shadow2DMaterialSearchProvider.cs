using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Unity Search provider backing the object picker on <c>Light2D.shadowMaterial</c>. It offers
    /// only Materials whose shader declares all five 2D shadow passes, so the picker cannot hand the
    /// user a Material that would silently drop a phase of the shadow.
    /// </summary>
    /// <remarks>
    /// Referenced from the serialized field by provider <em>id</em> rather than by <see cref="System.Type"/>:
    /// <c>SearchContextAttribute</c> is applied in the runtime assembly, which cannot see the
    /// editor-only <see cref="SearchProvider"/> type. The string id crosses that boundary.
    ///
    /// This filter is a discovery aid, not a guarantee. Drag-and-drop onto the field bypasses the
    /// picker entirely, so <c>Light2DEditor</c>'s compatibility error remains the actual guard.
    /// </remarks>
    static class Shadow2DMaterialSearchProvider
    {
        // Shared with the SearchContext attribute on Light2D.m_ShadowMaterial, which names the
        // provider by string id because the runtime assembly cannot see SearchProvider. Taking the id
        // from the component rather than redeclaring it here keeps the two from drifting apart -- a
        // mismatch would silently fall back to the unfiltered picker.
        internal const string k_ProviderId = Light2D.k_ShadowMaterialSearchProviderId;
        const string k_DisplayName = "2D Shadow Materials";

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(k_ProviderId, k_DisplayName)
            {
                priority = 100,
                filterId = "shadow2d:",
                fetchItems = (context, items, provider) => FetchCompatibleMaterials(context, provider),
                fetchThumbnail = (item, context) => AssetPreview.GetMiniThumbnail(ResolveMaterial(item)),
                toObject = (item, type) => ResolveMaterial(item)
            };
        }

        // Yielded lazily so Search streams results and the picker stays responsive on large projects:
        // deciding compatibility means loading each candidate Material to reach its shader.
        //
        // The per-Shader memo is what keeps that affordable. Compatibility is a property of the shader,
        // and a project has far fewer shaders than materials, so each distinct shader is probed once
        // per search rather than once per material. The dictionary is deliberately local to the fetch
        // rather than static: a shader edited between two picker sessions must be re-probed, and a
        // long-lived cache would have to be invalidated on asset import to stay honest.
        static IEnumerable<SearchItem> FetchCompatibleMaterials(SearchContext context, SearchProvider provider)
        {
            var compatibleByShader = new Dictionary<Shader, bool>();
            var guids = AssetDatabase.FindAssets("t:Material");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                    continue;

                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || material.shader == null)
                    continue;

                if (!compatibleByShader.TryGetValue(material.shader, out var compatible))
                {
                    compatible = ShadowRendering.IsCompatibleShadowShader(material.shader);
                    compatibleByShader[material.shader] = compatible;
                }

                if (!compatible)
                    continue;

                // Free-text in the picker's search box narrows by name, matching how the default
                // object picker behaves. An empty query lists every compatible Material.
                if (!string.IsNullOrEmpty(context.searchQuery) &&
                    material.name.IndexOf(context.searchQuery, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                yield return provider.CreateItem(context, path, material.name, path, null, null);
            }
        }

        // Items are keyed by asset path (SearchItem.id), so resolving one is a plain load.
        static Material ResolveMaterial(SearchItem item)
        {
            return item == null || string.IsNullOrEmpty(item.id)
                ? null
                : AssetDatabase.LoadAssetAtPath<Material>(item.id);
        }
    }
}
