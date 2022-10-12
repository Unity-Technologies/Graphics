using Unity.GraphToolsFoundation.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class RedirectableEdge : Wire
    {
        public RedirectableEdge()
        {
            RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount != 2 || evt.button != 0) return;
                if (Model is not WireModel edgeModel) return;

                RootView.Dispatch(new AddRedirectNodeCommand(edgeModel, GraphView.ContentViewContainer.WorldToLocal(evt.mousePosition)));
            });
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // TODO: (Sai) Revisit this other functionality that GTF
            // TODO: exposes once we've achieved feature parity with old SG
            //base.BuildContextualMenu(evt);

            evt.menu.AppendAction("Add Redirect Node", action =>
            {
                if (Model is not WireModel edgeModel) return;
                RootView.Dispatch(new AddRedirectNodeCommand(edgeModel, GraphView.ContentViewContainer.WorldToLocal(action.eventInfo.mousePosition)));
            });
        }
    }
}
