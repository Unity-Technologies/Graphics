using System;
using System.Reflection;

using UnityEngine;
using UnityEngine.Search;


namespace UnityEditor.VFX.UI
{
    static class TexturePicker
    {
        internal static void Pick(Type textureType, Action<Texture, bool> selectHandler)
        {
            var view = Search.SearchService.ShowObjectPicker(
                (x, y) => selectHandler(x as Texture, y),
                null,
                null,
                "Texture",
                textureType);
            view.itemIconSize = 1f;

            // Until the "viewState" API is made public (should be in 2022.1) we use reflection to remove the inspector button
            var quickSearchType = typeof(Search.SearchService).Assembly.GetType("UnityEditor.Search.QuickSearch");
            var viewStateInfo =
                quickSearchType?.GetProperty("viewState", BindingFlags.Instance | BindingFlags.NonPublic);
            var state = viewStateInfo?.GetValue(view);
            if (state != null)
            {
                var flagsInfo = state.GetType().GetField("flags", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                flagsInfo?.SetValue(state, SearchViewFlags.DisableInspectorPreview);
            }
        }
    }
}
