using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Graphview used by the searcher to instantiate previews.
    /// </summary>
    class SearcherGraphView : GraphView
    {
        new static readonly string ussClassName = "ge-searcher-graph-view";

        public SearcherGraphView(GraphViewEditorWindow window, BaseGraphTool graphTool) : base(window, graphTool, "")
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetHelper.AssetPath +
                "SmartSearch/Stylesheets/SearcherGraphView.uss"));

            AddToClassList(ussClassName);

            UnregisterCallback<ValidateCommandEvent>(OnValidateCommand);
            UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
        }
    }
}
