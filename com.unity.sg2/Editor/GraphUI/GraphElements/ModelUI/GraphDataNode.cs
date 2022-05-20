using Debug = UnityEngine.Debug;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Defs;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GraphDataNode : CollapsibleInOutNode
    {
        public const string PREVIEW_HINT = "Preview.Exists";
        public NodePreviewPart NodePreview => m_NodePreviewPart;
        NodePreviewPart m_NodePreviewPart;
        GraphDataNodeModel m_GraphDataNodeModel => NodeModel as GraphDataNodeModel;
        DynamicPartHolder m_StaticFieldParts;

        protected override void BuildPartList()
        {
            base.BuildPartList();

            // We don't want to display serialized fields on the node body, only ports
            // If this isn't in place, things like the PreviewMode dropdown show up on nodes
            PartList.RemovePart(nodeSettingsContainerPartName);

            if (NodeModel is not GraphDataNodeModel)
                return;

            // Retrieve the UI information about this node from its
            // NodeUIDescriptor, stored in the ShaderGraphStencil.
            var stencil = (ShaderGraphStencil)m_GraphDataNodeModel.GraphModel.Stencil;
            var nodeUIDescriptor = stencil.GetUIHints(m_GraphDataNodeModel.registryKey);

            // If the node has multiple possible topologies,
            // show the selector
            if (nodeUIDescriptor.SelectableFunctions.Count > 0)
            {
                FunctionSelectorPart part = new (
                    "sg-function-selector",
                    GraphElementModel,
                    this,
                    ussClassName,
                    nodeUIDescriptor.SelectableFunctions);
                PartList.InsertPartAfter(portContainerPartName, part);
            }

            if (!m_GraphDataNodeModel.TryGetNodeReader(out var nodeReader))
                return;

            m_StaticFieldParts = new(name, m_GraphDataNodeModel, this, ussClassName);

            var isNonPreviewableType = false;

            foreach (var portReader in nodeReader.GetPorts())
            {
                if (!portReader.IsHorizontal)
                    continue;
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

                if (!isStatic || parameterUIDescriptor.InspectorOnly)
                    continue;

                if (isGradientType)
                {
                    var gradientPart = new GradientPart(
                        "sg-gradient",
                        GraphElementModel,
                        this,
                        ussClassName,
                        portReader.LocalID);
                    m_StaticFieldParts.PartList.InsertPartAfter(
                        portContainerPartName,
                        gradientPart);
                    continue;
                }

                if (portReader.GetTypeField().GetRegistryKey().Name != Registry.ResolveKey<GraphType>().Name)
                    continue;

                var typeField = portReader.GetTypeField();
                if (typeField == null)
                    continue;

                var part = GetPartForPortField(portReader, typeField, parameterUIDescriptor);
                if (part != null)
                {
                    PartList.InsertPartAfter(portContainerPartName, part);
                }
            }

            // By default we assume all nodes should display previews, unless there
            // is a UIHint that dictates otherwise
            bool nodeHasPreview = nodeUIDescriptor.HasPreview;

            var shouldShowPreview = m_GraphDataNodeModel.existsInGraphData &&
                nodeHasPreview &&
                !isNonPreviewableType &&
                m_GraphDataNodeModel is not GraphDataContextNodeModel;

            if (shouldShowPreview)
                m_NodePreviewPart = new NodePreviewPart(
                    "node-preview",
                    GraphElementModel,
                    this,
                    ussClassName);
            PartList.AppendPart(m_NodePreviewPart);
        }

        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();
            Debug.Log("GraphDataNode.UpdateElementFromModel override");
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendSeparator();

            // Disable the expand/collapse option if the preview is already expanded/collapsed respectively
            evt.menu.AppendAction("Preview/Expand", action =>
            {
                GraphView.Dispatch(new ChangePreviewExpandedCommand(true, GraphView.GetSelection() ));
            },
                m_GraphDataNodeModel.IsPreviewExpanded ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

            evt.menu.AppendAction("Preview/Collapse", action =>
            {
                GraphView.Dispatch(new ChangePreviewExpandedCommand(false, GraphView.GetSelection() ));
            },
                m_GraphDataNodeModel.IsPreviewExpanded ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

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

        private IModelViewPart GetPartForPortField(
            PortHandler portHandler,
            FieldHandler fieldHandler,
            ParameterUIDescriptor parameterUIDescriptor)
        {
            // Figure out the correct part to display based on the port's fields.
            var length = GraphTypeHelpers.GetLength(fieldHandler);
            var height = GraphTypeHelpers.GetHeight(fieldHandler);
            var primitive = GraphTypeHelpers.GetPrimitive(fieldHandler);

            if (height > GraphType.Height.One)
            {
                return new MatrixPart(
                    "sg-matrix",
                    GraphElementModel,
                    this,
                    ussClassName,
                    portHandler.LocalID,
                    (int)height);
            }

            switch (length)
            {
                case GraphType.Length.One:
                {
                    switch (primitive)
                    {
                        case GraphType.Primitive.Bool:
                            return new BoolPart(
                                "sg-bool",
                                GraphElementModel,
                                this,
                                ussClassName,
                                portHandler.LocalID);
                        case GraphType.Primitive.Int:
                            return new IntPart(
                                "sg-int",
                                GraphElementModel,
                                this,
                                ussClassName,
                                portHandler.LocalID);
                        case GraphType.Primitive.Float:
                            if (parameterUIDescriptor.UseSlider)
                            {
                                return new SliderPart(
                                    "sg-slider",
                                    GraphElementModel,
                                    this,
                                    ussClassName,
                                    portHandler.LocalID);
                            }
                            else
                            {
                                return new FloatPart(
                                    "sg-float",
                                    GraphElementModel,
                                    this,
                                    ussClassName,
                                    portHandler.LocalID);
                            }
                        case GraphType.Primitive.Any:
                        // Not valid, the size should've been resolved.
                        default:
                            break;
                    }
                    break;
                }
                case GraphType.Length.Two:
                    return new Vector2Part(
                        "sg-vector2",
                        GraphElementModel,
                        this,
                        ussClassName,
                        portHandler.LocalID);
                case GraphType.Length.Three:
                    if (parameterUIDescriptor.UseColor)
                    {
                        return new ColorPart(
                            "sg-color",
                            GraphElementModel,
                            this,
                            ussClassName,
                            portHandler.LocalID,
                            includeAlpha: false);
                    }
                    else
                    {
                        return new Vector3Part(
                            "sg-vector3",
                            GraphElementModel,
                            this,
                            ussClassName,
                            portHandler.LocalID);
                    }
                case GraphType.Length.Four:
                    if (parameterUIDescriptor.UseColor)
                    {
                        return new ColorPart(
                            "sg-color",
                            GraphElementModel,
                            this,
                            ussClassName,
                            portHandler.LocalID,
                            includeAlpha: true);
                    }
                    else
                    {
                        return new Vector4Part(
                            "sg-vector4",
                            GraphElementModel,
                            this,
                            ussClassName,
                            portHandler.LocalID);
                    }
                case GraphType.Length.Any:
                // Not valid, the size should've been resolved.
                default:
                    break;
            }
            return null;
        }
    }
}
