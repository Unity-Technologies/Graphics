using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEditor.Search;
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
            var view = typeof(Texture).IsAssignableFrom(type)
                ? GetTexturePickerView(type, textureDimension, selectHandler)
                : GetGenericView(type, selectHandler);

            // Until the "viewState" API is made public (should be in 2022.1) we use reflection to remove the inspector button
            var quickSearchType = typeof(Search.SearchService).Assembly.GetType("UnityEditor.Search.QuickSearch");
            var viewStateInfo = quickSearchType?.GetProperty("viewState", BindingFlags.Instance | BindingFlags.NonPublic);
            var state = viewStateInfo?.GetValue(view);
            if (state != null)
            {
                var flagsInfo = state.GetType().GetField("flags", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                flagsInfo?.SetValue(state, SearchViewFlags.DisableInspectorPreview);
            }
        }

        static ISearchView GetGenericView(Type type, Action<Object, bool> selectHandler)
        {
            return Search.SearchService.ShowObjectPicker(
                selectHandler,
                null,
                null,
                type.Name,
                type);
        }

        static ISearchView GetTexturePickerView(Type type, TextureDimension textureDimension, Action<Object, bool> selectHandler)
        {
            var view = Search.SearchService.ShowPicker(
                Search.SearchService.CreateContext(CreateTextureProvider(type, textureDimension)),
                (x, y) => selectHandler(x?.ToObject(), y),
                null,
                null,
                null,
                type.Name,
                5f);
            view.itemIconSize = 5f;

            return view;
        }

        static SearchProvider CreateTextureProvider(Type type, TextureDimension textureDimension)
        {
            return new SearchProvider("tex", "Texture", (context, _) => FetchTextures(type, textureDimension, context));
        }

        static IEnumerable<SearchItem> FetchTextures(Type type, TextureDimension textureDimension, SearchContext context)
        {
            // This piece of code is meant to put RenderTextures in a separate tab
            // But the display is right now buggy, so keep it for later use when display issue is fixed
            //var createGroupProviderMethod = typeof(Search.SearchUtils).GetMethod("CreateGroupProvider", BindingFlags.NonPublic|BindingFlags.Static);
            //SearchProvider textureGroupProvider = null;
            //SearchProvider renderTextureGroupProvider = null;
            //if (createGroupProviderMethod != null)
            //{
            //    textureGroupProvider = createGroupProviderMethod.Invoke(null, new object[] { adbProvider, type.Name, 0, true }) as SearchProvider;
            //    renderTextureGroupProvider = createGroupProviderMethod.Invoke(null, new object[] { adbProvider, "Render Textures", 1, true }) as SearchProvider;;
            //}

            var userQuery = context.searchQuery;
            var providers = new[] { Search.SearchService.GetProvider("adb") };

            using (var query = Search.SearchService.CreateContext(providers, $"t:{type.Name} {userQuery}", context.options))
            using (var request = Search.SearchService.Request(query))
            {
                foreach (var r in request)
                {
                    //r.provider = textureGroupProvider;
                    yield return r;
                }
            }

            if (type != typeof(RenderTexture))
            {
                using (var query = Search.SearchService.CreateContext(providers, $"t:{nameof(RenderTexture)} {userQuery}", context.options))
                using (var request = Search.SearchService.Request(query))
                {
                    foreach (var r in request)
                    {
                        if (r == null) continue;
                        var rt = r.ToObject<RenderTexture>();
                        if (rt.dimension == textureDimension)
                        {
                            //r.provider = renderTextureGroupProvider;
                            yield return r;
                        }
                    }
                }
            }
        }
    }
}
