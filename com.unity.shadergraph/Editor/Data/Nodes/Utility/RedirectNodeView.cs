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

namespace UnityEditor.ShaderGraph
{
    class RedirectNodeView : Node, IShaderNodeView
    {
        IEdgeConnectorListener m_ConnectorListener;
        VisualElement m_TitleContainer;

        GraphView m_GraphView;
        
        VisualElement m_inOutPairsRoot;
        
        Port m_TempPort;

        ///////////////////////////////////////////////////////////
        /// Main
        ///////////////////////////////////////////////////////////
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
        }

        ///////////////////////////////////////////////////////////
        /// Helpers
        ///////////////////////////////////////////////////////////
        private void AddPortPair(Type type, int key = -1)
        {
            var data = userData as RedirectNodeData;
            data.AddPortPair();
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

        #region IShaderNodeView interface
        public Node gvNode => this;
        public AbstractMaterialNode node { get; private set; }
        public VisualElement colorElement { get { return this; } }

        public void Dispose()
        {
            //Merge input/output pairs into single edges

            node = null;
            ((VisualElement)this).userData = null;
        }

        public void OnModified(ModificationScope scope)
        {
            base.expanded = node.drawState.expanded;

            // Update slots to match node modification
            if (scope == ModificationScope.Topological)
            {
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
                    }
                    else
                    {
                        port.slot = newSlot;
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
            }

            RefreshExpandedState(); //This should not be needed. GraphView needs to improve the extension api here

            //foreach (var listener in m_ControlItems.Children().OfType<AbstractMaterialNodeModificationListener>())
            //{
            //    if (listener != null)
            //        listener.OnNodeModified(scope);
            //}
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
