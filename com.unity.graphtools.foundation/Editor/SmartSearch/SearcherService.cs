using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// <see cref="EditorWindow"/> used for the Searcher.
    /// </summary>
    class SearcherWindow : UnityEditor.GraphToolsFoundation.Searcher.SearcherWindow
    {
    }

    /// <summary>
    /// Searcher adapter used in GraphToolsFundations
    /// </summary>
    public interface ISearcherAdapter : UnityEditor.GraphToolsFoundation.Searcher.ISearcherAdapter
    {
        /// <summary>
        /// Sets the initial ratio for the details panel splitter.
        /// </summary>
        /// <param name="ratio">Ratio to set.</param>
        void SetInitialSplitterDetailRatio(float ratio);
    }

    /// <summary>
    /// Helper class providing searcher related functionality in GraphToolsFundations.
    /// </summary>
    public static class SearcherService
    {
        public static class Usage
        {
            public const string CreateNode = "create-node";
            public const string Values = "values";
            public const string Types = "types";
        }

        static readonly Searcher.SearcherWindow.Alignment k_FindAlignment = new Searcher.SearcherWindow.Alignment(
            Searcher.SearcherWindow.Alignment.Vertical.Top, Searcher.SearcherWindow.Alignment.Horizontal.Center);
        static readonly TypeSearcherAdapter k_TypeAdapter = new TypeSearcherAdapter();

        public static readonly Comparison<SearcherItem> TypeComparison = (x, y) =>
        {
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        };

        /// <summary>
        /// Display the searcher with the given parameters.
        /// </summary>
        /// <param name="preferences">The tool preferences.</param>
        /// <param name="position">The position in hostWindow coordinates.</param>
        /// <param name="callback">The function called when a selection is made.</param>
        /// <param name="dbs">The <see cref="SearcherDatabaseBase"/> that contains the items to be displayed.</param>
        /// <param name="filter">The search filter.</param>
        /// <param name="adapter">The searcher adapter.</param>
        /// <param name="usage">The usage string used to identify the searcher use.</param>
        /// <param name="hostWindow">The <see cref="EditorWindow"/> of the host.</param>
        public static void ShowSearcher(Preferences preferences,
            Vector2 position,
            Action<GraphNodeModelSearcherItem> callback,
            IEnumerable<SearcherDatabaseBase> dbs,
            SearcherFilter filter,
            ISearcherAdapter adapter,
            string usage,
            EditorWindow hostWindow)
        {
            var searcherSize = preferences.GetSearcherSize(usage);
            var rect = new Rect(position, searcherSize.Size);

            adapter.SetInitialSplitterDetailRatio(searcherSize.RightLeftRatio);

            EditorWindow window = null;
            var prefs = preferences;

            bool OnItemSelectFunc(SearcherItem item)
            {
                if (item is GraphNodeModelSearcherItem dataProvider)
                {
                    callback(dataProvider);
                    return true;
                }

                return false;
            }

            var searcher = new Searcher.Searcher(dbs, adapter, filter, usage);

            if (prefs?.GetBool(BoolPref.SearcherInRegularWindow) ?? false)
                window = PromptSearcherInOwnWindow(searcher, OnItemSelectFunc, rect);
            else
                PromptSearcherPopup(searcher, OnItemSelectFunc, rect, hostWindow);
            ListenToSearcherSize(preferences, usage, window);
        }

        static void ListenToSearcherSize(Preferences preferences, string usage, EditorWindow existingWindow = null)
        {
            // This is a retro engineering of the searcher to get changes in the hostWindow size and splitter position
            var searcherWindow = existingWindow != null ? existingWindow : EditorWindow.GetWindow<Searcher.SearcherWindow>();
            var searcherResizer = searcherWindow.rootVisualElement.Q("windowResizer");
            var rightPanel = searcherWindow.rootVisualElement.Q("windowDetailsVisualContainer");
            var leftPanel = searcherWindow.rootVisualElement.Q("searcherVisualContainer");

            if (searcherResizer != null)
            {
                EventCallback<GeometryChangedEvent> callback = _ =>
                {
                    float ratio = 1.0f;
                    if (rightPanel != null && leftPanel != null)
                        ratio = rightPanel.resolvedStyle.flexGrow / leftPanel.resolvedStyle.flexGrow;

                    preferences.SetSearcherSize(usage ?? "", searcherWindow.position.size, ratio);
                };

                searcherWindow.rootVisualElement.RegisterCallback(callback);
                leftPanel?.RegisterCallback(callback);
            }
        }

        public static void ShowInputToGraphNodes(Stencil stencil, string toolName, Type graphViewType, Preferences preferences, IGraphModel graphModel,
            IEnumerable<IPortModel> portModels, Vector2 position, Action<GraphNodeModelSearcherItem> callback, EditorWindow hostWindow)
        {
            var filter = stencil.GetSearcherFilterProvider()?.GetInputToGraphSearcherFilter(portModels);
            var adapter = stencil.GetSearcherAdapter(graphModel, "Add an input node", toolName, graphViewType, portModels);
            var dbProvider = stencil.GetSearcherDatabaseProvider();

            if (dbProvider == null)
                return;

            var dbs = dbProvider.GetGraphElementsSearcherDatabases(graphModel)
                .Concat(dbProvider.GetGraphVariablesSearcherDatabases(graphModel))
                .Concat(dbProvider.GetDynamicSearcherDatabases(portModels))
                .ToList();

            ShowSearcher(preferences, position, callback, dbs, filter, adapter, Usage.CreateNode, hostWindow);
        }

        public static void ShowOutputToGraphNodes(Stencil stencil, string toolName, Type graphViewType, Preferences preferences, IGraphModel graphModel,
            IPortModel portModel, Vector2 position, Action<GraphNodeModelSearcherItem> callback, EditorWindow hostWindow)
        {
            var filter = stencil.GetSearcherFilterProvider()?.GetOutputToGraphSearcherFilter(portModel);
            var adapter = stencil.GetSearcherAdapter(graphModel, $"Choose an action for {portModel.DataTypeHandle.GetMetadata(stencil).FriendlyName}", toolName, graphViewType, Enumerable.Repeat(portModel, 1));
            var dbProvider = stencil.GetSearcherDatabaseProvider();

            if (dbProvider == null)
                return;

            var dbs = dbProvider.GetGraphElementsSearcherDatabases(graphModel).ToList();

            ShowSearcher(preferences, position, callback, dbs, filter, adapter, Usage.CreateNode, hostWindow);
        }

        public static void ShowOutputToGraphNodes(Stencil stencil, string toolName, Type graphViewType, Preferences preferences, IGraphModel graphModel,
            IEnumerable<IPortModel> portModels, Vector2 position, Action<GraphNodeModelSearcherItem> callback, EditorWindow hostWindow)
        {
            var filter = stencil.GetSearcherFilterProvider()?.GetOutputToGraphSearcherFilter(portModels);
            var adapter = stencil.GetSearcherAdapter(graphModel, $"Choose an action for {portModels.First().DataTypeHandle.GetMetadata(stencil).FriendlyName}", toolName, graphViewType, portModels);
            var dbProvider = stencil.GetSearcherDatabaseProvider();

            if (dbProvider == null)
                return;

            var dbs = dbProvider.GetGraphElementsSearcherDatabases(graphModel).ToList();

            ShowSearcher(preferences, position, callback, dbs, filter, adapter, Usage.CreateNode, hostWindow);
        }

        public static void ShowEdgeNodes(Stencil stencil, string toolName, Type graphViewType, Preferences preferences, IGraphModel graphModel,
            IEdgeModel edgeModel, Vector2 position, Action<GraphNodeModelSearcherItem> callback, EditorWindow hostWindow)
        {
            var filter = stencil.GetSearcherFilterProvider()?.GetEdgeSearcherFilter(edgeModel);
            var adapter = stencil.GetSearcherAdapter(graphModel, "Insert Node", toolName, graphViewType);
            var dbProvider = stencil.GetSearcherDatabaseProvider();

            if (dbProvider == null)
                return;

            var dbs = dbProvider.GetGraphElementsSearcherDatabases(graphModel).ToList();

            ShowSearcher(preferences, position, callback, dbs, filter, adapter, Usage.CreateNode, hostWindow);
        }

        public static void ShowGraphNodes(Stencil stencil, string toolName, Type graphViewType, Preferences preferences, IGraphModel graphModel,
            Vector2 position, Action<GraphNodeModelSearcherItem> callback, EditorWindow hostWindow)
        {
            var filter = stencil.GetSearcherFilterProvider()?.GetGraphSearcherFilter();
            var adapter = stencil.GetSearcherAdapter(graphModel, "Add a graph node", toolName, graphViewType);
            var dbProvider = stencil.GetSearcherDatabaseProvider();

            if (dbProvider == null)
                return;

            var dbs = dbProvider.GetGraphElementsSearcherDatabases(graphModel)
                .Concat(dbProvider.GetDynamicSearcherDatabases((IPortModel)null))
                .ToList();

            ShowSearcher(preferences, position, callback, dbs, filter, adapter, Usage.CreateNode, hostWindow);
        }

        static void PromptSearcherPopup(Searcher.Searcher searcher, Func<SearcherItem, bool> onItemSelect, Rect rect, EditorWindow hostWindow)
        {
            Searcher.SearcherWindow.Show(hostWindow, searcher,onItemSelect, null, rect);
        }

        static EditorWindow PromptSearcherInOwnWindow(Searcher.Searcher searcher, Func<SearcherItem, bool> onItemSelect, Rect rect)
        {
            return Searcher.SearcherWindow.ShowReusableWindow<SearcherWindow>(searcher, onItemSelect, null, rect);
        }

        public static void FindInGraph(
            EditorWindow host,
            IGraphModel graph,
            Action<FindInGraphAdapter.FindSearcherItem> highlightDelegate,
            Action<FindInGraphAdapter.FindSearcherItem> selectionDelegate
        )
        {
            var items = graph.NodeModels
                .Where(x => x is IHasTitle titled && !string.IsNullOrEmpty(titled.Title))
                .Select(x => MakeFindItems(x, (x as IHasTitle)?.Title))
                .ToList();

            var database = new SearcherDatabase(items);
            var searcher = new Searcher.Searcher(database, new FindInGraphAdapter(highlightDelegate));
            var position = new Vector2(host.rootVisualElement.layout.center.x, 0);

            Searcher.SearcherWindow.Show(host, searcher, item =>
            {
                selectionDelegate(item as FindInGraphAdapter.FindSearcherItem);
                return true;
            },
                position, null, k_FindAlignment);
        }

        internal static void ShowEnumValues(string title, Type enumType, Vector2 position, Action<Enum, int> callback)
        {
            var items = Enum.GetValues(enumType)
                .Cast<Enum>()
                .Select(v => new EnumValuesAdapter.EnumValueSearcherItem(v) as SearcherItem)
                .ToList();
            var database = new SearcherDatabase(items);
            var searcher = new Searcher.Searcher(database, new EnumValuesAdapter(title), context: "Enum" + enumType.FullName);

            Searcher.SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (item == null)
                    return false;

                callback(((EnumValuesAdapter.EnumValueSearcherItem)item).value, 0);
                return true;
            }, position, null);
        }

        public static void ShowValues(Preferences preferences, string title, IEnumerable<string> values, Vector2 position,
            Action<string> callback)
        {
            var searcherSize = preferences.GetSearcherSize(Usage.Values);
            position += EditorWindow.focusedWindow.position.position;
            var rect = new Rect(position, searcherSize.Size);

            var items = values.Select(v => new SearcherItem(v)).ToList();
            var database = new SearcherDatabase(items);
            var adapter = new SimpleSearcherAdapter(title);
            adapter.SetInitialSplitterDetailRatio(searcherSize.RightLeftRatio);
            var searcher = new Searcher.Searcher(database, adapter, context: Usage.Values);

            Searcher.SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (item == null)
                    return false;

                callback(item.Name);
                return true;
            }, null, rect);
            ListenToSearcherSize(preferences, Usage.Values);
        }

        public static void ShowVariableTypes(Stencil stencil, Preferences preferences, Vector2 position, Action<TypeHandle, int> callback)
        {
            var databases = stencil.GetSearcherDatabaseProvider()?.GetVariableTypesSearcherDatabases();
            if (databases != null)
                ShowTypes(preferences, databases, position, callback);
        }

        public static void ShowTypes(Preferences preferences, IEnumerable<SearcherDatabaseBase> databases, Vector2 position, Action<TypeHandle, int> callback)
        {
            var searcherSize = preferences.GetSearcherSize(Usage.Types);
            position += EditorWindow.focusedWindow.position.position;
            var rect = new Rect(position, searcherSize.Size);

            k_TypeAdapter.SetInitialSplitterDetailRatio(searcherSize.RightLeftRatio);

            var searcher = new Searcher.Searcher(databases, k_TypeAdapter, context: Usage.Types) { SortComparison = TypeComparison };

            Searcher.SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (!(item is TypeSearcherItem typeItem))
                    return false;

                callback(typeItem.Type, 0);
                return true;
            }, null, rect);
            ListenToSearcherSize(preferences, Usage.Types);
        }

        static SearcherItem MakeFindItems(INodeModel node, string title)
        {
            switch (node)
            {
                // TODO virtual property in NodeModel formatting what's displayed in the find hostWindow
                case IConstantNodeModel cnm:
                {
                    var nodeTitle = cnm.Type == typeof(string) ? $"\"{title}\"" : title;
                    title = $"Const {cnm.Type.Name} {nodeTitle}";
                    break;
                }
            }

            return new FindInGraphAdapter.FindSearcherItem(title, node);
        }
    }
}
