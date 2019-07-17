using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Reflection;

using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing.Controls;

using Edge = UnityEditor.Experimental.GraphView.Edge;

namespace UnityEditor.ShaderGraph
{
    class RedirectNodeView : Node, IShaderNodeView
    {
        IEdgeConnectorListener m_ConnectorListener;
        VisualElement m_TitleContainer;
        GraphView m_GraphView;

        public RedirectNodeView()
        {
        }

        public void Initialize(AbstractMaterialNode inNode, PreviewManager previewManager, IEdgeConnectorListener connectorListener, GraphView graphView)
        {
            // Styling
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/RedirectNodeView"));
            AddToClassList("redirect-node");

            if (inNode == null)
                return;

            // Set references
            node = inNode;
            title = node.name;
            m_GraphView = graphView;
            m_ConnectorListener = connectorListener;

            viewDataKey = node.guid.ToString();

            // Expanded state
            base.expanded = node.drawState.expanded;
            RefreshExpandedState(); //This should not be needed. GraphView needs to improve the extension api here

            SetPosition(new Rect(node.drawState.position.x, node.drawState.position.y, 0, 0));
            AddSlots(node.GetSlots<MaterialSlot>());

            //callbacks
            RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
        }

        ///////////////////////////////////////////////////////////
        /// Callbacks
        ///////////////////////////////////////////////////////////
        void OnMouseDownEvent(MouseDownEvent evt)
        {
            if (expanded == true)
                return;

            var inSlot = node.GetSlotReference(0);
            var inNode = node.owner.GetNodeFromGuid(inSlot.nodeGuid);
            if (inNode != null)
            {

            }

            //if(evt.target is Edge)
            //{
            //    if (evt.clickCount == 2 && evt.button == 0)
            //    {
            //        var mGraph = m_GraphView as MaterialGraphView;
            //        mGraph.AddRedirectNode(evt.target as Edge, evt.localMousePosition);
            //    }
            //}
        }

        #region Helper functions
        //private void AddPortPair(Type type, int key = -1)
        //{
        //    var data = userData as RedirectNodeData;
        //    data.AddPortPair();
        //}

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
        #endregion

        #region IShaderNodeView interface
        public Node gvNode => this;
        public AbstractMaterialNode node { get; private set; }
        public VisualElement colorElement { get { return this; } }

        public void Dispose()
        {
            node = null;
            ((VisualElement)this).userData = null;
        }

        public void OnModified(ModificationScope scope)
        {
            
        }

        public void UpdatePortInputTypes()
        {

        }

        public void SetColor(Color newColor)
        {

        }

        public void ResetColor()
        {

        }
        #endregion

        #region Node class overrides
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {

        }
        #endregion
    }
}
