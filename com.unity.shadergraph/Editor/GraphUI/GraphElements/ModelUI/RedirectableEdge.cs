using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements
{
    public class RedirectableEdge : Edge
    {
        public RedirectableEdge()
        {
            RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount != 2 || evt.button != 0) return;
                if (Model is not EdgeModel edgeModel) return;

                CommandDispatcher.Dispatch(new AddRedirectNodeCommand(edgeModel, GraphView.ContentViewContainer.WorldToLocal(evt.mousePosition)));
            });
        }

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
