using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    sealed class VirtualFunctionStackLayout
    {
        readonly MaterialGraphView m_GraphView;
        readonly GraphData m_Graph;
        readonly Dictionary<string, VirtualFunctionStackView> m_Stacks = new();
        readonly List<Edge> m_Chain = new();

        internal VirtualFunctionStackLayout(MaterialGraphView graphView, GraphData graph)
        {
            m_GraphView = graphView;
            m_Graph = graph;
        }

        internal void AdoptOutputView(MaterialNodeView view, VirtualFunctionOutputNode output)
        {
            view.capabilities &= ~Capabilities.Movable;
            view.AddManipulator(new VirtualFunctionNodeDragger(m_GraphView, output));
        }

        internal void Refresh()
        {
            var outputs = m_Graph.VirtualFunctionOutputNodesByKey();
            HashSet<string> contexts = new();
            foreach (var stackData in m_Graph.virtualFunctionStacks)
            {
                contexts.Add(stackData.context);
                if (m_Stacks.TryGetValue(stackData.context, out var stack))
                    stack.Bind(stackData);
                else
                {
                    stack = new VirtualFunctionStackView(stackData);
                    stack.RegisterCallback<GeometryChangedEvent>(_ => PlaceNewStacks());
                    m_GraphView.AddElement(stack);
                    m_Stacks.Add(stackData.context, stack);
                }

                for (int i = 0; i < stackData.providerKeys.Count; ++i)
                {
                    if (outputs.TryGetValue(stackData.providerKeys[i], out var output) && m_GraphView.GetNodeByGuid(output.objectId) is Node view)
                        PlaceInStack(view, stack, i);
                }
            }

            List<string> removed = new();
            foreach (var pair in m_Stacks)
                if (!contexts.Contains(pair.Key))
                    removed.Add(pair.Key);
            foreach (var context in removed)
            {
                m_GraphView.RemoveElement(m_Stacks[context]);
                m_Stacks.Remove(context);
            }

            Chain();
            PlaceNewStacks();
        }

        void PlaceInStack(Node view, VirtualFunctionStackView stack, int index)
        {
            var current = view.GetFirstAncestorOfType<VirtualFunctionStackView>();
            if (current == stack && stack.IndexOf(view) == index)
                return;

            if (current != null)
                current.RemoveElement(view);
            else
                m_GraphView.RemoveElement(view);
            stack.InsertElement(Math.Min(index, stack.childCount), view);
        }

        void Chain()
        {
            foreach (var edge in m_Chain)
                m_GraphView.RemoveElement(edge);
            m_Chain.Clear();

            var stacks = m_Graph.virtualFunctionStacks;
            for (int i = 0; i < stacks.Count; ++i)
            {
                var view = m_Stacks[stacks[i].context];
                view.inputPort.style.display = i > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                view.outputPort.style.display = i < stacks.Count - 1 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            for (int i = 1; i < stacks.Count; ++i)
            {
                var edge = new Edge
                {
                    output = m_Stacks[stacks[i - 1].context].outputPort,
                    input = m_Stacks[stacks[i].context].inputPort,
                    pickingMode = PickingMode.Ignore,
                };
                m_GraphView.AddElement(edge);
                m_Chain.Add(edge);
            }
        }

        // Waits for a layout while sizes are unknown; derived positions aren't recorded for undo.
        void PlaceNewStacks()
        {
            var stacks = m_Graph.virtualFunctionStacks;
            bool anyNew = false;
            foreach (var stackData in stacks)
                anyNew |= !stackData.isPlaced;
            if (!anyNew)
                return;

            List<Vector2> sizes = new();
            foreach (var stackData in stacks)
            {
                if (!m_Stacks.TryGetValue(stackData.context, out var stack) || float.IsNaN(stack.layout.width))
                    return;
                sizes.Add(stack.layout.size);
            }

            VirtualFunctionStackData.PlaceNew(stacks, sizes);
            foreach (var stackData in stacks)
                m_Stacks[stackData.context].Bind(stackData);
        }
    }
}
