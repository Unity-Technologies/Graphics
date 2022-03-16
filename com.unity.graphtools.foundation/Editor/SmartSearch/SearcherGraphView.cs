using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    static class SearcherGraphView
    {
        static readonly string ussClassName = "ge-searcher-graph-view";

        public static GraphView CreateSearcherGraphView(Type graphViewType)
        {
            var graphView = Activator.CreateInstance(graphViewType, null, null, "", GraphViewDisplayMode.NonInteractive) as GraphView;

            if (graphView == null)
                return null;

            graphView.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetHelper.AssetPath +
                "SmartSearch/Stylesheets/SearcherGraphView.uss"));

            graphView.AddToClassList(ussClassName);

            return graphView;
        }
    }
}
