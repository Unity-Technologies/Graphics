using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class TestGraphView : GraphView
    {
        public SelectionDragger TestSelectionDragger => SelectionDragger;

        public TestGraphView(GraphViewEditorWindow window, BaseGraphTool graphTool, string name,
            GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive)
            : base(window, graphTool, name, displayMode)
        {
        }

        public TestGraphView(GraphViewEditorWindow window, BaseGraphTool graphTool,
            GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive)
            : this(window, graphTool, "", displayMode)
        {
        }

        public bool DisplaySmartSearchCalled { get; set; }
        public override void DisplaySmartSearch(Vector2 mousePosition)
        {
            DisplaySmartSearchCalled = true;
            base.DisplaySmartSearch(mousePosition);
        }

        public void RebuildUI()
        {
            foreach (var nodeModel in GraphModel.NodeModels)
            {
                var node = nodeModel.GetView<Node>(this);
                if(node != null)
                    RemoveElement(node);
            }

            foreach (var edge in GraphModel.EdgeModels)
            {
                var el = edge.GetView<Edge>(this);
                if (el != null)
                    RemoveElement(el);
            }

            foreach (var sticky in GraphModel.StickyNoteModels)
            {
                var el = sticky.GetView<StickyNote>(this);
                RemoveElement(el);
            }

            foreach (var placemat in GraphModel.PlacematModels)
            {
                var el = placemat.GetView<Placemat>(this);
                RemoveElement(el);
            }

            ViewForModel.Reset();

            foreach (var nodeModel in GraphModel.NodeModels)
            {
                var element = ModelViewFactory.CreateUI<GraphElement>(this, nodeModel);
                AddElement(element);
            }

            foreach (var edgeModel in GraphModel.EdgeModels)
            {
                var element = ModelViewFactory.CreateUI<GraphElement>(this, edgeModel);
                AddElement(element);
            }

            foreach (var stickyNoteModel in GraphModel.StickyNoteModels)
            {
                var element = ModelViewFactory.CreateUI<GraphElement>(this, stickyNoteModel);
                AddElement(element);
            }

            List<IModelView> placemats = new List<IModelView>();
            foreach (var placematModel in GraphModel.PlacematModels)
            {
                var element = ModelViewFactory.CreateUI<GraphElement>(this, placematModel);
                AddElement(element);
                placemats.Add(element);
            }

            // Update placemats to make sure hidden elements are all hidden (since
            // a placemat can hide a placemat UI created after itself).
            foreach (var placemat in placemats)
            {
                placemat.UpdateFromModel();
            }
        }
    }
}
