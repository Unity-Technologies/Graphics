using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    sealed class PropertyNodeView : TokenNode, IShaderNodeView
    {

        public PropertyNodeView(PropertyNode node, EdgeConnectorListener edgeConnectorListener)
            : base(null, ShaderPort.Create(node.GetOutputSlots<MaterialSlot>().First(), edgeConnectorListener))
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyNodeView"));
            this.node = node;
            viewDataKey = node.guid.ToString();
            userData = node;

            // Getting the generatePropertyBlock property to see if it is exposed or not
            var graph = node.owner as GraphData;
            var property = graph.properties.FirstOrDefault(x => x.guid == node.propertyGuid);
            var icon = (graph.isSubGraph || (property.isExposable && property.generatePropertyBlock)) ? exposedIcon : null;
            this.icon = icon;

            // Setting the position of the node, otherwise it ends up in the center of the canvas
            SetPosition(new Rect(node.drawState.position.x, node.drawState.position.y, 0, 0));

            // Removing the title label since it is not used and taking up space
            this.Q("title-label").RemoveFromHierarchy();

            // Registering the hovering callbacks for highlighting
            RegisterCallback<MouseEnterEvent>(OnMouseHover);
            RegisterCallback<MouseLeaveEvent>(OnMouseHover);
        }
        public static readonly Texture2D exposedIcon = Resources.Load<Texture2D>("GraphView/Nodes/BlackboardFieldExposed");
        public Node gvNode => this;
        public AbstractMaterialNode node { get; }

        public VisualElement colorElement => null;

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
            if (scope == ModificationScope.Graph)
            {
                // changing the icon to be exposed or not
                var propNode = (PropertyNode)node;
                var graph = node.owner as GraphData;
                var property = graph.properties.FirstOrDefault(x => x.guid == propNode.propertyGuid);

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

        public void Dispose()
        {
        }
    }
}
