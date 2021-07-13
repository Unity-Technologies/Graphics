using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Views
{
    public class ShaderGraphView : GraphView
    {
        public ShaderGraphView(GraphViewEditorWindow window, CommandDispatcher commandDispatcher,
            string graphViewName)
            : base(window, commandDispatcher, graphViewName)
        {
            
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            evt.menu.AppendSeparator();
        }
    }
}
