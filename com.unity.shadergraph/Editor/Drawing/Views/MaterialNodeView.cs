using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Rendering;

using UnityEditor.Experimental.GraphView;
using UnityEditor.Rendering;
using UnityEngine.UIElements;
using Node = UnityEditor.Experimental.GraphView.Node;

namespace UnityEditor.ShaderGraph.Drawing
{
    sealed class MaterialNodeView : Node
    {
        PreviewRenderData m_PreviewRenderData;
        Image m_PreviewImage;
        VisualElement m_PreviewContainer;
        VisualElement m_ControlItems;
        VisualElement m_PreviewFiller;
        VisualElement m_ControlsDivider;
        IEdgeConnectorListener m_ConnectorListener;
        VisualElement m_PortInputContainer;
        VisualElement m_SettingsContainer;
        bool m_ShowSettings = false;
        VisualElement m_SettingsButton;
        VisualElement m_Settings;
        VisualElement m_NodeSettingsView;


        public void Initialize(AbstractMaterialNode inNode, PreviewManager previewManager, IEdgeConnectorListener connectorListener)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/MaterialNodeView"));
            AddToClassList("MaterialNode");

            if (inNode == null)
                return;

            var contents = this.Q("contents");

            m_ConnectorListener = connectorListener;
            node = inNode;
            viewDataKey = node.guid.ToString();
            UpdateTitle();

            // Add controls container
            var controlsContainer = new VisualElement { name = "controls" };
            {
                m_ControlsDivider = new VisualElement { name = "divider" };
                m_ControlsDivider.AddToClassList("horizontal");
                controlsContainer.Add(m_ControlsDivider);
                m_ControlItems = new VisualElement { name = "items" };
                controlsContainer.Add(m_ControlItems);

                // Instantiate control views from node
                foreach (var propertyInfo in node.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    foreach (IControlAttribute attribute in propertyInfo.GetCustomAttributes(typeof(IControlAttribute), false))
                        m_ControlItems.Add(attribute.InstantiateControl(node, propertyInfo));
            }
            if (m_ControlItems.childCount > 0)
                contents.Add(controlsContainer);

            if (node.hasPreview)
            {
                // Add actual preview which floats on top of the node
                m_PreviewContainer = new VisualElement
                {
                    name = "previewContainer",
                    cacheAsBitmap = true,
                    style = { overflow = Overflow.Hidden },
                    pickingMode = PickingMode.Ignore
                };
                m_PreviewImage = new Image
                {
                    name = "preview",
                    pickingMode = PickingMode.Ignore,
                    image = Texture2D.whiteTexture,
                };
                {
                    // Add preview collapse button on top of preview
                    var collapsePreviewButton = new VisualElement { name = "collapse" };
                    collapsePreviewButton.Add(new VisualElement { name = "icon" });
                    collapsePreviewButton.AddManipulator(new Clickable(() =>
                        {
                            node.owner.owner.RegisterCompleteObjectUndo("Collapse Preview");
                            UpdatePreviewExpandedState(false);
                        }));
                    m_PreviewImage.Add(collapsePreviewButton);
                }
                m_PreviewContainer.Add(m_PreviewImage);

                // Hook up preview image to preview manager
                m_PreviewRenderData = previewManager.GetPreview(inNode);
                m_PreviewRenderData.onPreviewChanged += UpdatePreviewTexture;
                UpdatePreviewTexture();

                // Add fake preview which pads out the node to provide space for the floating preview
                m_PreviewFiller = new VisualElement { name = "previewFiller" };
                m_PreviewFiller.AddToClassList("expanded");
                {
                    var previewDivider = new VisualElement { name = "divider" };
                    previewDivider.AddToClassList("horizontal");
                    m_PreviewFiller.Add(previewDivider);

                    var expandPreviewButton = new VisualElement { name = "expand" };
                    expandPreviewButton.Add(new VisualElement { name = "icon" });
                    expandPreviewButton.AddManipulator(new Clickable(() =>
                        {
                            node.owner.owner.RegisterCompleteObjectUndo("Expand Preview");
                            UpdatePreviewExpandedState(true);
                        }));
                    m_PreviewFiller.Add(expandPreviewButton);
                }
                contents.Add(m_PreviewFiller);

                UpdatePreviewExpandedState(node.previewExpanded);
            }

            // Add port input container, which acts as a pixel cache for all port inputs
            m_PortInputContainer = new VisualElement
            {
                name = "portInputContainer",
                cacheAsBitmap = true,
                style = { overflow = Overflow.Hidden },
                pickingMode = PickingMode.Ignore
            };
            Add(m_PortInputContainer);

            AddSlots(node.GetSlots<MaterialSlot>());
            UpdatePortInputs();
            base.expanded = node.drawState.expanded;
            RefreshExpandedState(); //This should not be needed. GraphView needs to improve the extension api here
            UpdatePortInputVisibilities();

            SetPosition(new Rect(node.drawState.position.x, node.drawState.position.y, 0, 0));

            if (node is SubGraphNode)
            {
                RegisterCallback<MouseDownEvent>(OnSubGraphDoubleClick);
            }

            if (node is PropertyNode)
            {
                RegisterCallback<MouseEnterEvent>(OnMouseHover);
                RegisterCallback<MouseLeaveEvent>(OnMouseHover);
            }

            var masterNode = node as IMasterNode;
            if (masterNode != null)
            {
                AddToClassList("master");

                if (!masterNode.IsPipelineCompatible(GraphicsSettings.renderPipelineAsset))
                {
                    AttachMessage("The current render pipeline is not compatible with this master node.", ShaderCompilerMessageSeverity.Error);
                }
            }

            m_PortInputContainer.SendToBack();

            // Remove this after updated to the correct API call has landed in trunk. ------------
            VisualElement m_TitleContainer;
            VisualElement m_ButtonContainer;
            m_TitleContainer = this.Q("title");
            // -----------------------------------------------------------------------------------

            var settings = node as IHasSettings;
            if (settings != null)
            {
                m_NodeSettingsView = new NodeSettingsView();
                m_NodeSettingsView.visible = false;

                Add(m_NodeSettingsView);

                m_SettingsButton = new VisualElement {name = "settings-button"};
                m_SettingsButton.Add(new VisualElement { name = "icon" });

                m_Settings = settings.CreateSettingsElement();

                m_SettingsButton.AddManipulator(new Clickable(() =>
                    {
                        UpdateSettingsExpandedState();
                    }));

                // Remove this after updated to the correct API call has landed in trunk. ------------
                m_ButtonContainer = new VisualElement { name = "button-container" };
                m_ButtonContainer.style.flexDirection = FlexDirection.Row;
                m_ButtonContainer.Add(m_SettingsButton);
                m_ButtonContainer.Add(m_CollapseButton);
                m_TitleContainer.Add(m_ButtonContainer);
                // -----------------------------------------------------------------------------------
                //titleButtonContainer.Add(m_SettingsButton);
                //titleButtonContainer.Add(m_CollapseButton);
            }
        }

        public void AttachMessage(string errString, ShaderCompilerMessageSeverity severity)
        {
            ClearMessage();
            IconBadge badge;
            if (severity == ShaderCompilerMessageSeverity.Error)
            {
                badge = IconBadge.CreateError(errString);
            }
            else
            {
                badge = IconBadge.CreateComment(errString);
            }

            Add(badge);
            var myTitle = this.Q("title");
            badge.AttachTo(myTitle, SpriteAlignment.RightCenter);
        }

        public void ClearMessage()
        {
            var badge = this.Q<IconBadge>();
            if(badge != null)
            {
                badge.Detach();
                badge.RemoveFromHierarchy();
            }
        }

        void OnMouseHover(EventBase evt)
        {
            var graphView = GetFirstAncestorOfType<GraphEditorView>();
            if (graphView == null)
                return;

            var blackboardProvider = graphView.blackboardProvider;
            if (blackboardProvider == null)
                return;

            var propNode = (PropertyNode)node;

            var propRow = blackboardProvider.GetBlackboardRow(propNode.propertyGuid);
            if (propRow != null)
            {
                if (evt.eventTypeId == MouseEnterEvent.TypeId())
                {
                    propRow.AddToClassList("hovered");
                }
                else
                {
                    propRow.RemoveFromClassList("hovered");
                }
            }
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // style.positionTop and style.positionLeft are in relation to the parent,
            // so we translate the layout of the settings button to be in the coordinate
            // space of the settings view's parent.

            var settingsButtonLayout = m_SettingsButton.ChangeCoordinatesTo(m_NodeSettingsView.parent, m_SettingsButton.layout);
            m_NodeSettingsView.style.top = settingsButtonLayout.yMax - 18f;
            m_NodeSettingsView.style.left = settingsButtonLayout.xMin - 16f;
        }

        void OnSubGraphDoubleClick(MouseDownEvent evt)
        {
            if (evt.clickCount == 2 && evt.button == 0)
            {
                SubGraphNode subgraphNode = node as SubGraphNode;

                var path = AssetDatabase.GUIDToAssetPath(subgraphNode.subGraphGuid);
                ShaderGraphImporterEditor.ShowGraphEditWindow(path);
            }
        }

        public AbstractMaterialNode node { get; private set; }

        public override bool expanded
        {
            get { return base.expanded; }
            set
            {
                if (base.expanded != value)
                    base.expanded = value;

                if (node.drawState.expanded != value)
                {
                    var ds = node.drawState;
                    ds.expanded = value;
                    node.drawState = ds;
                }

                RefreshExpandedState(); //This should not be needed. GraphView needs to improve the extension api here
                UpdatePortInputVisibilities();
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is Node)
            {
                var canViewShader = node.hasPreview || node is IMasterNode;
                evt.menu.AppendAction("Copy Shader", CopyToClipboard,
                    _ => canViewShader ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Hidden,
                    GenerationMode.ForReals);
                evt.menu.AppendAction("Show Generated Code", ShowGeneratedCode,
                    _ => canViewShader ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Hidden,
                    GenerationMode.ForReals);
                
                if (Unsupported.IsDeveloperMode())
                {
                    evt.menu.AppendAction("Show Preview Code", ShowGeneratedCode,
                        _ => canViewShader ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Hidden,
                        GenerationMode.Preview);
                }
            }

            base.BuildContextualMenu(evt);
        }

        void CopyToClipboard(DropdownMenuAction action)
        {
            GUIUtility.systemCopyBuffer = ConvertToShader((GenerationMode) action.userData);
        }

        public string SanitizeName(string name)
        {
            return new string(name.Where(c => !Char.IsWhiteSpace(c)).ToArray());
        }

        public void ShowGeneratedCode(DropdownMenuAction action)
        {
            string name = GetFirstAncestorOfType<GraphEditorView>().assetName;
            var mode = (GenerationMode)action.userData;

            string path = String.Format("Temp/GeneratedFromGraph-{0}-{1}-{2}{3}.shader", SanitizeName(name),
                SanitizeName(node.name), node.guid, mode == GenerationMode.Preview ? "-Preview" : "");
            if (GraphUtil.WriteToFile(path, ConvertToShader(mode)))
                GraphUtil.OpenFile(path);
        }

        string ConvertToShader(GenerationMode mode)
        {
            List<PropertyCollector.TextureInfo> textureInfo;
            if (node is IMasterNode masterNode)
                return masterNode.GetShader(mode, node.name, out textureInfo);

            return node.owner.GetShader(node, mode, node.name).shader;
        }

        void RecreateSettings()
        {
            var settings = node as IHasSettings;
            if (settings != null)
            {
                m_Settings.RemoveFromHierarchy();

                m_Settings = settings.CreateSettingsElement();
                m_NodeSettingsView.Add(m_Settings);
            }
        }

        void UpdateSettingsExpandedState()
        {
            m_ShowSettings = !m_ShowSettings;
            if (m_ShowSettings)
            {
                m_NodeSettingsView.Add(m_Settings);
                m_NodeSettingsView.visible = true;

                m_SettingsButton.AddToClassList("clicked");
                RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                OnGeometryChanged(null);
            }
            else
            {
                m_Settings.RemoveFromHierarchy();

                m_NodeSettingsView.visible = false;
                m_SettingsButton.RemoveFromClassList("clicked");
                UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }

        void UpdatePreviewExpandedState(bool expanded)
        {
            node.previewExpanded = expanded;
            if (m_PreviewFiller == null)
                return;
            if (expanded)
            {
                if (m_PreviewContainer.parent != this)
                {
                    Add(m_PreviewContainer);
                    m_PreviewContainer.PlaceBehind(this.Q("selection-border"));
                }
                m_PreviewFiller.AddToClassList("expanded");
                m_PreviewFiller.RemoveFromClassList("collapsed");
            }
            else
            {
                if (m_PreviewContainer.parent == m_PreviewFiller)
                {
                    m_PreviewContainer.RemoveFromHierarchy();
                }
                m_PreviewFiller.RemoveFromClassList("expanded");
                m_PreviewFiller.AddToClassList("collapsed");
            }
            UpdatePreviewTexture();
        }

        void UpdateTitle()
        {
            if (node is SubGraphNode subGraphNode && subGraphNode.subGraphData != null)
                title = subGraphNode.subGraphAsset.name;
            else
                title = node.name;
        }

        public void OnModified(ModificationScope scope)
        {
            UpdateTitle();
            if (node.hasPreview)
                UpdatePreviewExpandedState(node.previewExpanded);

            base.expanded = node.drawState.expanded;

            // Update slots to match node modification
            if (scope == ModificationScope.Topological)
            {
                RecreateSettings();

                var slots = node.GetSlots<MaterialSlot>().ToList();

                var inputPorts = inputContainer.Children().OfType<ShaderPort>().ToList();
                foreach (var port in inputPorts)
                {
                    var currentSlot = port.slot;
                    var newSlot = slots.FirstOrDefault(s => s.id == currentSlot.id);
                    if (newSlot == null)
                    {
                        // Slot doesn't exist anymore, remove it
                        inputContainer.Remove(port);

                        // We also need to remove the inline input
                        var portInputView = m_PortInputContainer.Children().OfType<PortInputView>().FirstOrDefault(v => Equals(v.slot, port.slot));
                        if (portInputView != null)
                            portInputView.RemoveFromHierarchy();
                    }
                    else
                    {
                        port.slot = newSlot;
                        var portInputView = m_PortInputContainer.Children().OfType<PortInputView>().FirstOrDefault(x => x.slot.id == currentSlot.id);
                        portInputView.UpdateSlot(newSlot);

                        slots.Remove(newSlot);
                    }
                }

                var outputPorts = outputContainer.Children().OfType<ShaderPort>().ToList();
                foreach (var port in outputPorts)
                {
                    var currentSlot = port.slot;
                    var newSlot = slots.FirstOrDefault(s => s.id == currentSlot.id);
                    if (newSlot == null)
                    {
                        outputContainer.Remove(port);
                    }
                    else
                    {
                        port.slot = newSlot;
                        slots.Remove(newSlot);
                    }
                }

                AddSlots(slots);

                slots.Clear();
                slots.AddRange(node.GetSlots<MaterialSlot>());

                if (inputContainer.childCount > 0)
                    inputContainer.Sort((x, y) => slots.IndexOf(((ShaderPort)x).slot) - slots.IndexOf(((ShaderPort)y).slot));
                if (outputContainer.childCount > 0)
                    outputContainer.Sort((x, y) => slots.IndexOf(((ShaderPort)x).slot) - slots.IndexOf(((ShaderPort)y).slot));

                UpdatePortInputs();
                UpdatePortInputVisibilities();
            }

            RefreshExpandedState(); //This should not be needed. GraphView needs to improve the extension api here

            foreach (var listener in m_ControlItems.Children().OfType<AbstractMaterialNodeModificationListener>())
            {
                if (listener != null)
                    listener.OnNodeModified(scope);
            }
        }

        void AddSlots(IEnumerable<MaterialSlot> slots)
        {
            foreach (var slot in slots)
            {
                if (slot.hidden)
                    continue;

                var port = ShaderPort.Create(slot, m_ConnectorListener);
                if (slot.isOutputSlot)
                    outputContainer.Add(port);
                else
                    inputContainer.Add(port);
            }
        }

        void UpdatePortInputs()
        {
            foreach (var port in inputContainer.Children().OfType<ShaderPort>())
            {
                if (!m_PortInputContainer.Children().OfType<PortInputView>().Any(a => Equals(a.slot, port.slot)))
                {
                    var portInputView = new PortInputView(port.slot) { style = { position = Position.Absolute } };
                    m_PortInputContainer.Add(portInputView);
                    if (float.IsNaN(port.layout.width))
                    {
                        port.RegisterCallback<GeometryChangedEvent>(UpdatePortInput);
                    }
                    else
                    {
                        SetPortInputPosition(port, portInputView);
                    }
                }
            }
        }

        void UpdatePortInput(GeometryChangedEvent evt)
        {
            var port = (ShaderPort)evt.target;
            var inputView = m_PortInputContainer.Children().OfType<PortInputView>().First(x => Equals(x.slot, port.slot));
            SetPortInputPosition(port, inputView);
            port.UnregisterCallback<GeometryChangedEvent>(UpdatePortInput);
        }

        void SetPortInputPosition(ShaderPort port, PortInputView inputView)
        {
            inputView.style.top = port.layout.y;
            inputView.parent.style.height = inputContainer.layout.height;
        }

        public void UpdatePortInputVisibilities()
        {
            foreach (var portInputView in m_PortInputContainer.Children().OfType<PortInputView>().ToList())
            {
                var slot = portInputView.slot;
                portInputView.style.display = expanded && !node.owner.GetEdges(node.GetSlotReference(slot.id)).Any() ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void UpdatePortInputTypes()
        {
            foreach (var anchor in inputContainer.Children().Concat(outputContainer.Children()).OfType<ShaderPort>())
            {
                var slot = anchor.slot;
                anchor.portName = slot.displayName;
                anchor.visualClass = slot.concreteValueType.ToClassName();
            }

            foreach (var portInputView in m_PortInputContainer.Children().OfType<PortInputView>())
                portInputView.UpdateSlotType();

            foreach (var control in m_ControlItems.Children())
            {
                var listener = control as AbstractMaterialNodeModificationListener;
                if (listener != null)
                    listener.OnNodeModified(ModificationScope.Graph);
            }
        }

        void OnResize(Vector2 deltaSize)
        {
            var updatedWidth = topContainer.layout.width + deltaSize.x;
            var updatedHeight = m_PreviewImage.layout.height + deltaSize.y;

            var previewNode = node as PreviewNode;
            if (previewNode != null)
            {
                previewNode.SetDimensions(updatedWidth, updatedHeight);
                UpdateSize();
            }
        }

        void UpdatePreviewTexture()
        {
            if (m_PreviewRenderData.texture == null || !node.previewExpanded)
            {
                m_PreviewImage.visible = false;
                m_PreviewImage.image = Texture2D.blackTexture;
            }
            else
            {
                m_PreviewImage.visible = true;
                m_PreviewImage.AddToClassList("visible");
                m_PreviewImage.RemoveFromClassList("hidden");
                if (m_PreviewImage.image != m_PreviewRenderData.texture)
                    m_PreviewImage.image = m_PreviewRenderData.texture;
                else
                    m_PreviewImage.MarkDirtyRepaint();

                if (m_PreviewRenderData.shaderData.isCompiling)
                    m_PreviewImage.tintColor = new Color(1.0f, 1.0f, 1.0f, 0.3f);
                else
                    m_PreviewImage.tintColor = Color.white;
            }
        }

        void UpdateSize()
        {
            var previewNode = node as PreviewNode;

            if (previewNode == null)
                return;

            var width = previewNode.width;
            var height = previewNode.height;

            m_PreviewImage.style.height = height;
            m_PreviewImage.style.width = width;
        }

        public void Dispose()
        {
            foreach (var portInputView in m_PortInputContainer.Children().OfType<PortInputView>())
                portInputView.Dispose();

            node = null;
            userData = null;
            if (m_PreviewRenderData != null)
            {
                m_PreviewRenderData.onPreviewChanged -= UpdatePreviewTexture;
                m_PreviewRenderData = null;
            }
        }
    }
}
