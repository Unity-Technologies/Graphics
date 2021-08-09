using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements
{
    public class RedirectableEdge : Edge
    {
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            evt.menu.AppendAction("Add Redirect Node", action =>
            {
                if (Model is not EdgeModel edgeModel) return;
                CommandDispatcher.Dispatch(new AddRedirectNodeCommand(edgeModel, GraphView.ContentViewContainer.WorldToLocal(action.eventInfo.mousePosition)));
            });
        }
    }
}
