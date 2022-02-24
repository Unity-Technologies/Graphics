using Debug = UnityEngine.Debug;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Types;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GraphDataNode : CollapsibleInOutNode
    {
        NodePreviewPart m_NodePreviewPart;
        public NodePreviewPart NodePreview => m_NodePreviewPart;

        protected override void BuildPartList()
        {
            base.BuildPartList();

            var shouldShowPreview = m_GraphDataNodeModel.existsInGraphData;

            // TODO (Brett) This should only happen if m_GraphDataNodeMode.HasPreview
            if(shouldShowPreview)
                m_NodePreviewPart = new NodePreviewPart("node-preview", Model, this, ussClassName);
            PartList.AppendPart(m_NodePreviewPart);

            // TODO: Build out fields from node definition
            if (Model is not GraphDataNodeModel graphDataNodeModel) return;
            if (!graphDataNodeModel.TryGetNodeReader(out var nodeReader)) return;

            foreach (var portReader in nodeReader.GetPorts())
            {
                // Only add new node parts for static ports.
                if (!portReader.GetField("IsStatic", out bool isStatic) || !isStatic) continue;
                if (portReader.GetRegistryKey().Name != Registry.Registry.ResolveKey<GraphType>().Name) continue;

                // Figure out the correct part to display based on the port's fields.
                if (!portReader.GetField(GraphType.kHeight, out GraphType.Height height)) continue;
                if (!portReader.GetField(GraphType.kLength, out GraphType.Length length)) continue;

                if (height > GraphType.Height.One)
                {
                    PartList.InsertPartAfter(portContainerPartName, new MatrixPart("sg-matrix", Model, this, ussClassName, portReader.GetName(), (int)height));
                }

                if (length == GraphType.Length.One)
                {
                    if (!portReader.GetField(GraphType.kPrimitive, out GraphType.Primitive primitive)) continue;
                    switch (primitive)
                    {
                        case GraphType.Primitive.Bool:
                            // TODO: Checkbox
                            break;
                        case GraphType.Primitive.Int:
                            PartList.InsertPartAfter(portContainerPartName, new IntPart("sg-int", Model, this, ussClassName, portReader.GetName()));
                            break;
                        case GraphType.Primitive.Float:
                            PartList.InsertPartAfter(portContainerPartName, new FloatPart("sg-float", Model, this, ussClassName, portReader.GetName()));
                            break;
                        case GraphType.Primitive.Any:
                        default:
                            break;
                    }
                }
            }
        }

        GraphDataNodeModel m_GraphDataNodeModel => NodeModel as GraphDataNodeModel;

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendSeparator();

            // TODO: (Sai) Re-enable in Sprint 2
            // Currently commented out as we don't require preview expansion/collapse
            //evt.menu.AppendAction("Preview/Expand", action =>
            //{
            //    GraphView.Dispatch(new ChangePreviewExpandedCommand(true, new [] {m_GraphDataNodeModel}));
            //});
            //
            //evt.menu.AppendAction("Preview/Collapse", action =>
            //{
            //    GraphView.Dispatch(new ChangePreviewExpandedCommand(false, new [] {m_GraphDataNodeModel}));
            //});

            evt.menu.AppendAction("Copy Shader", action =>
            {
                Debug.LogWarning("UNIMPLEMENTED in GraphDataNode");
            });

            evt.menu.AppendAction("Show Generated Shader", action =>
            {
                Debug.LogWarning("UNIMPLEMENTED in GraphDataNode");
                // TODO (Brett) Get the shader code from the PreviewManager once it is implemented.
                // https://jira.unity3d.com/browse/GSG-780
            });

            evt.menu.AppendAction("Show Preview Code", action =>
            {
                Debug.LogWarning("UNIMPLEMENTED in GraphDataNode");
                // TODO (Brett) Get the shader code from the PreviewManager once it is implemented.
                // https://jira.unity3d.com/browse/GSG-780
            });

            evt.menu.AppendAction("Disconnect All", action =>
            {
                Debug.LogWarning("UNIMPLEMENTED in GraphDataNode");
            });

            base.BuildContextualMenu(evt);
        }
    }
}
