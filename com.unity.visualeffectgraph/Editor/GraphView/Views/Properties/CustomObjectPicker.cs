using System;
using System.Reflection;

using UnityEngine.Search;


namespace UnityEditor.VFX.UI
{
    static class CustomObjectPicker
    {
        internal static void Pick(Type textureType, Action<UnityEngine.Object, bool> selectHandler)
        {
            var view = Search.SearchService.ShowObjectPicker(
                selectHandler,
                null,
                null,
                textureType.Name,
                textureType);
            view.itemIconSize = 5f;

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
