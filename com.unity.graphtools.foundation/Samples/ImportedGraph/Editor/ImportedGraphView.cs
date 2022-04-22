
using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.ImportedGraph
{
    public class ImportedGraphView : GraphView
    {
        public ImportedGraphView(GraphViewEditorWindow window, BaseGraphTool graphTool, string graphViewName,
            GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive)
            : base(window, graphTool, graphViewName, displayMode)
        {
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            var selection = GetSelection().ToList();

            if (selection.Any(element => element is INodeModel || element is IPlacematModel || element is IStickyNoteModel))
            {
                evt.menu.AppendAction("Export Subgraph", _ =>
                {
                    var template = new GraphTemplate<ImportedGraphStencil>("Importable Subgraph", GraphWrapper.assetExtension);
                    Dispatch(new CreateSubgraphCommand(typeof(ImportedGraphAsset), selection, template, this));
                }, selection.Count == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
            }

            base.BuildContextualMenu(evt);
        }
    }
}
