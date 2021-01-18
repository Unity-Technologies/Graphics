using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Rendering;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Node = UnityEditor.Experimental.GraphView.Node;

namespace UnityEditor.ShaderGraph.Drawing
{
    sealed class MaterialNodeView : Node, IShaderNodeView, IInspectable
    {
        PreviewRenderData m_PreviewRenderData;
        Image m_PreviewImage;
        // Remove this after updated to the correct API call has landed in trunk. ------------
        VisualElement m_TitleContainer;
        new VisualElement m_ButtonContainer;

        VisualElement m_PreviewContainer;
        VisualElement m_ControlItems;
        VisualElement m_PreviewFiller;
        VisualElement m_ControlsDivider;
        IEdgeConnectorListener m_ConnectorListener;

        MaterialGraphView m_GraphView;

        public string inspectorTitle => $"{node.name} (Node)";
        public void Initialize(AbstractMaterialNode inNode, PreviewManager previewManager, IEdgeConnectorListener connectorListener, MaterialGraphView graphView)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/MaterialNodeView"));
            styleSheets.Add(Resources.Load<StyleSheet>($"Styles/ColorMode"));
            AddToClassList("MaterialNode");

            if (inNode == null)
                return;

            var contents = this.Q("contents");

            m_GraphView = graphView;
            mainContainer.style.overflow = StyleKeyword.None;    // Override explicit style set in base class
            m_ConnectorListener = connectorListener;
            node = inNode;
            viewDataKey = node.objectId;
            UpdateTitle();

            // Add disabled overlay
            Add(new VisualElement() { name = "disabledOverlay", pickingMode = PickingMode.Ignore });

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
                            SetPreviewExpandedStateOnSelection(false);
                        }));
                    m_PreviewImage.Add(collapsePreviewButton);
                }
                m_PreviewContainer.Add(m_PreviewImage);

                // Hook up preview image to preview manager
                m_PreviewRenderData = previewManager.GetPreviewRenderData(inNode);
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
                            SetPreviewExpandedStateOnSelection(true);
                        }));
                    m_PreviewFiller.Add(expandPreviewButton);
                }
                contents.Add(m_PreviewFiller);

                UpdatePreviewExpandedState(node.previewExpanded);
            }

            base.expanded = node.drawState.expanded;
            AddSlots(node.GetSlots<MaterialSlot>());

            if (node is SubGraphNode)
            {
                RegisterCallback<MouseDownEvent>(OnSubGraphDoubleClick);
            }

            m_TitleContainer = this.Q("title");

            if(node is BlockNode blockData)
            {
                AddToClassList("blockData");
                m_TitleContainer.RemoveFromHierarchy();
            }
            else
            {
                SetPosition(new Rect(node.drawState.position.x, node.drawState.position.y, 0, 0));
            }

            // Update active state
            SetActive(node.isActive);

            // Register OnMouseHover callbacks for node highlighting
            RegisterCallback<MouseEnterEvent>(OnMouseHover);
            RegisterCallback<MouseLeaveEvent>(OnMouseHover);

            ShaderGraphPreferences.onAllowDeprecatedChanged += UpdateTitle;
        }

        public bool FindPort(SlotReference slotRef, out ShaderPort port)
        {
            port = inputContainer.Query<ShaderPort>().ToList()
                .Concat(outputContainer.Query<ShaderPort>().ToList())
                .First(p => p.slot.slotReference.Equals(slotRef));

            return port != null;
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
            badge.AttachTo(m_TitleContainer, SpriteAlignment.RightCenter);
        }

        public void SetActive(bool state)
        {
            // Setup
            var disabledString = "disabled";
            var portDisabledString = "inactive";


            if (!state)
            {
                // Add elements to disabled class list
                AddToClassList(disabledString);

                var inputPorts = inputContainer.Query<ShaderPort>().ToList();
                foreach (var port in inputPorts)
                {
                    port.AddToClassList(portDisabledString);
                }
                var outputPorts = outputContainer.Query<ShaderPort>().ToList();
                foreach (var port in outputPorts)
                {
                    port.AddToClassList(portDisabledString);
                }
            }
            else
            {
                // Remove elements from disabled class list
                RemoveFromClassList(disabledString);

                var inputPorts = inputContainer.Query<ShaderPort>().ToList();
                foreach (var port in inputPorts)
                {
                    port.RemoveFromClassList(portDisabledString);
                }
                var outputPorts = outputContainer.Query<ShaderPort>().ToList();
                foreach (var port in outputPorts)
                {
                    port.RemoveFromClassList(portDisabledString);
                }

            }
        }



        public void ClearMessage()
        {
            var badge = this.Q<IconBadge>();
            badge?.Detach();
            badge?.RemoveFromHierarchy();
        }

        public VisualElement colorElement
        {
            get { return this; }
        }

        static readonly StyleColor noColor = new StyleColor(StyleKeyword.Null);
        public void SetColor(Color color)
        {
            m_TitleContainer.style.borderBottomColor = color;
        }

        public void ResetColor()
        {
            m_TitleContainer.style.borderBottomColor = noColor;
        }


        public Color GetColor()
        {
            return m_TitleContainer.resolvedStyle.borderBottomColor;
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

        public Node gvNode => this;

        [Inspectable("Node", null)]
        public AbstractMaterialNode node { get; private set; }

        public override bool expanded
        {
            get => base.expanded;
            set
            {
                if (base.expanded == value)
                    return;

                base.expanded = value;

                if (node.drawState.expanded != value)
                {
                    var ds = node.drawState;
                    ds.expanded = value;
                    node.drawState = ds;
                }

                foreach (var inputPort in inputContainer.Query<ShaderPort>().ToList())
                {
                    inputPort.parent.style.visibility = inputPort.style.visibility;
                }

                RefreshExpandedState(); // Necessary b/c we can't override enough Node.cs functions to update only what's needed
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is Node)
            {
                var canViewShader = node.hasPreview || node is SubGraphOutputNode;
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
                SanitizeName(node.name), node.objectId, mode == GenerationMode.Preview ? "-Preview" : "");
            if (GraphUtil.WriteToFile(path, ConvertToShader(mode)))
                GraphUtil.OpenFile(path);
        }

        string ConvertToShader(GenerationMode mode)
        {
            var generator = new Generator(node.owner, node, mode, node.name, null);
            return generator.generatedShader;
        }

        void SetNodesAsDirty()
        {
            var editorView = GetFirstAncestorOfType<GraphEditorView>();
            var nodeList = m_GraphView.Query<MaterialNodeView>().ToList();
            editorView.colorManager.SetNodesDirty(nodeList);
        }

        void UpdateNodeViews()
        {
            var editorView = GetFirstAncestorOfType<GraphEditorView>();
            var nodeList = m_GraphView.Query<MaterialNodeView>().ToList();
            editorView.colorManager.UpdateNodeViews(nodeList);
        }

        public object GetObjectToInspect()
        {
            return node;
        }

        public void SupplyDataToPropertyDrawer(IPropertyDrawer propertyDrawer, Action inspectorUpdateDelegate)
        {
            if (propertyDrawer is IGetNodePropertyDrawerPropertyData nodePropertyDrawer)
            {
                nodePropertyDrawer.GetPropertyData(SetNodesAsDirty, UpdateNodeViews);
            }
        }

        private void SetSelfSelected()
        {
            m_GraphView.ClearSelection();
            m_GraphView.AddToSelection(this);
        }

        protected override void ToggleCollapse()
        {
            node.owner.owner.RegisterCompleteObjectUndo(!expanded ? "Expand Nodes" : "Collapse Nodes");
            expanded = !expanded;

            // If selected, expand/collapse the other applicable nodes that are also selected
            if (selected)
            {
                m_GraphView.SetNodeExpandedForSelectedNodes(expanded, false);
            }
        }

        void SetPreviewExpandedStateOnSelection(bool state)
        {
            // If selected, expand/collapse the other applicable nodes that are also selected
            if (selected)
            {
                m_GraphView.SetPreviewExpandedForSelectedNodes(state);
            }
            else
            {
                node.owner.owner.RegisterCompleteObjectUndo(state ? "Expand Previews" : "Collapse Previews");
                node.previewExpanded = state;
            }
        }

        public bool CanToggleNodeExpanded()
        {
            return !(node is BlockNode) && m_CollapseButton.enabledInHierarchy;
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
            if (node is SubGraphNode subGraphNode && subGraphNode.asset != null)
                title = subGraphNode.asset.name;
            else
            {
                if (node.sgVersion < node.latestVersion)
                {
                    if (ShaderGraphPreferences.allowDeprecatedBehaviors)
                    {
                        title = node.name + $" (Deprecated V{node.sgVersion})";
                    }
                    else
                    {
                        title = node.name + $" (Deprecated)";
                    }
                }
                else
                {
                    title = node.name;
                }
            }
        }

        void UpdateShaderPortsForSlots(bool inputSlots, List<MaterialSlot> allSlots, ShaderPort[] slotShaderPorts)
        {
            VisualElement portContainer = inputSlots ? inputContainer : outputContainer;
            var existingPorts = portContainer.Query<ShaderPort>().ToList();
            foreach (ShaderPort shaderPort in existingPorts)
            {
                var currentSlotId = shaderPort.slot.id;
                int newSlotIndex = allSlots.FindIndex(s => s.id == currentSlotId);
                if (newSlotIndex < 0)
                {
                    // slot doesn't exist anymore, remove it
                    if (inputSlots)
                        portContainer.Remove(shaderPort.parent);    // remove parent (includes the InputView)
                    else
                        portContainer.Remove(shaderPort);
                }
                else
                {
                    var newSlot = allSlots[newSlotIndex];
                    slotShaderPorts[newSlotIndex] = shaderPort;

                    // these should probably be in an UpdateShaderPort(shaderPort, newSlot) function
                    shaderPort.slot = newSlot;
                    shaderPort.portName = newSlot.displayName;

                    if (inputSlots) // input slots also have to update the InputView
                        UpdatePortInputView(shaderPort);
                }
            }
        }

        public void OnModified(ModificationScope scope)
        {
            UpdateTitle();
            SetActive(node.isActive);
            if (node.hasPreview)
                UpdatePreviewExpandedState(node.previewExpanded);

            base.expanded = node.drawState.expanded;

            switch (scope)
            {
                // Update slots to match node modification
                case ModificationScope.Topological:
                {
                    var slots = node.GetSlots<MaterialSlot>().ToList();
                    // going to record the corresponding ShaderPort to each slot, so we can order them later
                    ShaderPort[] slotShaderPorts = new ShaderPort[slots.Count];

                    // update existing input and output ports
                    UpdateShaderPortsForSlots(true, slots, slotShaderPorts);
                    UpdateShaderPortsForSlots(false, slots, slotShaderPorts);

                    // check if there are any new slots that must create new ports
                    for (int i = 0; i < slots.Count; i++)
                    {
                        if (slotShaderPorts[i] == null)
                            slotShaderPorts[i] = AddShaderPortForSlot(slots[i]);
                    }

                    // make sure they are in the right order
                    // by bringing each port to front in declaration order
                    // note that this sorts input and output containers at the same time
                    foreach (var shaderPort in slotShaderPorts)
                    {
                        if (shaderPort != null)
                        {
                            if (shaderPort.slot.isInputSlot)
                                shaderPort.parent.BringToFront();
                            else
                                shaderPort.BringToFront();
                        }
                    }

                    break;
                }
            }

            RefreshExpandedState(); // Necessary b/c we can't override enough Node.cs functions to update only what's needed

            foreach (var listener in m_ControlItems.Children().OfType<AbstractMaterialNodeModificationListener>())
            {
                if (listener != null)
                    listener.OnNodeModified(scope);
            }
        }

        ShaderPort AddShaderPortForSlot(MaterialSlot slot)
        {
            if (slot.hidden)
                return null;

            ShaderPort port = ShaderPort.Create(slot, m_ConnectorListener);
            if (slot.isOutputSlot)
            {
                outputContainer.Add(port);
            }
            else
            {
                var portContainer = new VisualElement();
                portContainer.style.flexDirection = FlexDirection.Row;
                var portInputView = new PortInputView(slot) { style = { position = Position.Absolute } };
                portContainer.Add(portInputView);
                portContainer.Add(port);
                inputContainer.Add(portContainer);

                // Update active state
                if (node.isActive)
                {
                    portInputView.RemoveFromClassList("disabled");
                }
                else
                {
                    portInputView.AddToClassList("disabled");
                }
            }
            port.OnDisconnect = OnEdgeDisconnected;

            return port;
        }

        void AddSlots(IEnumerable<MaterialSlot> slots)
        {
            foreach (var slot in slots)
                AddShaderPortForSlot(slot);
        }

        void OnEdgeDisconnected(Port obj)
        {
            RefreshExpandedState();
        }

        static bool GetPortInputView(ShaderPort port, out PortInputView view)
        {
            view = port.parent.Q<PortInputView>();
            return view != null;
        }

        public void UpdatePortInputTypes()
        {
            var portList = inputContainer.Query<ShaderPort>().ToList();
            portList.AddRange(outputContainer.Query<ShaderPort>().ToList());
            foreach (var anchor in portList)
            {
                var slot = anchor.slot;
                anchor.portName = slot.displayName;
                anchor.visualClass = slot.concreteValueType.ToClassName();

                if (GetPortInputView(anchor, out var portInputView))
                {
                    portInputView.UpdateSlotType();
                    UpdatePortInputVisibility(portInputView, anchor);
                }
            }

            foreach (var control in m_ControlItems.Children())
            {
                if (control is AbstractMaterialNodeModificationListener listener)
                    listener.OnNodeModified(ModificationScope.Graph);
            }
        }

        void UpdatePortInputView(ShaderPort port)
        {
            if (GetPortInputView(port, out var portInputView))
            {
                portInputView.UpdateSlot(port.slot);
                UpdatePortInputVisibility(portInputView, port);
            }
        }

        void UpdatePortInputVisibility(PortInputView portInputView, ShaderPort port)
        {
            SetElementVisible(portInputView, !port.slot.isConnected);
            port.parent.style.visibility = port.style.visibility;
            portInputView.MarkDirtyRepaint();
        }

        void SetElementVisible(VisualElement element, bool isVisible)
        {
            const string k_HiddenClassList = "hidden";

            if (isVisible)
            {
                // Restore default value for visibility by setting it to StyleKeyword.Null.
                // Setting it to Visibility.Visible would make it visible even if parent is hidden.
                element.style.visibility = StyleKeyword.Null;
                element.RemoveFromClassList(k_HiddenClassList);
            }
            else
            {
                element.style.visibility = Visibility.Hidden;
                element.AddToClassList(k_HiddenClassList);
            }
        }

        void OnMouseHover(EventBase evt)
        {
            var graphEditorView = GetFirstAncestorOfType<GraphEditorView>();
            if (graphEditorView == null)
                return;

            var blackboardProvider = graphEditorView.blackboardProvider;
            if (blackboardProvider == null)
                return;

            // Keyword nodes should be highlighted when Blackboard entry is hovered
            // TODO: Move to new NodeView type when keyword node has unique style
            if(node is KeywordNode keywordNode)
            {
                var keywordRow = blackboardProvider.GetBlackboardRow(keywordNode.keyword);
                if (keywordRow != null)
                {
                    if (evt.eventTypeId == MouseEnterEvent.TypeId())
                    {
                        keywordRow.AddToClassList("hovered");
                    }
                    else
                    {
                        keywordRow.RemoveFromClassList("hovered");
                    }
                }
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

                if (m_PreviewRenderData.shaderData.isOutOfDate)
                    m_PreviewImage.tintColor = new Color(1.0f, 1.0f, 1.0f, 0.3f);
                else
                    m_PreviewImage.tintColor = Color.white;
            }
        }

        public void Dispose()
        {
            foreach (var portInputView in inputContainer.Query<PortInputView>().ToList())
                portInputView.Dispose();

            node = null;
            userData = null;
            if (m_PreviewRenderData != null)
            {
                m_PreviewRenderData.onPreviewChanged -= UpdatePreviewTexture;
                m_PreviewRenderData = null;
            }
            ShaderGraphPreferences.onAllowDeprecatedChanged -= UpdateTitle;
        }
    }
}
