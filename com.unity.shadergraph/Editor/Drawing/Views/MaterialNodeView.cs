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
using UnityEditor.ShaderGraph.Drawing.Colors;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Node = UnityEditor.Experimental.GraphView.Node;

namespace UnityEditor.ShaderGraph.Drawing
{
    sealed class MaterialNodeView : Node, IShaderNodeView
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
        VisualElement m_PortInputContainer;
        VisualElement m_SettingsContainer;
        bool m_ShowSettings = false;
        VisualElement m_SettingsButton;
        VisualElement m_Settings;
        VisualElement m_NodeSettingsView;

        GraphView m_GraphView;

        public void Initialize(AbstractMaterialNode inNode, PreviewManager previewManager, IEdgeConnectorListener connectorListener, GraphView graphView)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/MaterialNodeView"));
            styleSheets.Add(Resources.Load<StyleSheet>($"Styles/ColorMode"));
            AddToClassList("MaterialNode");

            if (inNode == null)
                return;

            var contents = this.Q("contents");

            m_GraphView = graphView;

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

            // Node Base class toggles the 'expanded' variable already, this is on top of that call
            m_CollapseButton.RegisterCallback<MouseUpEvent>(SetNodeExpandedStateOnSelection);

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
                            node.owner.owner.RegisterCompleteObjectUndo("Collapse Preview");
                            SetPreviewExpandedStateOnSelection(false);
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
                            SetPreviewExpandedStateOnSelection(true);
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

            m_PortInputContainer.SendToBack();

            m_TitleContainer = this.Q("title");

            var masterNode = node as IMasterNode;
            if (masterNode != null)
            {
                AddToClassList("master");

                (m_GraphView as MaterialGraphView)?.graph.messageManager?.ClearAllFromProvider(this);
                if (!masterNode.IsPipelineCompatible(GraphicsSettings.renderPipelineAsset))
                {
                    (m_GraphView as MaterialGraphView)?.graph.messageManager?.AddOrAppendError(this, node.tempId,
                        new ShaderMessage("The active Master Node is not compatible with the current Render Pipeline," +
                                          " or no Render Pipeline is assigned." +
                                          " Assign a Render Pipeline in the graphics settings that is compatible with this Master Node.",
                            ShaderCompilerMessageSeverity.Error));
                }

            }

            m_NodeSettingsView = new NodeSettingsView();
            m_NodeSettingsView.visible = false;
            Add(m_NodeSettingsView);

            m_SettingsButton = new VisualElement {name = "settings-button"};
            m_SettingsButton.Add(new VisualElement { name = "icon" });

            m_Settings = new VisualElement();
            AddDefaultSettings();

            // Add Node type specific settings
            var nodeTypeSettings = node as IHasSettings;
            if (nodeTypeSettings != null)
                m_Settings.Add(nodeTypeSettings.CreateSettingsElement());

            // Add manipulators
            m_SettingsButton.AddManipulator(new Clickable(() =>
                {
                    UpdateSettingsExpandedState();
                }));

            if(m_Settings.childCount > 0)
            {
                m_ButtonContainer = new VisualElement { name = "button-container" };
                m_ButtonContainer.style.flexDirection = FlexDirection.Row;
                m_ButtonContainer.Add(m_SettingsButton);
                m_ButtonContainer.Add(m_CollapseButton);
                m_TitleContainer.Add(m_ButtonContainer);
            }

            // Register OnMouseHover callbacks for node highlighting
            RegisterCallback<MouseEnterEvent>(OnMouseHover);
            RegisterCallback<MouseLeaveEvent>(OnMouseHover);
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

        public Node gvNode => this;
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
                var isMaster = node is IMasterNode;
                var isActive = node.guid == node.owner.activeOutputNodeGuid;
                if (isMaster)
                {
                    evt.menu.AppendAction("Set Active", SetMasterAsActive,
                        _ => isActive ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                }

                var canViewShader = node.hasPreview || node is IMasterNode || node is SubGraphOutputNode;
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

        void SetMasterAsActive(DropdownMenuAction action)
        {
            node.owner.owner.RegisterCompleteObjectUndo("Change Active Master");
            node.owner.activeOutputNodeGuid = node.guid;
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

        void AddDefaultSettings()
        {
            PropertySheet ps = new PropertySheet();
            bool hasDefaultSettings = false;

            if(node.canSetPrecision)
            {
                hasDefaultSettings = true;
                ps.Add(new PropertyRow(new Label("Precision")), (row) =>
                {
                    row.Add(new EnumField(node.precision), (field) =>
                    {
                        field.RegisterValueChangedCallback(evt =>
                        {
                            if (evt.newValue.Equals(node.precision))
                                return;

                            var editorView = GetFirstAncestorOfType<GraphEditorView>();
                            var nodeList = m_GraphView.Query<MaterialNodeView>().ToList();

                            editorView.colorManager.SetNodesDirty(nodeList);
                            node.owner.owner.RegisterCompleteObjectUndo("Change precision");
                            node.precision = (Precision)evt.newValue;
                            node.owner.ValidateGraph();
                            editorView.colorManager.UpdateNodeViews(nodeList);
                            node.Dirty(ModificationScope.Graph);
                        });
                    });
                });
            }

            if(hasDefaultSettings)
                m_Settings.Add(ps);
        }

        void RecreateSettings()
        {
            m_Settings.RemoveFromHierarchy();
            m_Settings = new PropertySheet();

            // Add default settings
            AddDefaultSettings();

            // Add Node type specific settings
            var nodeTypeSettings = node as IHasSettings;
            if (nodeTypeSettings != null)
                m_Settings.Add(nodeTypeSettings.CreateSettingsElement());

            m_NodeSettingsView.Add(m_Settings);
        }

        void UpdateSettingsExpandedState()
        {
            m_ShowSettings = !m_ShowSettings;
            if (m_ShowSettings)
            {
                m_NodeSettingsView.Add(m_Settings);
                m_NodeSettingsView.visible = true;
                SetSelfSelected();
                m_SettingsButton.AddToClassList("clicked");
                RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                OnGeometryChanged(null);
            }
            else
            {
                m_Settings.RemoveFromHierarchy();
                SetSelfSelected();
                m_NodeSettingsView.visible = false;
                m_SettingsButton.RemoveFromClassList("clicked");
                UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }


        private void SetSelfSelected()
        {
            m_GraphView.ClearSelection();
            m_GraphView.AddToSelection(this);
        }

        void SetNodeExpandedStateOnSelection(MouseUpEvent evt)
        {
            if (!selected)
                SetSelfSelected();
            else
            {
                if (m_GraphView is MaterialGraphView)
                {
                    var matGraphView = m_GraphView as MaterialGraphView;
                    matGraphView.SetNodeExpandedOnSelection(expanded);
                }
            }
        }

        void SetPreviewExpandedStateOnSelection(bool state)
        {
            if (!selected)
            {
                SetSelfSelected();
                UpdatePreviewExpandedState(state);
            }
            else
            {
                if(m_GraphView is MaterialGraphView)
                {
                    var matGraphView = m_GraphView as MaterialGraphView;
                    matGraphView.SetPreviewExpandedOnSelection(state);
                }
            }
        }

        public bool CanToggleExpanded()
        {
            return m_CollapseButton.enabledInHierarchy;
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
                        if (newSlot.isConnected)
                        {
                            portInputView?.RemoveFromHierarchy();
                        }
                        else
                        {
                            portInputView?.UpdateSlot(newSlot);
                        }

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
                if (port.slot.isConnected)
                {
                    continue;
                }

                var portInputView = m_PortInputContainer.Children().OfType<PortInputView>().FirstOrDefault(a => Equals(a.slot, port.slot));
                if (portInputView == null)
                {
                    portInputView = new PortInputView(port.slot) { style = { position = Position.Absolute } };
                    m_PortInputContainer.Add(portInputView);
                    SetPortInputPosition(port, portInputView);
                }

                port.RegisterCallback<GeometryChangedEvent>(UpdatePortInput);
            }
        }

        void UpdatePortInput(GeometryChangedEvent evt)
        {
            var port = (ShaderPort)evt.target;
            var inputViews = m_PortInputContainer.Children().OfType<PortInputView>().Where(x => Equals(x.slot, port.slot));

            // Ensure PortInputViews are initialized correctly
            // Dynamic port lists require one update to validate before init
            if(inputViews.Count() != 0)
            {
                var inputView = inputViews.First();
                SetPortInputPosition(port, inputView);
            }

            port.UnregisterCallback<GeometryChangedEvent>(UpdatePortInput);
        }

        void SetPortInputPosition(ShaderPort port, PortInputView inputView)
        {
            inputView.style.top = port.layout.y;
            inputView.parent.style.height = inputContainer.layout.height;
        }

        void UpdatePortInputVisibilities()
        {
            if (expanded)
            {
                m_PortInputContainer.style.display = StyleKeyword.Null;
            }
            else
            {
                m_PortInputContainer.style.display = DisplayStyle.None;
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

        void OnMouseHover(EventBase evt)
        {
            var graphView = GetFirstAncestorOfType<GraphEditorView>();
            if (graphView == null)
                return;

            var blackboardProvider = graphView.blackboardProvider;
            if (blackboardProvider == null)
                return;

            // Keyword nodes should be highlighted when Blackboard entry is hovered
            // TODO: Move to new NodeView type when keyword node has unique style
            if(node is KeywordNode keywordNode)
            {
                var keywordRow = blackboardProvider.GetBlackboardRow(keywordNode.keywordGuid);
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
            ((VisualElement)this).userData = null;
            if (m_PreviewRenderData != null)
            {
                m_PreviewRenderData.onPreviewChanged -= UpdatePreviewTexture;
                m_PreviewRenderData = null;
            }
        }
    }
}
