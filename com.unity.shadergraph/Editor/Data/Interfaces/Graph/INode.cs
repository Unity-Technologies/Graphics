using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Graphing
{
    enum ModificationScope
    {
        Nothing = 0,
        Node = 1,
        Graph = 2,
        Topological = 3,
        Layout = 4
    }

    delegate void OnNodeModified(AbstractMaterialNode node, ModificationScope scope);

    static class NodeExtensions
    {
        public static IEnumerable<T> GetSlots<T>(this AbstractMaterialNode node) where T : MaterialSlot
        {
            var slots = new List<T>();
            node.GetSlots(slots);
            return slots;
        }

        public static IEnumerable<T> GetInputSlots<T>(this AbstractMaterialNode node) where T : MaterialSlot
        {
            var slots = new List<T>();
            node.GetInputSlots(slots);
            return slots;
        }

        public static IEnumerable<T> GetOutputSlots<T>(this AbstractMaterialNode node) where T : MaterialSlot
        {
            var slots = new List<T>();
            node.GetOutputSlots(slots);
            return slots;
        }
    }
}
