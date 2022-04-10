using Debug = UnityEngine.Debug;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GraphDataNode : CollapsibleInOutNode
    {
        public const string PREVIEW_HINT = "Preview.Exists";
        public NodePreviewPart NodePreview => m_NodePreviewPart;
        NodePreviewPart m_NodePreviewPart;
        GraphDataNodeModel m_GraphDataNodeModel => NodeModel as GraphDataNodeModel;

        protected override void BuildPartList()
        {
            base.BuildPartList();

            if (Model is not GraphDataNodeModel graphDataNodeModel)
                return;

            // Retrieve the UI information about this node from its
            // NodeUIDescriptor, stored in the ShaderGraphStencil.
            var stencil = (ShaderGraphStencil)m_GraphDataNodeModel.GraphModel.Stencil;
            var nodeUIDescriptor = stencil.GetUIHints(m_GraphDataNodeModel.registryKey);

            // If the node has multiple possible topologies, show a selector.
            if (nodeUIDescriptor.SelectableFunctions.Count > 0)
            {
                Debug.Log(nodeUIDescriptor.SelectableFunctions.Keys);
            }

            if (!graphDataNodeModel.TryGetNodeReader(out var nodeReader))
                return;

            var isNonPreviewableType = false;

            foreach (var portReader in nodeReader.GetPorts())
            {
                // Only add new node parts for static ports.
                var staticField = portReader.GetTypeField().GetSubField<bool>("IsStatic");
                var portKey = portReader.GetTypeField().GetRegistryKey();
                bool isStatic = staticField?.GetData() ?? false;
                bool isGradientType = portKey.Name == Registry.ResolveKey<GradientType>().Name;
                var parameterUIDescriptor = nodeUIDescriptor.GetParameterInfo(portReader.LocalID);

                // GradientType cannot be previewed if directly acting as the output,
                // disable preview part on it if so
                if (isGradientType && !portReader.IsInput)
                    isNonPreviewableType = true;

                if (!isStatic)
                    continue;

                if (parameterUIDescriptor.InspectorOnly)
                    continue;

                if (isGradientType)
                {
                    PartList.InsertPartAfter(
                        portContainerPartName,
                        new GradientPart("sg-gradient", GraphElementModel, this, ussClassName, portReader.LocalID));
                    continue;
                }

                if (portReader.GetTypeField().GetRegistryKey().Name != Registry.ResolveKey<GraphType>().Name)
                    continue;

                var typeField = portReader.GetTypeField();
                if (typeField == null)
                    continue;

                // Figure out the correct part to display based on the port's fields.
                var length = GraphTypeHelpers.GetLength(typeField);
                var height = GraphTypeHelpers.GetHeight(typeField);
                var primitive = GraphTypeHelpers.GetPrimitive(typeField);

                if (height > GraphType.Height.One)
                {
                    PartList.InsertPartAfter(portContainerPartName, new MatrixPart("sg-matrix", GraphElementModel, this, ussClassName, portReader.LocalID, (int)height));
                    continue;
                }

                switch (length)
                {
                    case GraphType.Length.One:
                    {
                        switch (primitive)
                        {
                            case GraphType.Primitive.Bool:
                                PartList.InsertPartAfter(
                                    portContainerPartName,
                                    new BoolPart("sg-bool", GraphElementModel, this, ussClassName, portReader.LocalID)
                                );
                                break;
                            case GraphType.Primitive.Int:
                                PartList.InsertPartAfter(
                                    portContainerPartName,
                                    new IntPart("sg-int", GraphElementModel, this, ussClassName, portReader.LocalID)
                                );
                                break;
                            case GraphType.Primitive.Float:
                                if (parameterUIDescriptor.UseSlider)
                                {
                                    PartList.InsertPartAfter(portContainerPartName, new SliderPart("sg-slider", GraphElementModel, this, ussClassName, portReader.LocalID));
                                }
                                else
                                {
                                    PartList.InsertPartAfter(portContainerPartName, new FloatPart("sg-float", GraphElementModel, this, ussClassName, portReader.LocalID));
                                }
                                break;
                            case GraphType.Primitive.Any:
                            default:
                                break;
                        }
                        break;
                    }
                    case GraphType.Length.Two:
                        PartList.InsertPartAfter(
                            portContainerPartName,
                            new Vector2Part("sg-vector2", GraphElementModel, this, ussClassName, portReader.LocalID)
                        );
                        break;
                    case GraphType.Length.Three:
                        if (parameterUIDescriptor.UseColor)
                        {
                            PartList.InsertPartAfter(
                                portContainerPartName,
                                new ColorPart("sg-color", GraphElementModel, this, ussClassName, portReader.LocalID, includeAlpha: false)
                            );
                        }
                        else
                        {
                            PartList.InsertPartAfter(
                                portContainerPartName,
                                new Vector3Part("sg-vector3", GraphElementModel, this, ussClassName, portReader.LocalID)
                            );
                        }

                        break;
                    case GraphType.Length.Four:
                        if (parameterUIDescriptor.UseColor)
                        {
                            PartList.InsertPartAfter(
                                portContainerPartName,
                                new ColorPart("sg-color", GraphElementModel, this, ussClassName, portReader.LocalID, includeAlpha: true)
                            );
                        }
                        else
                        {
                            PartList.InsertPartAfter(
                                portContainerPartName,
                                new Vector4Part("sg-vector4", GraphElementModel, this, ussClassName, portReader.LocalID)
                            );
                        }
                        break;
                    case GraphType.Length.Any:
                        // Not valid, the size should've been resolved.
                        break;
                    default:
                        break;
                }
            }

            // By default we assume all nodes should display previews, unless there is a UIHint that dictates otherwise
            bool nodeHasPreview = nodeUIDescriptor.HasPreview;

            var shouldShowPreview = m_GraphDataNodeModel.existsInGraphData && nodeHasPreview && !isNonPreviewableType;

            if (shouldShowPreview)
                m_NodePreviewPart = new NodePreviewPart("node-preview", GraphElementModel, this, ussClassName);

            PartList.AppendPart(m_NodePreviewPart);

        }

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
