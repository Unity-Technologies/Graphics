using System;
using Debug = UnityEngine.Debug;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class SGNodeView : CollapsibleInOutNode
    {
        NodePreviewPart m_NodePreviewPart;
        DynamicPartHolder m_StaticFieldParts;
        SGNodeModel graphDataNodeModel => NodeModel as SGNodeModel;

        public SGNodeView()
        {
            AddToClassList("sg-node-view");
        }

        protected override void BuildPartList()
        {
            base.BuildPartList();

            // Retrieve this node's view model
            var nodeViewModel = graphDataNodeModel.GetViewModel();
            if (nodeViewModel.Name == null)
                return;

            m_StaticFieldParts = new(name, graphDataNodeModel, this, ussClassName);
            foreach (var portUIData in nodeViewModel.StaticPortUIData)
            {
                if (portUIData.IsGradient)
                {
                    var gradientPart = new GradientPart(
                        "sg-gradient",
                        GraphElementModel,
                        this,
                        ussClassName,
                        portUIData.Name,
                        portUIData.DisplayName);
                    m_StaticFieldParts.PartList.InsertPartAfter(
                        portContainerPartName,
                        gradientPart);
                    continue;
                }

                var part = ResolvePortType(portUIData);
                if (part != null)
                {
                    PartList.InsertPartAfter(portContainerPartName, part);
                }
            }

            // TODO: There should probably be a better way to assign "special" node parts like this.
            var nodeName = graphDataNodeModel.registryKey.Name;
            if (nodeName == "Swizzle")
            {
                PartList.InsertPartAfter(portContainerPartName, new SwizzleMaskPart("sg-swizzle-mask", GraphElementModel, this, ussClassName));
            }

            if (nodeName == "Transform")
            {
                PartList.InsertPartAfter(portContainerPartName,
                    new TransformDropdownsPart("sg-transform-dropdowns",
                        GraphElementModel,
                        this,
                        ussClassName));
            }

            if (nodeName == "ChannelMixer")
            {
                PartList.InsertPartAfter(portContainerPartName,
                    new ChannelMixerPart("sg-channel-mixer-container",
                        GraphElementModel,
                        this,
                        ussClassName));
            }

            // By default we assume all nodes should display previews, unless there
            // is a UIHint that dictates otherwise
            bool nodeHasPreview = nodeViewModel.HasPreview;

            var shouldShowPreview =
                graphDataNodeModel.graphDataOwner.existsInGraphData &&
                nodeHasPreview;

            if (shouldShowPreview)
            {
                m_NodePreviewPart = new NodePreviewPart(
                    "node-preview",
                    GraphElementModel,
                    this,
                    ussClassName);
                PartList.AppendPart(m_NodePreviewPart);

                PartList.RemovePart(nodeCachePartName);

                PartList.AppendPart( SGNodeCachePart.Create(nodeCachePartName,Model,this,ussClassName));
            }
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendSeparator();

            // Disable the expand/collapse option if the preview is already expanded/collapsed respectively
            evt.menu.AppendAction("Preview/Expand", action =>
            {
                GraphView.Dispatch(new ChangePreviewExpandedCommand(true, GraphView.GetSelection() ));
            },
                graphDataNodeModel.IsPreviewExpanded ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

            evt.menu.AppendAction("Preview/Collapse", action =>
            {
                GraphView.Dispatch(new ChangePreviewExpandedCommand(false, GraphView.GetSelection() ));
            },
                graphDataNodeModel.IsPreviewExpanded ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

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

        // Figure out the correct part to display based on the port type.
        ModelViewPart ResolvePortType(SGPortViewModel portViewModel)
        {
            if (portViewModel.Options is {Count: > 0})
            {
                return new StaticPortOptionsPart("sg-dropdown", GraphElementModel, this, ussClassName, portViewModel.Name, portViewModel.DisplayName);
            }

            if (portViewModel.IsMatrix)
            {
                return new MatrixPart(
                    "sg-matrix",
                    GraphElementModel,
                    this,
                    ussClassName,
                    portViewModel.Name,
                    portViewModel.DisplayName,
                    portViewModel.MatrixHeight);
            }

            switch (portViewModel.ComponentLength)
            {
                case ComponentLength.One:
                {
                    switch (portViewModel.NumericType)
                    {
                        case NumericType.Bool:
                            return new BoolPart(
                                "sg-bool",
                                GraphElementModel,
                                this,
                                ussClassName,
                                portViewModel.Name,
                                portViewModel.DisplayName);
                        case NumericType.Int:
                            return new IntPart(
                                "sg-int",
                                GraphElementModel,
                                this,
                                ussClassName,
                                portViewModel.Name,
                                portViewModel.DisplayName);
                        case NumericType.Float:
                            if (portViewModel.UseSlider)
                            {
                                return new SliderPart(
                                    "sg-slider",
                                    GraphElementModel,
                                    this,
                                    ussClassName,
                                    portViewModel.Name,
                                    portViewModel.DisplayName);
                            }
                            else
                            {
                                return new FloatPart(
                                    "sg-float",
                                    GraphElementModel,
                                    this,
                                    ussClassName,
                                    portViewModel.Name,
                                    portViewModel.DisplayName);
                            }
                        case NumericType.Unknown:
                        // Not valid, the type should've been resolved.
                        default:
                            break;
                    }
                    break;
                }
                case ComponentLength.Two:
                    return new Vector2Part(
                        "sg-vector2",
                        GraphElementModel,
                        this,
                        ussClassName,
                        portViewModel.Name,
                        portViewModel.DisplayName);
                case ComponentLength.Three:
                    if (portViewModel.UseColor)
                    {
                        return new ColorPart(
                            "sg-color",
                            GraphElementModel,
                            this,
                            ussClassName,
                            portViewModel.Name,
                            portViewModel.DisplayName,
                            includeAlpha: false,
                            isHdr: portViewModel.IsHdr);
                    }
                    else
                    {
                        return new Vector3Part(
                            "sg-vector3",
                            GraphElementModel,
                            this,
                            ussClassName,
                            portViewModel.Name,
                            portViewModel.DisplayName);
                    }
                case ComponentLength.Four:
                    if (portViewModel.UseColor)
                    {
                        return new ColorPart(
                            "sg-color",
                            GraphElementModel,
                            this,
                            ussClassName,
                            portViewModel.Name,
                            portViewModel.DisplayName,
                            includeAlpha: true,
                            isHdr: portViewModel.IsHdr);
                    }
                    else
                    {
                        return new Vector4Part(
                            "sg-vector4",
                            GraphElementModel,
                            this,
                            ussClassName,
                            portViewModel.Name,
                            portViewModel.DisplayName);
                    }
                case ComponentLength.Unknown:
                // Not valid, the size should've been resolved.
                default:
                    break;
            }
            return null;
        }
    }
}
