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
using UnityEditor.ShaderGraph.Drawing.Views.Blackboard;
using ContextualMenuManipulator = UnityEngine.UIElements.ContextualMenuManipulator;

namespace UnityEditor.ShaderGraph
{
    sealed class PropertyNodeView : TokenNode, IShaderNodeView, IInspectable
    {
        static readonly Texture2D exposedIcon = Resources.Load<Texture2D>("GraphView/Nodes/BlackboardFieldExposed");

        internal delegate void ChangeDisplayNameCallback(string newDisplayName);

        // When the properties are changed, this delegate is used to trigger an update in the view that represents those properties
        Action m_propertyViewUpdateTrigger;

        Action m_ResetReferenceNameAction;

        public PropertyNodeView(PropertyNode node, EdgeConnectorListener edgeConnectorListener)
            : base(null, ShaderPort.Create(node.GetOutputSlots<MaterialSlot>().First(), edgeConnectorListener))
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyNodeView"));
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

            // Set callback association for display name updates
            property.displayNameUpdateTrigger += node.UpdateNodeDisplayName;
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

                shaderInputPropertyDrawer.GetPropertyData(
                    graph.isSubGraph,
                    graph,
                    this.ChangeExposedField,
                    () => graph.ValidateGraph(),
                    () => graph.OnKeywordChanged(),
                    this.ChangePropertyValue,
                    this.RegisterPropertyChangeUndo,
                    this.MarkNodesAsDirty);

                this.m_propertyViewUpdateTrigger = inspectorUpdateDelegate;
                this.m_ResetReferenceNameAction = shaderInputPropertyDrawer.ResetReferenceName;
            }
        }

        void ChangeExposedField(bool newValue)
        {
            property.generatePropertyBlock = newValue;
            UpdateIcon();
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
        }

        void RegisterPropertyChangeUndo(string actionName)
        {
            var graph = node.owner as GraphData;
            graph.owner.RegisterCompleteObjectUndo(actionName);
        }

        void MarkNodesAsDirty(bool triggerPropertyViewUpdate = false, ModificationScope modificationScope = ModificationScope.Node)
        {
            DirtyNodes(modificationScope);
            if (triggerPropertyViewUpdate)
                this.m_propertyViewUpdateTrigger();
        }

        void ChangePropertyValue(object newValue)
        {
            if (property == null)
                return;

            switch (property)
            {
                case BooleanShaderProperty booleanProperty:
                    booleanProperty.value = ((ToggleData)newValue).isOn;
                    break;
                case Vector1ShaderProperty vector1Property:
                    vector1Property.value = (float)newValue;
                    break;
                case Vector2ShaderProperty vector2Property:
                    vector2Property.value = (Vector2)newValue;
                    break;
                case Vector3ShaderProperty vector3Property:
                    vector3Property.value = (Vector3)newValue;
                    break;
                case Vector4ShaderProperty vector4Property:
                    vector4Property.value = (Vector4)newValue;
                    break;
                case ColorShaderProperty colorProperty:
                    colorProperty.value = (Color)newValue;
                    break;
                case Texture2DShaderProperty texture2DProperty:
                    texture2DProperty.value.texture = (Texture)newValue;
                    break;
                case Texture2DArrayShaderProperty texture2DArrayProperty:
                    texture2DArrayProperty.value.textureArray = (Texture2DArray)newValue;
                    break;
                case Texture3DShaderProperty texture3DProperty:
                    texture3DProperty.value.texture = (Texture3D)newValue;
                    break;
                case CubemapShaderProperty cubemapProperty:
                    cubemapProperty.value.cubemap = (Cubemap)newValue;
                    break;
                case Matrix2ShaderProperty matrix2Property:
                    matrix2Property.value = (Matrix4x4)newValue;
                    break;
                case Matrix3ShaderProperty matrix3Property:
                    matrix3Property.value = (Matrix4x4)newValue;
                    break;
                case Matrix4ShaderProperty matrix4Property:
                    matrix4Property.value = (Matrix4x4)newValue;
                    break;
                case SamplerStateShaderProperty samplerStateProperty:
                    samplerStateProperty.value = (TextureSamplerState)newValue;
                    break;
                case GradientShaderProperty gradientProperty:
                    gradientProperty.value = (Gradient)newValue;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            this.MarkDirtyRepaint();
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

        public bool FindPort(SlotReference slot, out ShaderPort port)
        {
            port = output as ShaderPort;
            return port != null && port.slot.slotReference.Equals(slot);
        }

        void UpdateIcon()
        {
            var graph = node?.owner as GraphData;
            if ((graph != null) && (property != null))
                icon = (graph.isSubGraph || property.isExposed) ? BlackboardProvider.exposedIcon : null;
            else
                icon = null;
        }

        public void OnModified(ModificationScope scope)
        {
            //disconnected property nodes are always active
            if (!node.IsSlotConnected(PropertyNode.OutputSlotId))
                node.SetActive(true);

            SetActive(node.isActive);

            if (scope == ModificationScope.Graph)
            {
                UpdateIcon();
            }

            if (scope == ModificationScope.Topological || scope == ModificationScope.Node)
            {
                // Updating the text label of the output slot
                var slot = node.GetSlots<MaterialSlot>().ToList().First();
                this.Q<Label>("type").text = slot.displayName;
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

        BlackboardRow GetAssociatedBlackboardRow()
        {
            var graphView = GetFirstAncestorOfType<GraphEditorView>();
            if (graphView == null)
                return null;

            var blackboardProvider = graphView.blackboardProvider;
            if (blackboardProvider == null)
                return null;

            var propNode = (PropertyNode)node;
            return blackboardProvider.GetBlackboardRow(propNode.property);
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
            // The associated blackboard row can be deleted in which case this property node view is also cleaned up with it, so we want to check for null
            if (propRow != null)
            {
                propRow.RemoveFromClassList("hovered");
            }
        }
    }
}
