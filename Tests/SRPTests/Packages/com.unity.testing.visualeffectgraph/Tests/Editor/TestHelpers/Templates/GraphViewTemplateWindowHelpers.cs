using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Search;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.Test
{
    static class GraphViewTemplateWindowHelpers
    {
        const float kTimeout = 30_000f; // 30 seconds

        public static IEnumerator WaitTemplateSearchCompleted(GraphViewTemplateWindow templateWindow)
        {
            var searchProviderField = templateWindow.GetType().GetField("m_SearchProvider", BindingFlags.Instance | BindingFlags.NonPublic);
            var searchProvider = (TemplateSearchProvider)searchProviderField?.GetValue(templateWindow);
            Assert.NotNull(searchProvider, "Search provider should not be null.");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (searchProvider.IsSearching && sw.ElapsedMilliseconds < kTimeout)
            {
                yield return null;
            }
            sw.Stop();
            Assert.IsFalse(searchProvider.IsSearching, $"Search indexing did not end after {sw.ElapsedMilliseconds} ms.");
            Debug.Log($"Waited {sw.ElapsedMilliseconds} ms for search to complete.");
        }

        public static IEnumerator WaitUntilTemplatesAreCollected(GraphViewTemplateWindow templateWindow)
        {
            yield return WaitTemplateSearchCompleted(templateWindow);

            var templateTree = GetTemplateTree(templateWindow);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (templateTree.Count == 0 && sw.ElapsedMilliseconds < kTimeout)
            {
                yield return null;
            }
            sw.Stop();
            Assert.Greater(templateTree.Count, 0, $"No templates found in the template window after {sw.ElapsedMilliseconds} ms.");
            Debug.Log($"Waited {sw.ElapsedMilliseconds} ms for templates to be collected.");
        }

        public static bool TrySetSaveFileDialogHelper(GraphViewTemplateWindow window, GraphViewTemplateWindow.ISaveFileDialogHelper saveFileDialogHelper)
        {
            var templateHelperField = window.GetType().GetField("m_TemplateHelper", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(templateHelperField);
            var templateHelper = (ITemplateHelper)templateHelperField.GetValue(window);
            if (templateHelper != null)
            {
                templateHelper.saveFileDialogHelper = saveFileDialogHelper;
                return true;
            }

            return false;
        }

        public static void SetLastUsedTemplatePref(string guid = "a8d8823499ff50847aa460cb119c445d")
        {
            // Force selection of the simple loop template
            var templateWindowPrefs = new GraphViewTemplateWindowPrefs { LastUsedTemplateGuid = guid };
            templateWindowPrefs.SavePrefs(VFXTemplateHelperInternal.VFXGraphToolKey);
        }

        public static void SortBy(GraphViewTemplateWindow window, string sortBy)
        {
            var sortDropdown = window.rootVisualElement.Q<DropdownField>();
            Assert.NotNull(sortDropdown);
            sortDropdown.value = sortBy;
        }

        public static TreeView GetTreeView(GraphViewTemplateWindow window)
        {
            var treeViewField = window.GetType().GetField("m_ListOfTemplates", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(treeViewField);
            TreeView treeView = treeViewField.GetValue(window) as TreeView;
            Assert.NotNull(treeView);

            return treeView;
        }

        public static void ClearFavorite(List<TreeViewItemData<ITemplateDescriptor>> items)
        {
            items.ForEach(x =>
            {
                var id = GraphViewTemplateWindow.GetGlobalId((GraphViewTemplateDescriptor)x.data);
                SearchSettings.RemoveItemFavorite(new SearchItem(id));
            });
            SearchSettings.Save();
        }

        public static List<TreeViewItemData<ITemplateDescriptor>> GetTemplateTree(GraphViewTemplateWindow window)
        {
            var templateTreeField = window.GetType().GetField("m_TemplatesTree", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(templateTreeField);
            List<TreeViewItemData<ITemplateDescriptor>> templateTree = templateTreeField.GetValue(window) as List<TreeViewItemData<ITemplateDescriptor>>;
            Assert.NotNull(templateTree);

            return templateTree;
        }
    }
}
