using GtfPlayground.Commands;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;

namespace GtfPlayground
{
    public class PlaygroundGraphView : GraphView
    {
        public PlaygroundGraphView(GraphViewEditorWindow window, CommandDispatcher commandDispatcher,
            string graphViewName) : base(window, commandDispatcher, graphViewName)
        {
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            evt.menu.AppendSeparator();
            
            evt.menu.AppendAction("Scatter Nodes", action =>
            {
                CommandDispatcher.Dispatch(new ScatterNodesCommand());
            });
            
            evt.menu.AppendAction("Generate Graph", action =>
            {
                CommandDispatcher.Dispatch(new GenerateGraphCommand());
            });
        }
    }
}