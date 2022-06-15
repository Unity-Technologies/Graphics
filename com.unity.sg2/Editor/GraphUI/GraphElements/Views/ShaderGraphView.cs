using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphView : GraphView
    {
        public ShaderGraphView(
            GraphViewEditorWindow window,
            BaseGraphTool graphTool,
            string graphViewName,
            GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive)
            : base(window, graphTool, graphViewName, displayMode)
        {
            // This can be called by the searcher and if so, all of these dependencies will be null, need to guard against that
            if(window != null)
                ViewSelection = new ShaderGraphViewSelection(this, GraphViewModel.GraphModelState, GraphViewModel.SelectionState);
        }

    //    protected override void CollectCopyableGraphElements(
    //        IEnumerable<IGraphElementModel> elements,
    //        HashSet<IGraphElementModel> elementsToCopySet)
    //    {
    //        var elementsList = elements.ToList();
    //        base.CollectCopyableGraphElements(elementsList, elementsToCopySet);

    //        // Pasting a redirect should also paste an edge to its source node.
    //        foreach (var redirect in elementsList.OfType<RedirectNodeModel>())
    //        {
    //            var incomingEdge = redirect.GetIncomingEdges().FirstOrDefault();
    //            if (incomingEdge != null) elementsToCopySet.Add(incomingEdge);
    //        }
    //    }
    }
}
