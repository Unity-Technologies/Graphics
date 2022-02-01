using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class TestGraphView : GraphView
    {
        public SelectionDragger TestSelectionDragger => SelectionDragger;

        public TestGraphView(GraphViewEditorWindow window, BaseGraphTool graphTool)
            : base(window, graphTool, "")
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
                var node = nodeModel.GetUI<Node>(this);
                if(node != null)
                    RemoveElement(node);
            }

            foreach (var edge in GraphModel.EdgeModels)
            {
                var el = edge.GetUI<Edge>(this);
                if (el != null)
                    RemoveElement(el);
            }

            foreach (var sticky in GraphModel.StickyNoteModels)
            {
                var el = sticky.GetUI<StickyNote>(this);
                RemoveElement(el);
            }

            foreach (var placemat in GraphModel.PlacematModels)
            {
                var el = placemat.GetUI<Placemat>(this);
                RemoveElement(el);
            }

            UIForModel.Reset();

            foreach (var nodeModel in GraphModel.NodeModels)
            {
                var element = GraphElementFactory.CreateUI<GraphElement>(this, nodeModel);
                AddElement(element);
            }

            foreach (var edgeModel in GraphModel.EdgeModels)
            {
                var element = GraphElementFactory.CreateUI<GraphElement>(this, edgeModel);
                AddElement(element);
            }

            foreach (var stickyNoteModel in GraphModel.StickyNoteModels)
            {
                var element = GraphElementFactory.CreateUI<GraphElement>(this, stickyNoteModel);
                AddElement(element);
            }

            List<IModelUI> placemats = new List<IModelUI>();
            foreach (var placematModel in GraphModel.PlacematModels)
            {
                var element = GraphElementFactory.CreateUI<GraphElement>(this, placematModel);
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
