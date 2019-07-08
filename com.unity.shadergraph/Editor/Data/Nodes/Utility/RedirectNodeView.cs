using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Reflection;

using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    class RedirectNodeView : Node, IShaderNodeView
    {
        IEdgeConnectorListener m_ConnectorListener;
        VisualElement m_TitleContainer;

        GraphView m_GraphView;

        Port m_TempPort;

        ///////////////////////////////////////////////////////////
        /// Initialization
        ///////////////////////////////////////////////////////////
        public RedirectNodeView()//: base("UXML/RedirectNode.uxml")
        {

        }

        public void Initialize(AbstractMaterialNode inNode, PreviewManager previewManager, IEdgeConnectorListener connectorListener, GraphView graphView)
        {
            // Styling
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/RedirectNodeView")); // @SamH: Update with real path
            //ClearClassList();
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
        }

        #region IShaderNodeView interface
        public Node gvNode => this;
        public AbstractMaterialNode node { get; private set; }
        public VisualElement colorElement { get { return this; } }

        public void Dispose()
        {

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
                //UpdatePortInputVisibilities();
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {

        }
        #endregion
    }
}
