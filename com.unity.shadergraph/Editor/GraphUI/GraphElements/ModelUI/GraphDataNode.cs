using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.CommandDispatch;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements
{
    public class GraphDataNode : CollapsibleInOutNode
    {
        NodePreviewPart m_NodePreviewPart;
        public NodePreviewPart NodePreview => m_NodePreviewPart;

        protected override void BuildPartList()
        {
            base.BuildPartList();

            // TODO (Brett) This should only happen if m_GraphDataNodeMode.HasPreview
            m_NodePreviewPart = new NodePreviewPart("node-preview", Model, this, ussClassName);
            PartList.AppendPart(m_NodePreviewPart);

            // TODO: Build out fields from node definition
        }

        GraphDataNodeModel m_GraphDataNodeModel => NodeModel as GraphDataNodeModel;

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Preview/Expand", action =>
            {
                GraphView.Dispatch(new ChangePreviewExpandedCommand(true, new [] {m_GraphDataNodeModel}));
            });

            evt.menu.AppendAction("Preview/Collapse", action =>
            {
                GraphView.Dispatch(new ChangePreviewExpandedCommand(false, new [] {m_GraphDataNodeModel}));
            });

            evt.menu.AppendAction("Get Shader Code", action =>
            {
                // TODO (Brett) Get the shader code from the PreviewManager once it is implemented.
                // https://jira.unity3d.com/browse/GSG-780
            });

            // TODO: Add preview mode 2D/3D change submenu options to menu

            base.BuildContextualMenu(evt);
        }
    }
}
