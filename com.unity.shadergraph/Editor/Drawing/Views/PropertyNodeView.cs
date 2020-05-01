using System;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphing;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;
using Data.Interfaces;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;

namespace UnityEditor.ShaderGraph
{
    sealed class PropertyNodeView : TokenNode, IShaderNodeView, IInspectable
    {
        static Type s_ContextualMenuManipulator = TypeCache.GetTypesDerivedFrom<MouseManipulator>().FirstOrDefault(t => t.FullName == "UnityEngine.UIElements.ContextualMenuManipulator");
        static readonly Texture2D exposedIcon = Resources.Load<Texture2D>("GraphView/Nodes/BlackboardFieldExposed");

        // When the properties are changed, this delegate is used to trigger an update in the view that represents those properties
        Action m_propertyViewUpdateTrigger;

        IManipulator m_ResetReferenceMenu;

        ShaderInputPropertyDrawer.ChangeReferenceNameCallback m_resetReferenceNameTrigger;

        public PropertyNodeView(PropertyNode node, EdgeConnectorListener edgeConnectorListener)
            : base(null, ShaderPort.Create(node.GetOutputSlots<MaterialSlot>().First(), edgeConnectorListener))
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyNodeView"));
            this.node = node;
            viewDataKey = node.objectId.ToString();
            userData = node;

            // Getting the generatePropertyBlock property to see if it is exposed or not
            var graph = node.owner as GraphData;
            var property = node.property;
            var icon = (graph.isSubGraph || (property.isExposable && property.generatePropertyBlock)) ? exposedIcon : null;
            this.icon = icon;

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

        public PropertyInfo[] GetPropertyInfo()
        {
            // The AbstractShaderProperty is declared as private here so we're specifying the NonPublic flag
            return this.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public void SupplyDataToPropertyDrawer(IPropertyDrawer propertyDrawer, Action inspectorUpdateDelegate)
        {
            if(propertyDrawer is ShaderInputPropertyDrawer shaderInputPropertyDrawer)
            {
                var propNode = node as PropertyNode;
                var graph = node.owner as GraphData;

                shaderInputPropertyDrawer.GetPropertyData(graph.isSubGraph,
                    this.ChangeExposedField,
                    this.ChangeReferenceNameField,
                    () => graph.ValidateGraph(),
                    () => graph.OnKeywordChanged(),
                    this.ChangePropertyValue,
                    this.RegisterPropertyChangeUndo,
                    this.MarkNodesAsDirty);

                this.m_propertyViewUpdateTrigger = inspectorUpdateDelegate;
                this.m_resetReferenceNameTrigger = shaderInputPropertyDrawer._resetReferenceNameCallback;
            }
        }

        void ChangeExposedField(bool newValue)
        {
            property.generatePropertyBlock = newValue;
            icon = property.generatePropertyBlock ? BlackboardProvider.exposedIcon : null;
        }

        void ChangeReferenceNameField(string newValue)
        {
            var graph = node.owner as GraphData;

            if (newValue != property.referenceName)
                graph.SanitizeGraphInputReferenceName(property, newValue);

            UpdateReferenceNameResetMenu();
        }
        void UpdateReferenceNameResetMenu()
        {
            if (string.IsNullOrEmpty(property.overrideReferenceName))
            {
                this.RemoveManipulator(m_ResetReferenceMenu);
                m_ResetReferenceMenu = null;
            }
            else
            {
                m_ResetReferenceMenu = (IManipulator)Activator.CreateInstance(s_ContextualMenuManipulator, (Action<ContextualMenuPopulateEvent>)BuildResetReferenceNameContextualMenu);
                this.AddManipulator(m_ResetReferenceMenu);
            }
        }

        void BuildResetReferenceNameContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Reset Reference", e =>
            {
                property.overrideReferenceName = null;
                m_resetReferenceNameTrigger(property.referenceName);
                DirtyNodes(ModificationScope.Graph);
            }, DropdownMenuAction.AlwaysEnabled);
        }

        void RegisterPropertyChangeUndo(string actionName)
        {
            var graph = node.owner as GraphData;
            graph.owner.RegisterCompleteObjectUndo(actionName);
        }

        void MarkNodesAsDirty(bool triggerPropertyViewUpdate = false, ModificationScope modificationScope = ModificationScope.Node)
        {
            DirtyNodes(modificationScope);
            if(triggerPropertyViewUpdate)
                this.m_propertyViewUpdateTrigger();
        }

        void ChangePropertyValue(object newValue)
        {
            if(property == null)
                return;

            switch(property)
            {
                case BooleanShaderProperty booleanProperty:
                    booleanProperty.value = ((ToggleData)newValue).isOn;
                    break;
                case Vector1ShaderProperty vector1Property:
                    vector1Property.value = (float) newValue;
                    break;
                case Vector2ShaderProperty vector2Property:
                    vector2Property.value = (Vector2) newValue;
                    break;
                case Vector3ShaderProperty vector3Property:
                    vector3Property.value = (Vector3) newValue;
                    break;
                case Vector4ShaderProperty vector4Property:
                    vector4Property.value = (Vector4) newValue;
                    break;
                case ColorShaderProperty colorProperty:
                    colorProperty.value = (Color) newValue;
                    break;
                case Texture2DShaderProperty texture2DProperty:
                    texture2DProperty.value.texture = (Texture) newValue;
                    break;
                case Texture2DArrayShaderProperty texture2DArrayProperty:
                    texture2DArrayProperty.value.textureArray = (Texture2DArray) newValue;
                    break;
                case Texture3DShaderProperty texture3DProperty:
                    texture3DProperty.value.texture = (Texture3D) newValue;
                    break;
                case CubemapShaderProperty cubemapProperty:
                    cubemapProperty.value.cubemap = (Cubemap) newValue;
                    break;
                case Matrix2ShaderProperty matrix2Property:
                    matrix2Property.value = (Matrix4x4) newValue;
                    break;
                case Matrix3ShaderProperty matrix3Property:
                    matrix3Property.value = (Matrix4x4) newValue;
                    break;
                case Matrix4ShaderProperty matrix4Property:
                    matrix4Property.value = (Matrix4x4) newValue;
                    break;
                case SamplerStateShaderProperty samplerStateProperty:
                    samplerStateProperty.value = (TextureSamplerState) newValue;
                    break;
                case GradientShaderProperty gradientProperty:
                    gradientProperty.value = (Gradient) newValue;
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

        public void OnModified(ModificationScope scope)
        {
            SetActive(node.isActive);
            
            if (scope == ModificationScope.Graph)
            {
                // changing the icon to be exposed or not
                var propNode = (PropertyNode)node;
                var graph = node.owner as GraphData;
                var property = propNode.property;

                var icon = property.generatePropertyBlock ? exposedIcon : null;
                this.icon = icon;
            }

            if (scope == ModificationScope.Topological)
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

            var propRow = blackboardProvider.GetBlackboardRow(propNode.property);
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
        }
    }
}
