using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor;
using UnityEditor.Search;

using UnityEngine;
using UnityEngine.Search;
using UnityEngine.VFX;


static class VFXPicker
{
    internal static void Pick(VisualEffectAsset vfxAsset, Action<VisualEffect> selectHandler)
    {
        var view = SearchService.ShowPicker(
            SearchService.CreateContext(CreateSceneRefProvider(vfxAsset)),
            (item, canceled) => SelectItem(item, canceled, selectHandler),
            null,
            null,
            null,
            "Visual Effect",
            0f);
        view.itemIconSize = 0f;

        // Until the "viewState" API is made public (should be in 2022.1) we use reflection to remove the inspector button
        var quickSearchType = typeof(SearchService).Assembly.GetType("UnityEditor.Search.QuickSearch");
        var viewStateInfo = quickSearchType?.GetProperty("viewState", BindingFlags.Instance | BindingFlags.NonPublic);
        var state = viewStateInfo?.GetValue(view);
        if (state != null)
        {
            var flagsInfo = state.GetType().GetField("flags", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            flagsInfo?.SetValue(state, SearchViewFlags.DisableInspectorPreview);
        }
    }

    static void SelectItem(SearchItem item, bool canceled, Action<VisualEffect> selectHandler)
    {
        if (!canceled)
        {
            if (item?.ToObject<GameObject>() is { } go
                && go.TryGetComponent(typeof(VisualEffect), out var component)
                && component is VisualEffect vfx)
            {
                selectHandler(vfx);
            }
            else
            {
                selectHandler(null);
            }
        }
    }

    static SearchProvider CreateSceneRefProvider(VisualEffectAsset vfxAsset)
    {
        return new SearchProvider("sref", "Visual Effect", (context, provider) => FetchSceneRefs(vfxAsset, context.searchQuery));
    }

    static IEnumerable<SearchItem> FetchSceneRefs(VisualEffectAsset vfxAsset, string userQuery)
    {
        var sceneProvider = SearchService.GetProvider("scene");

        var path = AssetDatabase.GetAssetPath(vfxAsset);
        var searchQuery = BuildQuery(path, userQuery);
        using (var query = SearchService.CreateContext(sceneProvider, searchQuery))
        using (var request = SearchService.Request(query))
        {
            return request.Where(x => IsValidItem(x, vfxAsset));
        }
    }

    static bool IsValidItem(in SearchItem item, VisualEffectAsset vfxAsset)
    {
        return item.ToObject<GameObject>().TryGetComponent(typeof(VisualEffect), out var component)
            && component is VisualEffect vfx
            && vfx.visualEffectAsset == vfxAsset;
    }

    static string BuildQuery(in string refPath, in string userQuery)
    {
        string baseQuery = $"ref=\"{refPath}\"";
        return string.IsNullOrEmpty(userQuery)
            ? baseQuery
            : $"{baseQuery} ({userQuery})";
    }
}
