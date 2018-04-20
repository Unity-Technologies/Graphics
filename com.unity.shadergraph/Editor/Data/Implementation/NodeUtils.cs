using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityEditor.Graphing
{
    public class SlotConfigurationException : Exception
    {
        public SlotConfigurationException(string message)
            : base(message)
        {}
    }

    public static class NodeUtils
    {
        public static void SlotConfigurationExceptionIfBadConfiguration(INode node, IEnumerable<int> expectedInputSlots, IEnumerable<int> expectedOutputSlots)
        {
            var missingSlots = new List<int>();

            var inputSlots = expectedInputSlots as IList<int> ?? expectedInputSlots.ToList();
            missingSlots.AddRange(inputSlots.Except(node.GetInputSlots<ISlot>().Select(x => x.id)));

            var outputSlots = expectedOutputSlots as IList<int> ?? expectedOutputSlots.ToList();
            missingSlots.AddRange(outputSlots.Except(node.GetOutputSlots<ISlot>().Select(x => x.id)));

            if (missingSlots.Count == 0)
                return;

            var toPrint = missingSlots.Select(x => x.ToString());

            throw new SlotConfigurationException(string.Format("Missing slots {0} on node {1}", string.Join(", ", toPrint.ToArray()), node));
        }

        public static IEnumerable<IEdge> GetAllEdges(INode node)
        {
            var result = new List<IEdge>();
            var validSlots = ListPool<ISlot>.Get();

            validSlots.AddRange(node.GetInputSlots<ISlot>());
            for (int index = 0; index < validSlots.Count; index++)
            {
                var inputSlot = validSlots[index];
                result.AddRange(node.owner.GetEdges(inputSlot.slotReference));
            }

            validSlots.Clear();
            validSlots.AddRange(node.GetOutputSlots<ISlot>());
            for (int index = 0; index < validSlots.Count; index++)
            {
                var outputSlot = validSlots[index];
                result.AddRange(node.owner.GetEdges(outputSlot.slotReference));
            }

            ListPool<ISlot>.Release(validSlots);
            return result;
        }

        // CollectNodesNodeFeedsInto looks at the current node and calculates
        // which child nodes it depends on for it's calculation.
        // Results are returned depth first so by processing each node in
        // order you can generate a valid code block.
        public enum IncludeSelf
        {
            Include,
            Exclude
        }

        public static void DepthFirstCollectNodesFromNode(List<INode> nodeList, INode node, IncludeSelf includeSelf = IncludeSelf.Include, List<int> slotIds = null)
        {
            // no where to start
            if (node == null)
                return;

            // already added this node
            if (nodeList.Contains(node))
                return;

            var ids = node.GetInputSlots<ISlot>().Select(x => x.id);
            if (slotIds != null)
                ids = node.GetInputSlots<ISlot>().Where(x => slotIds.Contains(x.id)).Select(x => x.id);

            foreach (var slot in ids)
            {
                foreach (var edge in node.owner.GetEdges(node.GetSlotReference(slot)))
                {
                    var outputNode = node.owner.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                    DepthFirstCollectNodesFromNode(nodeList, outputNode);
                }
            }

            if (includeSelf == IncludeSelf.Include)
                nodeList.Add(node);
        }

        public static void CollectNodesNodeFeedsInto(List<INode> nodeList, INode node, IncludeSelf includeSelf = IncludeSelf.Include)
        {
            if (node == null)
                return;

            if (nodeList.Contains(node))
                return;

            foreach (var slot in node.GetOutputSlots<ISlot>())
            {
                foreach (var edge in node.owner.GetEdges(slot.slotReference))
                {
                    var inputNode = node.owner.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                    CollectNodesNodeFeedsInto(nodeList, inputNode);
                }
            }
            if (includeSelf == IncludeSelf.Include)
                nodeList.Add(node);
        }

        static Stack<INode> s_NodeStack = new Stack<INode>();

        public static ShaderStage FindEffectiveShaderStage(INode initialNode, bool goingBackwards)
        {
            var shaderStage = ShaderStage.Dynamic;
            s_NodeStack.Clear();
            s_NodeStack.Push(initialNode);
            while (s_NodeStack.Any() && shaderStage == ShaderStage.Dynamic)
            {
                var node = s_NodeStack.Pop();
                foreach (var slot in goingBackwards ? node.GetInputSlots<MaterialSlot>() : node.GetOutputSlots<MaterialSlot>())
                {
                    if (shaderStage != ShaderStage.Dynamic)
                        break;
                    if (slot.shaderStage == ShaderStage.Dynamic)
                    {
                        foreach (var edge in node.owner.GetEdges(slot.slotReference))
                        {
                            var connectedNode = node.owner.GetNodeFromGuid(goingBackwards ? edge.outputSlot.nodeGuid : edge.inputSlot.nodeGuid);
                            var connectedSlot = goingBackwards ? connectedNode.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId) : connectedNode.FindInputSlot<MaterialSlot>(edge.inputSlot.slotId);
                            if (connectedSlot.shaderStage == ShaderStage.Dynamic)
                                s_NodeStack.Push(connectedNode);
                            else
                            {
                                shaderStage = connectedSlot.shaderStage;
                                break;
                            }
                        }
                    }
                    else
                        shaderStage = slot.shaderStage;
                }
            }
            return shaderStage;
        }

        public static string GetSlotDimension(ConcreteSlotValueType slotValue)
        {
            switch (slotValue)
            {
                case ConcreteSlotValueType.Vector1:
                    return String.Empty;
                case ConcreteSlotValueType.Vector2:
                    return "2";
                case ConcreteSlotValueType.Vector3:
                    return "3";
                case ConcreteSlotValueType.Vector4:
                    return "4";
                case ConcreteSlotValueType.Matrix2:
                    return "2x2";
                case ConcreteSlotValueType.Matrix3:
                    return "3x3";
                case ConcreteSlotValueType.Matrix4:
                    return "4x4";
                default:
                    return "Error";
            }
        }

        public static string ConvertConcreteSlotValueTypeToString(AbstractMaterialNode.OutputPrecision p, ConcreteSlotValueType slotValue)
        {
            switch (slotValue)
            {
                case ConcreteSlotValueType.Boolean:
                    return p.ToString();
                case ConcreteSlotValueType.Vector1:
                    return p.ToString();
                case ConcreteSlotValueType.Vector2:
                    return p + "2";
                case ConcreteSlotValueType.Vector3:
                    return p + "3";
                case ConcreteSlotValueType.Vector4:
                    return p + "4";
                case ConcreteSlotValueType.Texture2D:
                    return "Texture2D";
                case ConcreteSlotValueType.Cubemap:
                    return "Cubemap";
                case ConcreteSlotValueType.Gradient:
                    return "Gradient";
                case ConcreteSlotValueType.Matrix2:
                    return p + "2x2";
                case ConcreteSlotValueType.Matrix3:
                    return p + "3x3";
                case ConcreteSlotValueType.Matrix4:
                    return p + "4x4";
                case ConcreteSlotValueType.SamplerState:
                    return "SamplerState";
                default:
                    return "Error";
            }
        }

        public static string GetHLSLSafeName(string input)
        {
            char[] arr = input.ToCharArray();
            arr = Array.FindAll<char>(arr, (c => (Char.IsLetterOrDigit(c))));
            return new string(arr);
        }

        public static string FloatToShaderValue(float value)
        {
            if (Single.IsPositiveInfinity(value))
                return "1.#INF";
            else if (Single.IsNegativeInfinity(value))
                return "-1.#INF";
            else if (Single.IsNaN(value))
                return "NAN";
            else
            {
                 return value.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
