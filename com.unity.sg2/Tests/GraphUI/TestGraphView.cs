using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    class TestGraphView : ShaderGraphView
    {
        List<GraphElement> m_GraphElements = new();

        // Needed by GTF
        protected TestGraphView(
            GraphViewEditorWindow window,
            BaseGraphTool graphTool,
            string graphViewName,
            GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive)
            : base(window, graphTool, graphViewName, displayMode)
        {

        }

        protected TestGraphView(
            GraphViewEditorWindow window,
            BaseGraphTool graphTool,
            string graphViewName,
            PreviewUpdateDispatcher previewUpdateDispatcher,
            GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive)
            : base(window, graphTool, graphViewName, previewUpdateDispatcher, displayMode)
        {

        }

        public new static TestGraphView Create(
            GraphViewEditorWindow window,
            BaseGraphTool graphTool,
            string graphViewName,
            PreviewUpdateDispatcher previewUpdateDispatcher,
            GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive)
        {
            var graphView = new TestGraphView(window, graphTool, graphViewName, previewUpdateDispatcher, displayMode);
            graphView.Initialize();

            return graphView;
        }

        public GraphElement GetGraphElement(GraphElementModel elementModel)
        {
            return m_GraphElements.FirstOrDefault(graphElement => ReferenceEquals(graphElement.GraphElementModel, elementModel));
        }

        public override void AddElement(GraphElement graphElement)
        {
            base.AddElement(graphElement);

            m_GraphElements.Add(graphElement);
        }
    }
}
