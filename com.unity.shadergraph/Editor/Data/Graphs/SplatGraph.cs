using System.Linq;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    partial class SplatGraph
    {
        private readonly struct Node
        {
            public readonly AbstractMaterialNode SgNode;
            public readonly bool OutputsPerSplat;
            public readonly bool RunsPerSplat;
            public readonly int Order;
            //public readonly HashSet<int> DifferentiateOutputSlots;

            public Node(AbstractMaterialNode sgNode, bool outputsPerSplat, bool runsPerSplat, int order)
            {
                SgNode = sgNode;
                OutputsPerSplat = outputsPerSplat;
                RunsPerSplat = runsPerSplat;
                Order = order;
            }
        }

        private enum SplatFunctionInputType
        {
            Variable,
            SplatProperty,
            SplatArray
        }

        private readonly struct SplatFunctionInputVariable
        {
            public readonly string Name;
            public readonly string ShaderTypeString;
            public readonly SplatFunctionInputType Type;

            public SplatFunctionInputVariable(string name, string shaderTypeString, SplatFunctionInputType type)
            {
                Name = name;
                ShaderTypeString = shaderTypeString;
                Type = type;
            }

            // TODO: more general hashing
            public override int GetHashCode()
                => Name.GetHashCode() * 23 + ShaderTypeString.GetHashCode();
        }

        private readonly struct SplatFunctionOutputVariable
        {
            public readonly string Name;
            public readonly string ShaderTypeString;
            public readonly bool InOut;

            public SplatFunctionOutputVariable(string name, string shaderTypeString, bool inOut)
            {
                Name = name;
                ShaderTypeString = shaderTypeString;
                InOut = inOut;
            }

            public override int GetHashCode()
                => Name.GetHashCode() * 23 + ShaderTypeString.GetHashCode();
        }

        //private struct SplatFunctionInputVariableDerivative
        //{
        //    public string Name;
        //    public string ShaderTypeString;
        //    public Func<GenerationMode, string> Value;
        //}

        private readonly struct Order
        {
            public readonly Node[] Nodes;
            public readonly SplatFunctionInputVariable[] InputVars;
            public readonly SplatFunctionOutputVariable[] OutputVars;

            public Order(Node[] nodes, SplatFunctionInputVariable[] inputVars, SplatFunctionOutputVariable[] outputVars)
            {
                Nodes = nodes;
                InputVars = inputVars;
                OutputVars = outputVars;
            }
        }

        private Order[] m_Orders = null;

        // TODO: derivative tracing
        //private SplatFunctionInputVariableDerivative[] m_InputDerivatives = null;

        public static SplatGraph Compile(IReadOnlyList<AbstractMaterialNode> shaderGraphNodes)
        {
            var splatGraphNodes = new Node[shaderGraphNodes.Count];
            var splatFunctionInputsPerOrder = new List<HashSet<SplatFunctionInputVariable>>();
            var splatFunctionOutputsPerOrder = new List<HashSet<SplatFunctionOutputVariable>>();

            for (int i = 0; i < shaderGraphNodes.Count; ++i)
            {
                var node = shaderGraphNodes[i];
                var inputSlots = node.GetInputSlots<MaterialSlot>().Select(slot =>
                {
                    var edge = node.owner.GetEdges(slot.slotReference).FirstOrDefault();
                    var fromNode = edge != null ? node.owner.GetNodeFromGuid(edge.outputSlot.nodeGuid) : null;
                    var fromNodeIndex = FindNodeIndex(fromNode);
                    // Make sure fromNodeIndex >= 0 and fromSlot != null is the same check.
                    Debug.Assert(fromNodeIndex < 0 || fromNode.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId) != null);
                    return (slot, fromSlot: fromNodeIndex >= 0 ? fromNode.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId) : null, fromNodeIndex);
                });

                // Determines if the node outputs per-splat values.
                // (The process perhaps need to be evaluated for each individual output slot, but:
                //  1. ISplatLoopNode currently has only 1 output.
                //  2. PropertyNode/ISplatUnpackNode has only 1 ouptut.
                //  3. Regular nodes are agnostic to splatting so we consider their outputs are either all splatting or non-splatting.
                //  In future we might introduce nodes that output both splatting and non-splatting values.)
                // Meanwhile determines if the node runs for each splat.
                bool outputsPerSplat, runsPerSplat;
                if (node is ISplatLoopNode)
                {
                    outputsPerSplat = false;
                    runsPerSplat = inputSlots.Any(s => s.fromNodeIndex != -1 && splatGraphNodes[s.fromNodeIndex].OutputsPerSplat);
                }
                else if (node is ISplatUnpackNode || node.IsSplattingPropertyNode())
                {
                    outputsPerSplat = true;
                    runsPerSplat = false;
                }
                else
                {
                    runsPerSplat = outputsPerSplat = inputSlots.Any(s => s.fromNodeIndex != -1 && splatGraphNodes[s.fromNodeIndex].OutputsPerSplat);
                }

                // Compute the order of the node.
                // Each order is a series of non-splatting operations followed by another series of splatting operations that don't
                // depend on the splatting data from the same order.
                // The order value is therefore only incremented if we go from splatting to non-splatting i.e. after an ISplatLoopNode
                // AND this ISplatLoopNode has splatting inputs.
                var order = inputSlots.Aggregate(0, (result, slot) =>
                {
                    var inputOrder = slot.fromNodeIndex >= 0 ? splatGraphNodes[slot.fromNodeIndex].Order : 0;
                    if (slot.fromNodeIndex >= 0 && slot.fromSlot.owner is ISplatLoopNode && splatGraphNodes[slot.fromNodeIndex].RunsPerSplat)
                        ++inputOrder;
                    return Mathf.Max(result, inputOrder);
                });

                splatGraphNodes[i] = new Node(node, outputsPerSplat, runsPerSplat, order);

                var (inputs, outputs) = GetSplatFunctionInputOutputVarsForOrder(order);
                foreach (var (inputSlot, fromSlot, fromNodeIndex) in inputSlots)
                {
                    // Disconnect slots have default values - either constants, uniforms or SurfaceDescription.
                    if (fromSlot == null)
                        continue;

                    var varName = fromSlot.owner.GetVariableNameForSlot(fromSlot.id);
                    var shaderType = fromSlot.concreteValueType.ToShaderString(fromSlot.owner.concretePrecision);

                    var fromSplatNode = splatGraphNodes[fromNodeIndex];
                    Debug.Assert(fromSplatNode.Order <= order);

                    if (runsPerSplat
                        && !fromSlot.owner.IsNonSplattingPropertyNode()
                        && (fromSplatNode.Order < order         // Inputs from previous orders
                            || !fromSplatNode.RunsPerSplat))    // Inputs from the current order's scalar part
                    {
                        var splatInputType = SplatFunctionInputType.Variable;
                        if (fromSlot.owner.IsSplattingPropertyNode())
                            splatInputType = SplatFunctionInputType.SplatProperty;
                        else if (fromSplatNode.OutputsPerSplat)
                            splatInputType = SplatFunctionInputType.SplatArray;
                        inputs.Add(new SplatFunctionInputVariable(varName, shaderType, splatInputType));
                    }

                    if (fromSplatNode.RunsPerSplat && fromSplatNode.Order < order)
                    {
                        GetSplatFunctionInputOutputVarsForOrder(fromSplatNode.Order).outputs
                            .Add(new SplatFunctionOutputVariable(varName, shaderType, fromSlot.owner is ISplatLoopNode));
                    }
                }
            }

            return new SplatGraph()
            {
                m_Orders = Enumerable.Range(0, splatFunctionInputsPerOrder.Count).Select(order => new Order(
                    splatGraphNodes.Where(n => n.Order == order).ToArray(),
                    splatFunctionInputsPerOrder[order].ToArray(),
                    splatFunctionOutputsPerOrder[order].ToArray())
                ).ToArray()
            };

            // Local functions
            (HashSet<SplatFunctionInputVariable> inputs, HashSet<SplatFunctionOutputVariable> outputs) GetSplatFunctionInputOutputVarsForOrder(int order)
            {
                Debug.Assert(order >= 0);
                for (int j = splatFunctionInputsPerOrder.Count; j <= order; ++j)
                {
                    splatFunctionInputsPerOrder.Add(new HashSet<SplatFunctionInputVariable>());
                    splatFunctionOutputsPerOrder.Add(new HashSet<SplatFunctionOutputVariable>());
                }
                return (splatFunctionInputsPerOrder[order], splatFunctionOutputsPerOrder[order]);
            }

            int FindNodeIndex(AbstractMaterialNode node)
            {
                for (int i = 0; i < shaderGraphNodes.Count; ++i)
                {
                    if (shaderGraphNodes[i] == node)
                        return i;
                }
                return -1;
            }
        }
    }
}
