using System;
using System.Collections.Generic;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEngine;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // Holds its output nodes by provider key, in order.
    [Serializable]
    sealed class VirtualFunctionStackData
    {
        [SerializeField]
        string m_Context;

        [SerializeField]
        List<string> m_ProviderKeys = new();

        [SerializeField]
        Vector2 m_Position;

        [SerializeField]
        bool m_IsPlaced;

        internal VirtualFunctionStackData(string context, List<string> providerKeys)
        {
            m_Context = context ?? string.Empty;
            m_ProviderKeys = providerKeys;
        }

        internal string context => m_Context ?? string.Empty;

        internal IReadOnlyList<string> providerKeys => m_ProviderKeys;

        internal bool isPlaced => m_IsPlaced;

        internal Vector2 position
        {
            get => m_Position;
            set
            {
                m_Position = value;
                m_IsPlaced = true;
            }
        }

        internal bool Holds(string providerKey) => m_ProviderKeys.Contains(providerKey);

        internal void Remove(string providerKey) => m_ProviderKeys.Remove(providerKey);

        internal bool Matches(VirtualFunctionStackData other)
        {
            if (context != other.context || m_ProviderKeys.Count != other.m_ProviderKeys.Count)
                return false;
            for (int i = 0; i < m_ProviderKeys.Count; ++i)
                if (m_ProviderKeys[i] != other.m_ProviderKeys[i])
                    return false;
            return true;
        }

        // Orphans follow the declared functions, in the stack they were in before.
        internal static List<VirtualFunctionStackData> StacksFor(List<IProvider<IVirtualFunction>> functions, ICollection<string> outputKeys, IReadOnlyList<VirtualFunctionStackData> previousStacks)
        {
            List<VirtualFunctionStackData> stacks = new();
            HashSet<string> placed = new();

            void Place(string context, string key)
            {
                if (!placed.Add(key))
                    return;
                var stack = stacks.Find(s => s.context == context);
                if (stack == null)
                {
                    stack = new VirtualFunctionStackData(context, new List<string>());
                    var previous = FindStack(previousStacks, context);
                    if (previous is { isPlaced: true })
                        stack.position = previous.position;
                    stacks.Add(stack);
                }
                stack.m_ProviderKeys.Add(key);
            }

            foreach (var function in Order(functions))
                Place(AbstractVirtualFunctionNode.ContextOf(function.Definition), function.Info.ProviderKey);
            foreach (var stack in previousStacks)
                foreach (var key in stack.providerKeys)
                    if (outputKeys.Contains(key))
                        Place(stack.context, key);
            foreach (var key in outputKeys)
                Place(string.Empty, key);
            return stacks;
        }

        internal static void PlaceNew(IReadOnlyList<VirtualFunctionStackData> stacks, IReadOnlyList<Vector2> sizes)
        {
            const float kSpacing = 60;

            bool anyPlaced = false;
            float bottom = 0;
            float left = 0;
            for (int i = 0; i < stacks.Count; ++i)
            {
                if (!stacks[i].isPlaced)
                    continue;
                var position = stacks[i].position;
                bottom = anyPlaced ? Mathf.Max(bottom, position.y + sizes[i].y) : position.y + sizes[i].y;
                left = anyPlaced ? Mathf.Min(left, position.x) : position.x;
                anyPlaced = true;
            }

            for (int i = 0; i < stacks.Count; ++i)
            {
                if (stacks[i].isPlaced)
                    continue;
                stacks[i].position = anyPlaced ? new Vector2(left, bottom + kSpacing) : Vector2.zero;
                bottom = stacks[i].position.y + sizes[i].y;
                left = stacks[i].position.x;
                anyPlaced = true;
            }
        }

        static VirtualFunctionStackData FindStack(IReadOnlyList<VirtualFunctionStackData> stacks, string context)
        {
            foreach (var stack in stacks)
                if (stack.context == context)
                    return stack;
            return null;
        }

        // Authored context orders first, then other contexts, then none; within each, authored orders first; ties keep the interface's order.
        static List<IProvider<IVirtualFunction>> Order(List<IProvider<IVirtualFunction>> functions)
        {
            Dictionary<string, (int? order, int firstIndex)> contexts = new();
            for (int i = 0; i < functions.Count; ++i)
            {
                var presentation = functions[i].Definition.Presentation;
                var context = AbstractVirtualFunctionNode.ContextOf(functions[i].Definition);
                if (!contexts.TryGetValue(context, out var known))
                    contexts.Add(context, (presentation.ContextOrder, i));
                else if (CompareOrders(presentation.ContextOrder, known.order) < 0)
                    contexts[context] = (presentation.ContextOrder, known.firstIndex);
            }

            int CompareContexts(string a, string b)
            {
                if (a == b)
                    return 0;
                if ((a.Length == 0) != (b.Length == 0))
                    return a.Length == 0 ? 1 : -1;
                var (orderA, firstA) = contexts[a];
                var (orderB, firstB) = contexts[b];
                int byOrder = CompareOrders(orderA, orderB);
                return byOrder != 0 ? byOrder : firstA.CompareTo(firstB);
            }

            List<int> indices = new();
            for (int i = 0; i < functions.Count; ++i)
                indices.Add(i);
            indices.Sort((a, b) =>
            {
                var definitionA = functions[a].Definition;
                var definitionB = functions[b].Definition;
                int byContext = CompareContexts(AbstractVirtualFunctionNode.ContextOf(definitionA), AbstractVirtualFunctionNode.ContextOf(definitionB));
                if (byContext != 0)
                    return byContext;
                int byOrder = CompareOrders(definitionA.Presentation.OrderInContext, definitionB.Presentation.OrderInContext);
                return byOrder != 0 ? byOrder : a.CompareTo(b);
            });

            List<IProvider<IVirtualFunction>> ordered = new();
            foreach (var index in indices)
                ordered.Add(functions[index]);
            return ordered;
        }

        static int CompareOrders(int? a, int? b)
        {
            if (a.HasValue != b.HasValue)
                return a.HasValue ? -1 : 1;
            return (a ?? 0).CompareTo(b ?? 0);
        }
    }
}
