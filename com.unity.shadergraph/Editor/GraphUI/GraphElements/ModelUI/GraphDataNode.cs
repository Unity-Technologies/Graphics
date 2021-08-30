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

            /*PartList.InsertPartAfter(titleIconContainerPartName, new DebugStringPart("registry-key", Model, this,
                ussClassName,
                m =>
                {
                    try
                    {
                        return ((GraphDataNodeModel) NodeModel).registryKey.ToString();
                    }
                    catch
                    {
                        return "Invalid key";
                    }
                }));

            /*PartList.InsertPartAfter(titleIconContainerPartName, new DebugStringPart("graph-data-path", Model, this,
                ussClassName, m => ((GraphDataNodeModel) m).graphDataName));*/

            if (/*m_GraphDataNodeModel.HasPreview*/ true)
            {
                m_NodePreviewPart = new NodePreviewPart(CommandDispatcher, "node-preview", Model, this, ussClassName);
                PartList.AppendPart(m_NodePreviewPart);
            }

            // TODO: Build out fields from node definition
        }

        GraphDataNodeModel m_GraphDataNodeModel => NodeModel as GraphDataNodeModel;

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Preview/Expand", action =>
            {
                CommandDispatcher.Dispatch(new ChangePreviewExpandedCommand(true, new [] {m_GraphDataNodeModel}));
            });

            evt.menu.AppendAction("Preview/Collapse", action =>
            {
                CommandDispatcher.Dispatch(new ChangePreviewExpandedCommand(false, new [] {m_GraphDataNodeModel}));
            });

            base.BuildContextualMenu(evt);

            // TODO: Add preview mode 2D/3D change submenu options to menu
        }
    }
}
