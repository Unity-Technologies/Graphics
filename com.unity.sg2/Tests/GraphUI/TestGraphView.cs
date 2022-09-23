using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    class TestGraphView : ShaderGraphView
    {
        List<GraphElement> m_GraphElements = new();

        // Needed by GTF
        public TestGraphView(
            GraphViewEditorWindow window,
            BaseGraphTool graphTool,
            string graphViewName,
            GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive)
            : base(window, graphTool, graphViewName, displayMode)
        {

        }

        public TestGraphView(
            GraphViewEditorWindow window,
            BaseGraphTool graphTool,
            string graphViewName,
            PreviewUpdateDispatcher previewUpdateDispatcher,
            GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive)
            : base(window, graphTool, graphViewName, previewUpdateDispatcher, displayMode)
        {

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
