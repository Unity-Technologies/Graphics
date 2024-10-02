using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphing;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;
using ContextualMenuManipulator = UnityEngine.UIElements.ContextualMenuManipulator;

namespace UnityEditor.ShaderGraph
{
    sealed class PropertyNodeView : TokenNode, IShaderNodeView, IInspectable, IShaderInputObserver
    {
        static readonly Texture2D exposedIcon = Resources.Load<Texture2D>("GraphView/Nodes/BlackboardFieldExposed");
        public static StyleSheet styleSheet;

        // When the properties are changed, this delegate is used to trigger an update in the view that represents those properties
        Action m_TriggerInspectorUpdate;

        Action m_ResetReferenceNameAction;

        public PropertyNodeView(PropertyNode node, EdgeConnectorListener edgeConnectorListener)
            : base(null, ShaderPort.Create(node.GetOutputSlots<MaterialSlot>().First(), edgeConnectorListener))
        {
            if (styleSheet == null)
                styleSheet = Resources.Load<StyleSheet>("Styles/PropertyNodeView");
            styleSheets.Add(styleSheet);

            this.node = node;
            viewDataKey = node.objectId.ToString();
            userData = node;

            // Getting the generatePropertyBlock property to see if it is exposed or not
            UpdateIcon();

            // Setting the position of the node, otherwise it ends up in the center of the canvas
            SetPosition(new Rect(node.drawState.position.x, node.drawState.position.y, 0, 0));

            // Removing the title label since it is not used and taking up space
            this.Q("title-label").RemoveFromHierarchy();

            // Add disabled overlay
            Add(new VisualElement() { name = "disabledOverlay", pickingMode = PickingMode.Ignore });

            // Update active state
            SetActive(node.isActive);

            // Registering the hovering callbacks for highlighting
            RegisterCallback<MouseEnterEvent>(OnMouseHover);
            RegisterCallback<MouseLeaveEvent>(OnMouseHover);

            // add the right click context menu
            IManipulator contextMenuManipulator = new ContextualMenuManipulator(AddContextMenuOptions);
            this.AddManipulator(contextMenuManipulator);

            if (property != null)
            {
                property.onAfterVersionChange += () =>
                {
                    m_TriggerInspectorUpdate?.Invoke();
                };
            }
        }

        // Updating the text label of the output slot
        void UpdateDisplayName()
        {
            var slot = node.GetSlots<MaterialSlot>().ToList().First();
            this.Q<Label>("type").text = slot.displayName;
        }

        public Node gvNode => this;
        public AbstractMaterialNode node { get; }
        public VisualElement colorElement => null;
        public string inspectorTitle => $"{property.displayName} (Node)";

        [Inspectable("ShaderInput", null)]
        AbstractShaderProperty property => (node as PropertyNode)?.property;

        public object GetObjectToInspect()
        {
            return property;
        }

        public void SupplyDataToPropertyDrawer(IPropertyDrawer propertyDrawer, Action inspectorUpdateDelegate)
        {
            if (propertyDrawer is ShaderInputPropertyDrawer shaderInputPropertyDrawer)
            {
                var propNode = node as PropertyNode;
                var graph = node.owner as GraphData;

                var shaderInputViewModel = new ShaderInputViewModel()
                {
                    model = property,
                    parentView = null,
                    isSubGraph = graph.isSubGraph,
                    isInputExposed = property.isExposed,
                    inputName = property.displayName,
                    inputTypeName = property.GetPropertyTypeString(),
                    requestModelChangeAction = this.RequestModelChange
                };
                shaderInputPropertyDrawer.GetViewModel(shaderInputViewModel, node.owner, this.OnDisplayNameUpdated);

                this.m_TriggerInspectorUpdate = inspectorUpdateDelegate;
                this.m_ResetReferenceNameAction = shaderInputPropertyDrawer.ResetReferenceName;
            }
        }

        void RequestModelChange(IGraphDataAction changeAction)
        {
            node.owner?.owner.graphDataStore.Dispatch(changeAction);
        }

        internal static void AddMainColorMenuOptions(ContextualMenuPopulateEvent evt, ColorShaderProperty colorProp, GraphData graphData, Action inspectorUpdateAction)
        {
            if (!graphData.isSubGraph)
            {
                if (!colorProp.isMainColor)
                {
                    evt.menu.AppendAction(
                        "Set as Main Color",
                        e =>
                        {
                            ColorShaderProperty col = graphData.GetMainColor();
                            if (col != null)
                            {
                                if (EditorUtility.DisplayDialog("Change Main Color Action", $"Are you sure you want to change the Main Color from {col.displayName} to {colorProp.displayName}?", "Yes", "Cancel"))
                                {
                                    graphData.owner.RegisterCompleteObjectUndo("Change Main Color");
                                    col.isMainColor = false;
                                    colorProp.isMainColor = true;
                                    inspectorUpdateAction();
                                }
                                return;
                            }

                            graphData.owner.RegisterCompleteObjectUndo("Set Main Color");
                            colorProp.isMainColor = true;
                            inspectorUpdateAction();
                        });
                }
                else
                {
                    evt.menu.AppendAction(
                        "Clear Main Color",
                        e =>
                        {
                            graphData.owner.RegisterCompleteObjectUndo("Clear Main Color");
                            colorProp.isMainColor = false;
                            inspectorUpdateAction();
                        });
                }
            }
        }

        internal static void AddMainTextureMenuOptions(ContextualMenuPopulateEvent evt, Texture2DShaderProperty texProp, GraphData graphData, Action inspectorUpdateAction)
        {
            if (!graphData.isSubGraph)
            {
                if (!texProp.isMainTexture)
                {
                    evt.menu.AppendAction(
                        "Set as Main Texture",
                        e =>
                        {
                            Texture2DShaderProperty tex = graphData.GetMainTexture();
                            // There's already a main texture, ask the user if they want to change and toggle the old one to not be main
                            if (tex != null)
                            {
                                if (EditorUtility.DisplayDialog("Change Main Texture Action", $"Are you sure you want to change the Main Texture from {tex.displayName} to {texProp.displayName}?", "Yes", "Cancel"))
                                {
                                    graphData.owner.RegisterCompleteObjectUndo("Change Main Texture");
                                    tex.isMainTexture = false;
                                    texProp.isMainTexture = true;
                                    inspectorUpdateAction();
                                }
                                return;
                            }

                            graphData.owner.RegisterCompleteObjectUndo("Set Main Texture");
                            texProp.isMainTexture = true;
                            inspectorUpdateAction();
                        });
                }
                else
                {
                    evt.menu.AppendAction(
                        "Clear Main Texture",
                        e =>
                        {
                            graphData.owner.RegisterCompleteObjectUndo("Clear Main Texture");
                            texProp.isMainTexture = false;
                            inspectorUpdateAction();
                        });
                }
            }
        }

        void AddContextMenuOptions(ContextualMenuPopulateEvent evt)
        {
            // Checks if the reference name has been overridden and appends menu action to reset it, if so
            if (property.isRenamable &&
                !string.IsNullOrEmpty(property.overrideReferenceName))
            {
                evt.menu.AppendAction(
                    "Reset Reference",
                    e =>
                    {
                        m_ResetReferenceNameAction();
                        DirtyNodes(ModificationScope.Graph);
                    },
                    DropdownMenuAction.AlwaysEnabled);
            }

            if (property is ColorShaderProperty colorProp)
            {
                AddMainColorMenuOptions(evt, colorProp, node.owner, m_TriggerInspectorUpdate);
            }

            if (property is Texture2DShaderProperty texProp)
            {
                AddMainTextureMenuOptions(evt, texProp, node.owner, m_TriggerInspectorUpdate);
            }
        }

        void OnDisplayNameUpdated(bool triggerPropertyViewUpdate = false, ModificationScope modificationScope = ModificationScope.Node)
        {
            if (triggerPropertyViewUpdate)
                m_TriggerInspectorUpdate?.Invoke();

            UpdateDisplayName();
        }

        void DirtyNodes(ModificationScope modificationScope = ModificationScope.Node)
        {
            var graph = node.owner as GraphData;

            var colorManager = GetFirstAncestorOfType<GraphEditorView>().colorManager;
            var nodes = GetFirstAncestorOfType<GraphEditorView>().graphView.Query<MaterialNodeView>().ToList();

            colorManager.SetNodesDirty(nodes);
            colorManager.UpdateNodeViews(nodes);

            foreach (var node in graph.GetNodes<PropertyNode>())
            {
                node.Dirty(modificationScope);
            }
        }

        public void SetColor(Color newColor)
        {
            // Nothing to do here yet
        }

        public void ResetColor()
        {
            // Nothing to do here yet
        }

        public void UpdatePortInputTypes()
        {
        }

        public void UpdateDropdownEntries()
        {
        }

        public bool FindPort(SlotReference slot, out ShaderPort port)
        {
            port = output as ShaderPort;
            return port != null && port.slot.slotReference.Equals(slot);
        }

        void UpdateIcon()
        {
            var graph = node?.owner as GraphData;
            if ((graph != null) && (property != null))
                icon = (graph.isSubGraph || property.isExposed) ? exposedIcon : null;
            else
                icon = null;
        }

        public void OnModified(ModificationScope scope)
        {
            //disconnected property nodes are always active
            if (!node.IsSlotConnected(PropertyNode.OutputSlotId) && node.activeState is AbstractMaterialNode.ActiveState.Implicit)
                node.SetActive(true);

            SetActive(node.isActive);

            if (scope == ModificationScope.Graph)
            {
                UpdateIcon();
            }

            if (scope == ModificationScope.Topological || scope == ModificationScope.Node)
            {
                UpdateDisplayName();
            }
        }

        public void SetActive(bool state)
        {
            // Setup
            var disabledString = "disabled";

            if (!state)
            {
                // Add elements to disabled class list
                AddToClassList(disabledString);
            }
            else
            {
                // Remove elements from disabled class list
                RemoveFromClassList(disabledString);
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
            badge.AttachTo(this, SpriteAlignment.RightCenter);
        }

        public void ClearMessage()
        {
            var badge = this.Q<IconBadge>();
            if (badge != null)
            {
                badge.Detach();
                badge.RemoveFromHierarchy();
            }
        }

        SGBlackboardRow GetAssociatedBlackboardRow()
        {
            var graphView = GetFirstAncestorOfType<GraphEditorView>();

            var blackboardController = graphView?.blackboardController;
            if (blackboardController == null)
                return null;

            var propNode = (PropertyNode)node;
            return blackboardController.GetBlackboardRow(propNode.property);
        }

        void OnMouseHover(EventBase evt)
        {
            var propRow = GetAssociatedBlackboardRow();
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

        public void Dispose()
        {
            var propRow = GetAssociatedBlackboardRow();
            // If this node view is deleted, remove highlighting from associated blackboard row
            if (propRow != null)
            {
                propRow.RemoveFromClassList("hovered");
            }
            styleSheets.Clear();
            m_TriggerInspectorUpdate = null;
            m_ResetReferenceNameAction = null;
            UnregisterCallback<MouseEnterEvent>(OnMouseHover);
            UnregisterCallback<MouseLeaveEvent>(OnMouseHover);
        }

        public void OnShaderInputUpdated(ModificationScope modificationScope)
        {
            if (modificationScope == ModificationScope.Layout)
                UpdateDisplayName();
        }
    }
}
