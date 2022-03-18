using System;
using Debug = UnityEngine.Debug;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Types;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GraphDataNode : CollapsibleInOutNode
    {
        private const string COLOR_HINT = ".UseColor";
        private const string SLIDER_HINT = ".UseSlider";

        NodePreviewPart m_NodePreviewPart;
        public NodePreviewPart NodePreview => m_NodePreviewPart;

        protected override void BuildPartList()
        {
            base.BuildPartList();

            var shouldShowPreview = m_GraphDataNodeModel.existsInGraphData;

            if (shouldShowPreview)
                m_NodePreviewPart = new NodePreviewPart("node-preview", GraphElementModel, this, ussClassName);

            PartList.AppendPart(m_NodePreviewPart);

            if (Model is not GraphDataNodeModel graphDataNodeModel) return;
            if (!graphDataNodeModel.TryGetNodeReader(out var nodeReader)) return;

            var stencil = (ShaderGraphStencil)m_GraphDataNodeModel.GraphModel.Stencil;
            var uiHints = stencil.GetUIHints(m_GraphDataNodeModel.registryKey);

            foreach (var portReader in nodeReader.GetPorts())
            {
                // Only add new node parts for static ports.
                var staticField = portReader.GetTypeField().GetField<bool>("IsStatic");
                var portKey = portReader.GetTypeField().GetRegistryKey();
                bool isStatic = staticField != null && staticField;
                if (portKey.Name == Registry.Registry.ResolveKey<GradientType>().Name)
                {
                    PartList.InsertPartAfter(portContainerPartName, new GradientPart("sg-gradient", GraphElementModel, this, ussClassName, portReader.GetName()));
                    continue;
                }
                if (!isStatic) continue;
                if (portReader.GetTypeField().GetRegistryKey().Name != Registry.Registry.ResolveKey<GraphType>().Name) continue;
                var portName = portReader.LocalID;

                var typeField = portReader.GetTypeField();
                if (typeField == null) continue;

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
                                    new BoolPart("sg-bool", GraphElementModel, this, ussClassName, portReader.GetName())
                                );
                                break;
                            case GraphType.Primitive.Int:
                                PartList.InsertPartAfter(
                                    portContainerPartName,
                                    new IntPart("sg-int", GraphElementModel, this, ussClassName, portReader.LocalID)
                                );
                                break;
                            case GraphType.Primitive.Float:
                                if (uiHints.ContainsKey(portName + SLIDER_HINT))
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
                        if (uiHints.ContainsKey(portName + COLOR_HINT))
                        {
                            PartList.InsertPartAfter(
                                portContainerPartName,
                                new ColorPart("sg-color", Model, this, ussClassName, portReader.LocalID, includeAlpha: false)
                            );
                        }
                        else
                        {
                            PartList.InsertPartAfter(
                                portContainerPartName,
                                new Vector3Part("sg-vector3", GraphElementModel, this, ussClassName, portReader.GetName())
                            );
                        }

                        break;
                    case GraphType.Length.Four:
                        if (uiHints.ContainsKey(portName + COLOR_HINT))
                        {
                            PartList.InsertPartAfter(
                                portContainerPartName,
                                new ColorPart("sg-color", GraphElementModel, this, ussClassName, portReader.GetName(), includeAlpha: true)
                            );
                        }
                        else
                        {
                            PartList.InsertPartAfter(
                                portContainerPartName,
                                new Vector4Part("sg-vector4", GraphElementModel, this, ussClassName, portReader.GetName())
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
