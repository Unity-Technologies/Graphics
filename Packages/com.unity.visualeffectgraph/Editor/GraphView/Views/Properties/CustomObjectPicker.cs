using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor.Search;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Search;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    static class CustomObjectPicker
    {
        internal static void Pick(Type type, TextureDimension textureDimension, Action<Object, bool> selectHandler)
        {
            SearchViewState state;
            if (typeof(Texture).IsAssignableFrom(type))
            {
                state = GetTexturePickerView(type, textureDimension, selectHandler);
            }
            else if (typeof(ShaderGraphVfxAsset).IsAssignableFrom(type))
            {
                state = GetShaderGraphPickerView(selectHandler);
            }
            else
            {
                ShowGenericView(type, selectHandler);
                return;
            }

            Search.SearchService.ShowPicker(state);
        }

        private static SearchViewState GetShaderGraphPickerView(Action<Object, bool> selectHandler)
        {
            return new SearchViewState
            {
                flags = SearchViewFlags.DisableInspectorPreview,
                title = "VFX Shader Graph",
                itemSize = 1f,
                selectHandler = (x, y) => selectHandler(x?.ToObject(), y),
                context = Search.SearchService.CreateContext(CreateShaderProvider()),
            };
        }

        private static SearchProvider CreateShaderProvider()
        {
            return new SearchProvider("shader", "Shader Graph", (context, _) => FetchShaderGraph(context));
        }

        private static IEnumerable<SearchItem> FetchShaderGraph(SearchContext context)
        {
            var userQuery = context.searchQuery;
            var providers = new[] { Search.SearchService.GetProvider("adb") };
            var assetProvider = Search.SearchService.GetProvider("asset");

            using (var query = Search.SearchService.CreateContext(providers, $"t:{nameof(ShaderGraphVfxAsset)} {userQuery}", context.options))
            using (var request = Search.SearchService.Request(query))
            {
                foreach (var r in request)
                {
                    if (r != null && r.ToObject<ShaderGraphVfxAsset>() == null)
                    {
                        var shader = r.ToObject<Shader>();
                        var path = AssetDatabase.GetAssetPath(shader);
                        var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                        var vfxShader = subAssets.SingleOrDefault(x => x is ShaderGraphVfxAsset);
                        if (vfxShader != null)
                        {
                            var gid = GlobalObjectId.GetGlobalObjectIdSlow(vfxShader);
                            path = AssetDatabase.GetAssetPath(vfxShader);
                            var item = Search.Providers.AssetProvider.CreateItem("vfxshader", context, assetProvider, gid.ToString(), path, r.score - 900, SearchDocumentFlags.Asset | SearchDocumentFlags.Nested);
                            item.label = vfxShader.name;
                            item.description = path;
                            yield return item;
                        }
                    }
                    else
                    {
                        yield return r;
                    }
                }
            }
        }

        static void ShowGenericView(Type type, Action<Object, bool> selectHandler)
        {
            Search.SearchService.ShowObjectPicker(
                selectHandler,
                null,
                null,
                type.Name,
                type);
        }

        static SearchViewState GetTexturePickerView(Type type, TextureDimension textureDimension, Action<Object, bool> selectHandler)
        {
            return new SearchViewState
            {
                flags = SearchViewFlags.DisableInspectorPreview,
                title = type.Name,
                itemSize = 5f,
                selectHandler = (x, y) => selectHandler(x?.ToObject(), y),
                context = Search.SearchService.CreateContext(CreateTextureProvider(type, textureDimension)),
            };
        }

        static SearchProvider CreateTextureProvider(Type type, TextureDimension textureDimension)
        {
            return new SearchProvider("tex", "Texture", (context, _) => FetchTextures(type, textureDimension, context));
        }

        static IEnumerable<SearchItem> FetchTextures(Type type, TextureDimension textureDimension, SearchContext context)
        {
            var userQuery = context.searchQuery;
            var providers = new[] { Search.SearchService.GetProvider("adb") };

            using (var query = Search.SearchService.CreateContext(providers, $"t:{type.Name} {userQuery}", context.options))
            using (var request = Search.SearchService.Request(query))
            {
                foreach (var r in request)
                {
                    if (r == null)
                    {
                        yield return null;
                    }
                    else
                    {
                        r.provider = Search.SearchUtils.CreateGroupProvider(r.provider, "Texture 2D", 0, true);
                        yield return r;
                    }
                }
            }

            if (type != typeof(RenderTexture))
            {
                using (var query = Search.SearchService.CreateContext(providers, $"t:{nameof(RenderTexture)} {userQuery}", context.options))
                using (var request = Search.SearchService.Request(query))
                {
                    foreach (var r in request)
                    {
                        if (r == null)
                        {
                            yield return null;
                        }
                        else
                        {
                            var rt = r.ToObject<RenderTexture>();
                            if (rt != null && rt.dimension == textureDimension)
                            {
                                r.provider =
                                    Search.SearchUtils.CreateGroupProvider(r.provider, "Render Texture", 0, true);
                                yield return r;
                            }
                            else
                            {
                                yield return null;
                            }
                        }
                    }
                }
            }
        }
    }
}
