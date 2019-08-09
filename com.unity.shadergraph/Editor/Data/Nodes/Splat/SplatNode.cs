using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Splat", "Splat")]
    partial class SplatNode : AbstractMaterialNode, ISplatCountListener, IHasSettings, IGeneratesBodyCode, IGeneratesFunction,
        IMayRequirePosition, IMayRequireNormal, IMayRequireTangent, IMayRequireBitangent, IMayRequireVertexColor, IMayRequireMeshUV,
        IMayRequireViewDirection, IMayRequireScreenPosition, IMayRequireFaceSign,
        IMayRequireDepthTexture, IMayRequireCameraOpaqueTexture, IMayRequireTime
    {
        public SplatNode()
        {
            name = "Splat";
            UpdateNodeAfterDeserialization();
        }

        public override string title => $"{base.title} x{m_SplatCount}";

        public override bool hasPreview => true;

        public const int kBlendWeightInputSlotId = 0;
        public const int kConditionInputSlotId = 1;
        public const int kSplatInputSlotIdStart = 4;

        private IEnumerable<MaterialSlot> EnumerateSplatInputSlots()
            => this.GetInputSlots<MaterialSlot>();

        private IEnumerable<MaterialSlot> EnumerateSplatOutputSlots()
            => this.GetOutputSlots<MaterialSlot>();

        [SerializeField]
        private List<string> m_SplatSlotNames = new List<string>();

        [SerializeField]
        private int m_SplatCount = 4;

        [SerializeField]
        private bool m_Conditional = false;

        public bool Conditional
        {
            get => m_Conditional;
            set => m_Conditional = value;
        }

        public override void UpdateNodeAfterDeserialization()
        {
            CreateSplatSlots(null, null);
            AddBlendWeightAndCondtionSlots();

            var validSlots = Enumerable.Range(kSplatInputSlotIdStart, m_SplatSlotNames.Count * 2).Concat(new[] { kBlendWeightInputSlotId });
            if (m_Conditional)
                validSlots = validSlots.Concat(new[] { kConditionInputSlotId });
            RemoveSlotsNameNotMatching(validSlots);
        }

        void ISplatCountListener.OnSplatCountChange(int splatCount)
        {
            if (m_SplatCount != splatCount)
            {
                m_SplatCount = splatCount;
                Dirty(ModificationScope.Node);
            }
        }

        private struct SplatGraphNode
        {
            public AbstractMaterialNode Node;
            public int Order; // See CompileSplatGraph()
            public HashSet<int> DifferentiateOutputSlots;
        }

        private enum SplatFunctionInputType
        {
            Variable,
            SplatProperty,
            SplatArray
        }

        private struct SplatFunctionInput
        {
            public string VariableName;
            public string VariableType;
            public SplatFunctionInputType Type;

            // TODO: more general hashing
            public override int GetHashCode()
                => VariableName.GetHashCode() * 23 + VariableType.GetHashCode();
        }

        private struct SplatFunctionOutput
        {
            public string VariableName;
            public string VariableType;
            public bool IsSplatLoopNode;

            public override int GetHashCode()
                => VariableName.GetHashCode() * 23 + VariableType.GetHashCode();
        }

        private struct SplatGraphInputDerivatives
        {
            public string VariableName;
            public string VariableType;
            public Func<GenerationMode, string> VariableValue;
        }

        private class SplatGraph
        {
            public IReadOnlyList<SplatGraphNode> Nodes;
            public IReadOnlyList<IReadOnlyList<SplatFunctionInput>> SplatFunctionInputsPerOrder;
            public IReadOnlyList<IReadOnlyList<SplatFunctionOutput>> SplatFunctionOutputsPerOrder;
            public IReadOnlyList<SplatGraphInputDerivatives> InputDerivatives;
        }

        private SplatGraph m_SplatGraph;

        private bool RecurseBuildDifferentialFunction(MaterialSlot inputSlot, List<SplatGraphNode> splatNodes, List<SplatGraphInputDerivatives> inputDerivatives, HashSet<string> processedOutputSlots)
        {
            var edge = owner.GetEdges(inputSlot.slotReference).FirstOrDefault();
            var outputNode = edge != null ? owner.GetNodeFromGuid(edge.outputSlot.nodeGuid) : null;
            var splatNodeIndex = outputNode != null ? splatNodes.FindIndex(n => n.Node == outputNode) : -1;

            if (edge != null && outputNode == null)
            {
                owner.AddValidationError(inputSlot.owner.tempId, $"Internal error: Cannot traverse along input slot {inputSlot.id}");
                return false;
            }
            else if (outputNode != null && splatNodeIndex == -1)
            {
                owner.AddValidationError(outputNode.tempId, $"Internal error: the node is not collected by the splat graph.");
                return false;
            }

            var derivativeVarName = edge == null ? inputSlot.GetDefaultValueDerivative() : outputNode.GetVariableNameForSlot(edge.outputSlot.slotId);
            if (derivativeVarName == "0")
                return true; // constant - don't recurse further and don't add the input

            if (processedOutputSlots.Contains(derivativeVarName))
                return true; // this one has been solved

            processedOutputSlots.Add(derivativeVarName);

            // The slot is disconnected, or a non-splat dependent node is encountered:
            // Take the derivative, and stop chaining the inputs because they are inputs to the Splat function.
            if (splatNodeIndex < 0 || splatNodes[splatNodeIndex].Order < 0)
            {
                inputDerivatives.Add(new SplatGraphInputDerivatives()
                {
                    VariableName = derivativeVarName,
                    VariableType = inputSlot.concreteValueType.ToShaderString(inputSlot.owner.concretePrecision),
                    VariableValue = genMode => inputSlot.owner.GetSlotValue(inputSlot.id, genMode)
                });
                return true;
            }

            // Otherwise: apply the chainning rule to obtain a function of the derivatives of the inputs.
            // The node must be differentiable.
            var differentiable = outputNode as IDifferentiable;
            var derivative = differentiable?.GetDerivative(edge.outputSlot.slotId) ?? default(Derivative);
            if (derivative.FuncVariableInputSlotIds == null)
            {
                owner.AddValidationError(outputNode.tempId, $"Conditional texture sampling requires the node {outputNode.name} to be differentiable");
                return false;
            }

            for (int i = 0; i < derivative.FuncVariableInputSlotIds.Count; ++i)
            {
                var inputSlotId = derivative.FuncVariableInputSlotIds[i];
                var outputNodeInputSlot = outputNode.FindInputSlot<MaterialSlot>(inputSlotId);
                if (outputNodeInputSlot == null)
                {
                    owner.AddValidationError(outputNode.tempId, $"Internal error: Cannot find Slot {inputSlotId}");
                    return false;
                }

                if (!RecurseBuildDifferentialFunction(outputNodeInputSlot, splatNodes, inputDerivatives, processedOutputSlots))
                    return false;
            }

            splatNodes[splatNodeIndex].DifferentiateOutputSlots.Add(edge.outputSlot.slotId);
            return true;
        }

        private void ClearCompiledSplatGraph()
        {
            m_SplatGraph = null;
        }

        private bool CompileSplatGraph()
        {
            var inputSGNodes = new List<AbstractMaterialNode>();
            NodeUtils.DepthFirstCollectNodesFromNode(inputSGNodes, this, NodeUtils.IncludeSelf.Exclude, EnumerateSplatInputSlots().Select(slot => slot.id).ToList());

            // Determine the Order for each node:
            // - -1 for nodes that are independent on the splat loops.
            // - A node's initial Order is the max of all input nodes.
            // - A node that is an ISplatLoopNode increases its initial Order by 1.
            // Basically Order means which for-loop this node is in. Nodes with higher order have data dependency on previous loop results (sum, max, etc. on all splats).
            // Nodes of order -1 are out of any splat loop. They are calculated only once.
            var splatGraphNodes = new SplatGraphNode[inputSGNodes.Count];
            // Gather inputs and outputs from previous order loop.
            var splatFunctionInputsPerOrder = new List<HashSet<SplatFunctionInput>>();
            var splatFunctionOutputsPerOrder = new List<HashSet<SplatFunctionOutput>>();
            for (int i = 0; i < inputSGNodes.Count; ++i)
            {
                ref var splatGraphNode = ref splatGraphNodes[i];
                var node = splatGraphNode.Node = inputSGNodes[i];
                splatGraphNode.Order = -1;
                splatGraphNode.DifferentiateOutputSlots = new HashSet<int>();

                if (node is PropertyNode propertyNode)
                {
                    // Property node has no input. It should has an order of -1.
                    Debug.Assert(!node.GetInputSlots<MaterialSlot>().Any() && splatGraphNode.Order == -1);
                    // Only gather the splat properties. Regular properties are uniforms.
                    if (node.IsSplattingPropertyNode())
                    {
                        splatGraphNode.Order = 0;
                        GetSplatFunctionInputsOutputsForOrder(0).input.Add(new SplatFunctionInput()
                        {
                            VariableName = propertyNode.GetVariableNameForSlot(PropertyNode.OutputSlotId),
                            VariableType = propertyNode.shaderProperty.concreteShaderValueType.ToShaderString(propertyNode.concretePrecision),
                            Type = SplatFunctionInputType.SplatProperty
                        });
                    }
                    continue;
                }

                var inputSlots = node.GetInputSlots<MaterialSlot>().Select(slot =>
                {
                    var edge = node.owner.GetEdges(slot.slotReference).FirstOrDefault();
                    var otherNode = edge != null ? node.owner.GetNodeFromGuid(edge.outputSlot.nodeGuid) : null;
                    if (otherNode == null)
                        return (slot, null, -1);

                    var otherNodeOrder = splatGraphNodes[inputSGNodes.IndexOf(otherNode)].Order;
                    return (slot, otherSlot: otherNode.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId), otherNodeOrder);
                });

                splatGraphNode.Order = inputSlots.Aggregate(splatGraphNode.Order, (result, slot) => Mathf.Max(result, slot.otherNodeOrder + (slot.otherSlot?.owner is ISplatLoopNode || slot.otherSlot?.owner is ISplatUnpackNode ? 1 : 0)));

                // Gather the inputs from previous order.
                if (splatGraphNode.Order >= 0)
                {
                    var splatFunctionInputs = GetSplatFunctionInputsOutputsForOrder(splatGraphNode.Order).input;
                    foreach (var (inputSlot, otherSlot, otherNodeOrder) in inputSlots)
                    {
                        if (otherSlot == null)
                            continue;

                        if (otherNodeOrder < splatGraphNode.Order)
                        {
                            var varName = otherSlot.owner.GetVariableNameForSlot(otherSlot.id);
                            var varType = otherSlot.concreteValueType.ToShaderString(otherSlot.owner.concretePrecision);

                            var splatInputType = SplatFunctionInputType.Variable;
                            if (otherNodeOrder >= 0 && !(otherSlot.owner is ISplatLoopNode)
                                || otherNodeOrder < 0 && otherSlot.owner is ISplatUnpackNode)
                                splatInputType = SplatFunctionInputType.SplatArray;

                            splatFunctionInputs.Add(new SplatFunctionInput()
                            {
                                VariableName = varName,
                                VariableType = varType,
                                Type = splatInputType
                            });

                            if (otherNodeOrder >= 0)
                            {
                                GetSplatFunctionInputsOutputsForOrder(otherNodeOrder).output.Add(new SplatFunctionOutput()
                                {
                                    VariableName = varName,
                                    VariableType = varType,
                                    IsSplatLoopNode = otherSlot.owner is ISplatLoopNode
                                });
                            }
                        }
                    }
                }
            }

            // Find the orders that outputs to SplatNode.
            foreach (var inputSlot in EnumerateSplatInputSlots())
            {
                var edge = owner.GetEdges(guid, inputSlot.id).FirstOrDefault();
                var outputNode = edge != null ? owner.GetNodeFromGuid(edge.outputSlot.nodeGuid) : null;
                if (outputNode != null)
                {
                    var outputSlot = outputNode.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId);
                    var order = splatGraphNodes[inputSGNodes.IndexOf(outputNode)].Order;
                    if (order >= 0)
                    {
                        GetSplatFunctionInputsOutputsForOrder(order).output.Add(new SplatFunctionOutput()
                        {
                            VariableName = outputSlot.owner.GetVariableNameForSlot(outputSlot.id),
                            VariableType = outputSlot.concreteValueType.ToShaderString(concretePrecision),
                            IsSplatLoopNode = outputSlot.owner is ISplatLoopNode
                        });
                    }
                }
            }

            m_SplatGraph = new SplatGraph()
            {
                Nodes = splatGraphNodes.OrderBy(n => n.Order).ToArray(), // OrderBy performs stable sort
                SplatFunctionInputsPerOrder = splatFunctionInputsPerOrder.Select(hashset => hashset.ToArray()).ToArray(),
                SplatFunctionOutputsPerOrder = splatFunctionOutputsPerOrder.Select(hashset => hashset.ToArray()).ToArray(),
                InputDerivatives = new SplatGraphInputDerivatives[0]
            };

            return true;

            //var splatDependent = new bool[splatGraphNodes.Count];
            //var splatFunctionInputs = new HashSet<SplatGraphInput>(); 
            //var varToDifferentiate = new HashSet<MaterialSlot>();
            //for (int i = 0; i < splatGraphNodes.Count; ++i)
            //{
            //    var node = splatGraphNodes[i];
            //    if (node is PropertyNode propertyNode)
            //    {
            //        if (node.IsSplattingPropertyNode())
            //        {
            //            splatDependent[i] = true;
            //            splatFunctionInputs.Add(new SplatGraphInput()
            //            {
            //                VariableName = propertyNode.GetVariableNameForSlot(PropertyNode.OutputSlotId),
            //                VariableType = propertyNode.shaderProperty.concreteShaderValueType.ToShaderString(propertyNode.concretePrecision),
            //                SplatProperty = true
            //            });
            //        }
            //        continue;
            //    }
            //    else if (node is SplatNode splatNode)
            //    {
            //        // concatenated SplatNodes are not splat dependent (because the blended results are not per splat)
            //        splatDependent[i] = false;
            //    }

            //    var inputSlots = new List<MaterialSlot>();
            //    node.GetInputSlots(inputSlots);

            //    var outputSlots = new (bool splatDependent, MaterialSlot slot)[inputSlots.Count];
            //    for (int j = 0; j < inputSlots.Count; ++j)
            //    {
            //        var slot = inputSlots[j];
            //        var edge = node.owner.GetEdges(slot.slotReference).FirstOrDefault();
            //        var outputNode = edge != null ? node.owner.GetNodeFromGuid(edge.outputSlot.nodeGuid) : null;
            //        outputSlots[j] = (
            //            outputNode != null && splatDependent[splatGraphNodes.IndexOf(outputNode)],
            //            outputNode?.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId));
            //    }

            //    if (Conditional && node is IMayRequireDerivatives requireDerivatives)
            //    {
            //        foreach (var slotId in requireDerivatives.GetDifferentiatingInputSlotIds())
            //            varToDifferentiate.Add(node.FindInputSlot<MaterialSlot>(slotId));
            //    }

            //    splatDependent[i] = outputSlots.Any(v => v.splatDependent);
            //    if (splatDependent[i])
            //    {
            //        foreach (var (splat, slot) in outputSlots)
            //        {
            //            if (!splat && slot != null)
            //            {
            //                splatFunctionInputs.Add(new SplatGraphInput()
            //                {
            //                    VariableName = slot.owner.GetVariableNameForSlot(slot.id),
            //                    VariableType = slot.concreteValueType.ToShaderString(slot.owner.concretePrecision),
            //                    SplatProperty = false
            //                });
            //            }
            //        }
            //    }
            //}

            //var graphNodes = new List<SplatGraphNode>();
            //for (int i = 0; i < splatGraphNodes.Count; ++i)
            //{
            //    graphNodes.Add(new SplatGraphNode()
            //    {
            //        Node = splatGraphNodes[i],
            //        SplatDependent = splatDependent[i],
            //        DifferentiateOutputSlots = new HashSet<int>()
            //    });
            //}

            //var inputDerivatives = new List<SplatGraphInputDerivatives>();
            //var processedOutputSlots = new HashSet<string>();
            //foreach (var diffSlot in varToDifferentiate)
            //{
            //    if (!RecurseBuildDifferentialFunction(diffSlot, graphNodes, inputDerivatives, processedOutputSlots))
            //        return false;
            //}

            //m_SplatGraph = new SplatGraph()
            //{
            //    Nodes = splatGraphNodes,
            //    SplatFunctionInputs = splatFunctionInputs.ToList(),
            //    InputDerivatives = inputDerivatives
            //};

            // Local functions
            (HashSet<SplatFunctionInput> input, HashSet<SplatFunctionOutput> output) GetSplatFunctionInputsOutputsForOrder(int order)
            {
                Debug.Assert(order >= 0);
                for (int j = splatFunctionInputsPerOrder.Count; j <= order; ++j)
                {
                    splatFunctionInputsPerOrder.Add(new HashSet<SplatFunctionInput>());
                    splatFunctionOutputsPerOrder.Add(new HashSet<SplatFunctionOutput>());
                }
                return (splatFunctionInputsPerOrder[order], splatFunctionOutputsPerOrder[order]);
            }
        }

        public override void ValidateNode()
        {
            ClearCompiledSplatGraph();

            // AbstractMaterialNode takes all input dynamic vector slots and figure out one unified concrete type for all.
            // Splat inputs are all independent and we simply take the concrete type of the input slot and duplicate onto the corresponding output slot.
            hasError = false;
            foreach (var inputSlot in this.GetInputSlots<MaterialSlot>())
            {
                inputSlot.hasError = false;
                var edge = owner.GetEdges(inputSlot.slotReference).FirstOrDefault();
                if (edge == null)
                    continue;

                var outputNode = owner.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                if (outputNode == null)
                    continue;

                var outputSlot = outputNode.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId);
                if (outputSlot == null)
                    continue;

                if (outputSlot.hasError)
                {
                    inputSlot.hasError = true;
                    hasError = true;
                    continue;
                }

                if (inputSlot.id >= kSplatInputSlotIdStart && inputSlot is DynamicVectorMaterialSlot dynamicVector)
                    dynamicVector.SetConcreteType(outputSlot.concreteValueType);
            }

            foreach (var outputSlot in this.GetOutputSlots<MaterialSlot>())
            {
                if (hasError)
                {
                    outputSlot.hasError = true;
                    continue;
                }

                if (outputSlot is DynamicVectorMaterialSlot dynamicVector)
                {
                    var inputSlot = FindInputSlot<DynamicVectorMaterialSlot>(dynamicVector.id - 1);
                    if (inputSlot != null) // Could be null during the slot removing process
                        dynamicVector.SetConcreteType(inputSlot.concreteValueType);
                }
            }

            var errorMessage = k_validationErrorMessage;
            hasError = CalculateNodeHasError(ref errorMessage) || hasError;
            hasError = ValidateConcretePrecision(ref errorMessage) || hasError;

            if (hasError)
            {
                owner.AddValidationError(tempId, errorMessage);
                return;
            }

            if (CompileSplatGraph())
                ++version;
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            if (m_SplatGraph == null)
                return;

            // Generate code for non-splat part.
            foreach (var node in m_SplatGraph.Nodes)
            {
                if (node.Order >= 0)
                    break;

                sb.currentNode = node.Node;

                if (node.Node is ISplatLoopNode splatLoopNode)
                    splatLoopNode.GenerateSetupCode(sb, graphContext, generationMode);

                if (node.Node is IGeneratesBodyCode bodyCode)
                    bodyCode.GenerateNodeCode(sb, graphContext, generationMode);

                sb.ReplaceInCurrentMapping(PrecisionUtil.Token, node.Node.concretePrecision.ToShaderString());
            }

            // Call SplatFunctions by order.
            for (int order = 0; order < m_SplatGraph.SplatFunctionInputsPerOrder.Count; ++order)
            {
                foreach (var splatOutput in m_SplatGraph.SplatFunctionOutputsPerOrder[order].Where(v => !v.IsSplatLoopNode))
                    sb.AppendLine($"{splatOutput.VariableType} {splatOutput.VariableName}[{m_SplatCount}];");
                foreach (var splatLoopNode in m_SplatGraph.Nodes.Where(n => n.Order == order && n.Node is ISplatLoopNode))
                    (splatLoopNode.Node as ISplatLoopNode).GenerateSetupCode(sb, graphContext, generationMode);

                for (int splat = 0; splat < m_SplatCount; ++splat)
                {
                    if (splat == 4)
                    {
                        sb.AppendLine($"#ifdef {GraphData.kSplatCount8Keyword}");
                        sb.IncreaseIndent();
                    }

                    // TODO:
                    //// Generate "if (condition)" for conditional.
                    //if (m_Conditional)
                    //{
                    //    var condition = $"{(i < 4 ? "1" : "1")}.{"xyzw"[i % 4]}";
                    //    sb.AppendLine($"UNITY_BRANCH if ({condition} > 0)");
                    //    sb.AppendLine("{");
                    //    sb.IncreaseIndent();
                    //}

                    sb.AppendIndentation();
                    sb.Append($"SplatFunction_{GetVariableNameForNode()}_Order{order}(IN");
                    foreach (var splatInput in m_SplatGraph.SplatFunctionInputsPerOrder[order])
                    {
                        var varName = splatInput.VariableName;
                        if (splatInput.Type == SplatFunctionInputType.SplatProperty)
                            varName = $"{varName.Substring(0, varName.Length - 1)}{splat}";
                        else if (splatInput.Type == SplatFunctionInputType.SplatArray)
                            varName = $"{varName}[{splat}]";
                        sb.Append($", {varName}");
                    }
                    foreach (var derivative in m_SplatGraph.InputDerivatives)
                        sb.Append($", ddx_{derivative.VariableName}, ddy_{derivative.VariableName}");
                    foreach (var splatOutput in m_SplatGraph.SplatFunctionOutputsPerOrder[order])
                        sb.Append($", {splatOutput.VariableName}{(splatOutput.IsSplatLoopNode ? string.Empty : $"[{splat}]")}");
                    sb.Append(");");
                    sb.AppendNewLine();

                    // TODO:
                    //// If conditional: assign the default values if condition is not met.
                    //if (m_Conditional)
                    //{
                    //    sb.DecreaseIndent();
                    //    sb.AppendLine("}");
                    //    sb.AppendLine("else");
                    //    sb.AppendLine("{");
                    //    sb.IncreaseIndent();
                    //    foreach (var slot in EnumerateSplatOutputSlots())
                    //        sb.AppendLine($"{GetVariableNameForSlot(slot.id)}_splats[{i}] = {FindInputSlot<MaterialSlot>(slot.id - 1).GetDefaultValue(generationMode)};");
                    //    sb.DecreaseIndent();
                    //    sb.AppendLine("}");
                    //}

                    if (splat == 7)
                    {
                        sb.DecreaseIndent();
                        sb.AppendLine("#endif");
                    }
                }
            }

            // Blend together

            //// Generate input derivatives if any.
            //foreach (var derivative in m_SplatGraph.InputDerivatives)
            //{
            //    sb.AppendLine($"{derivative.VariableType} ddx_{derivative.VariableName} = ddx({derivative.VariableValue(generationMode)});");
            //    sb.AppendLine($"{derivative.VariableType} ddy_{derivative.VariableName} = ddy({derivative.VariableValue(generationMode)});");
            //}

            //var blendWeights0 = GetSlotValue(kBlendWeights0InputSlotId, generationMode);
            //var blendWeights1 = m_SplatCount > 4 ? GetSlotValue(kBlendWeights1InputSlotId, generationMode) : string.Empty;
            //var conditions0 = m_Conditional ? GetSlotValue(kConditions0InputSlotId, generationMode) : blendWeights0;
            //var conditions1 = m_Conditional && m_SplatCount > 4 ? GetSlotValue(kConditions1InputSlotId, generationMode) : blendWeights1;

            //// For each splat: call the splat function
            //for (int i = 0; i < m_SplatCount; ++i)
            //{
            //    if (i == 4)
            //    {
            //        sb.AppendLine($"#ifdef {GraphData.kSplatCount8Keyword}");
            //        sb.IncreaseIndent();
            //    }

            //    // Generate "if (condition)" for conditional.
            //    if (m_Conditional)
            //    {
            //        var condition = $"{(i < 4 ? "1" : "1")}.{"xyzw"[i % 4]}";
            //        sb.AppendLine($"UNITY_BRANCH if ({condition} > 0)");
            //        sb.AppendLine("{");
            //        sb.IncreaseIndent();
            //    }

            //    sb.AppendIndentation();
            //    sb.Append($"SplatFunction_{GetVariableNameForNode()}(IN");
            //    foreach (var splatInput in m_SplatGraph.SplatFunctionInputs)
            //    {
            //        var varName = splatInput.VariableName;
            //        if (splatInput.SplatProperty)
            //            varName = $"{varName.Substring(0, varName.Length - 1)}{i}";
            //        sb.Append($", {varName}");
            //    }
            //    foreach (var derivative in m_SplatGraph.InputDerivatives)
            //        sb.Append($", ddx_{derivative.VariableName}, ddy_{derivative.VariableName}");
            //    foreach (var slot in EnumerateSplatOutputSlots())
            //        sb.Append($", {GetVariableNameForSlot(slot.id)}_splats[{i}]");
            //    sb.Append(");");
            //    sb.AppendNewLine();

            //    // If conditional: assign the default values if condition is not met.
            //    if (m_Conditional)
            //    {
            //        sb.DecreaseIndent();
            //        sb.AppendLine("}");
            //        sb.AppendLine("else");
            //        sb.AppendLine("{");
            //        sb.IncreaseIndent();
            //        foreach (var slot in EnumerateSplatOutputSlots())
            //            sb.AppendLine($"{GetVariableNameForSlot(slot.id)}_splats[{i}] = {FindInputSlot<MaterialSlot>(slot.id - 1).GetDefaultValue(generationMode)};");
            //        sb.DecreaseIndent();
            //        sb.AppendLine("}");
            //    }

            //    if (i == 7)
            //    {
            //        sb.DecreaseIndent();
            //        sb.AppendLine("#endif");
            //    }
            //}

            // Generate actual weight applying code
            foreach (var slot in EnumerateSplatOutputSlots())
                sb.AppendLine($"{slot.concreteValueType.ToShaderString()} {GetVariableNameForSlot(slot.id)} = 0;");

            var inputWeightValue = GetSplatInputValueFormat(kBlendWeightInputSlotId);
            var assignStatements = Enumerable.Range(0, m_SplatSlotNames.Count).Select(i => $"{GetVariableNameForSlot(kSplatInputSlotIdStart + i * 2 + 1)} += {GetSplatInputValueFormat(i * 2 + kSplatInputSlotIdStart)} * {inputWeightValue};");

            for (int i = 0; i < m_SplatCount; ++i)
            {
                if (i == 4)
                {
                    sb.AppendLine($"#ifdef {GraphData.kSplatCount8Keyword}");
                    sb.IncreaseIndent();
                }

                foreach (var assign in assignStatements)
                    sb.AppendLine(string.Format(assign, i));

                if (i == 7)
                {
                    sb.DecreaseIndent();
                    sb.AppendLine("#endif");
                }
            }

            string GetSplatInputValueFormat(int splatInputSlotId)
            {
                var splatInputSlot = FindInputSlot<MaterialSlot>(splatInputSlotId);
                var edge = owner.GetEdges(splatInputSlot.slotReference).FirstOrDefault();
                var fromNode = edge != null ? owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid) : null;
                if (fromNode == null)
                    return splatInputSlot.GetDefaultValue(generationMode);

                bool isArray;
                if (fromNode is SplatUnpackNode)
                    isArray = true;
                else if (fromNode is ISplatLoopNode)
                    isArray = false;
                else
                    isArray = m_SplatGraph.Nodes.Where(n => n.Node == fromNode).First().Order >= 0;
                return ShaderGenerator.ConvertNodeOutputValue($"{fromNode.GetVariableNameForSlot(edge.outputSlot.slotId)}{(isArray ? "[{0}]" : string.Empty)}", fromNode.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId).concreteValueType, splatInputSlot.concreteValueType);
            }
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            if (m_SplatGraph == null)
                return;

            if (m_Conditional && !graphContext.conditional)
                graphContext = new GraphContext(graphContext.graphInputStructName, graphContext.splatCount, m_Conditional);

            // Generate global functions from splat graph.
            foreach (var node in m_SplatGraph.Nodes)
            {
                if (node.Node is IGeneratesFunction functionNode)
                {
                    registry.builder.currentNode = node.Node;
                    functionNode.GenerateNodeFunction(registry, graphContext, generationMode);
                    registry.builder.ReplaceInCurrentMapping(PrecisionUtil.Token, node.Node.concretePrecision.ToShaderString());
                }
            }

            // Generate the splat functions from splat graph.
            for (int order = 0; order < m_SplatGraph.SplatFunctionInputsPerOrder.Count; ++order)
            {
                var splatFunction = new ShaderStringBuilder();
                splatFunction.Append($"void SplatFunction_{GetVariableNameForNode()}_Order{order}({graphContext.graphInputStructName} IN");

                // splat function inputs
                foreach (var splatInput in m_SplatGraph.SplatFunctionInputsPerOrder[order])
                    splatFunction.Append($", {splatInput.VariableType} {splatInput.VariableName}");

                // splat function input derivatives
                foreach (var d in m_SplatGraph.InputDerivatives)
                    splatFunction.Append($", {d.VariableType} ddx_{d.VariableName}, {d.VariableType} ddy_{d.VariableName}");

                // splat function output: either inout param for ISplatLoopNode, or out param for regular output
                foreach (var splatOutput in m_SplatGraph.SplatFunctionOutputsPerOrder[order])
                    splatFunction.Append($", {(splatOutput.IsSplatLoopNode ? "inout" : "out")} {splatOutput.VariableType} {(splatOutput.IsSplatLoopNode ? string.Empty : "out")}{splatOutput.VariableName}");
                splatFunction.AppendLine(")");

                using (splatFunction.BlockScope())
                {
                    foreach (var node in m_SplatGraph.Nodes.Where(n => n.Order == order))
                    {
                        splatFunction.currentNode = node.Node;

                        if (node.Node is IGeneratesBodyCode bodyNode)
                            bodyNode.GenerateNodeCode(splatFunction, graphContext, generationMode);

                        var differentiable = node.Node as IDifferentiable;
                        foreach (var differentiateSlot in node.DifferentiateOutputSlots)
                        {
                            var derivative = differentiable.GetDerivative(differentiateSlot);
                            var funcVars = new (string ddx, string ddy)[derivative.FuncVariableInputSlotIds.Count];
                            for (int i = 0; i < derivative.FuncVariableInputSlotIds.Count; ++i)
                            {
                                var inputSlotId = derivative.FuncVariableInputSlotIds[i];
                                var inputSlot = node.Node.FindInputSlot<MaterialSlot>(inputSlotId);
                                var (value, ddx, ddy) = node.Node.GetSlotValueWithDerivative(inputSlotId, generationMode);
                                funcVars[i] = (ddx, ddy);
                            }
                            var outputSlot = node.Node.FindOutputSlot<MaterialSlot>(differentiateSlot);
                            var typeString = outputSlot.concreteValueType.ToShaderString(node.Node.concretePrecision);
                            var ddxVar = $"ddx_{node.Node.GetVariableNameForSlot(differentiateSlot)} = {string.Format(derivative.Function(generationMode), funcVars.Select(v => v.ddx).ToArray())}";
                            var ddyVar = $"ddy_{node.Node.GetVariableNameForSlot(differentiateSlot)} = {string.Format(derivative.Function(generationMode), funcVars.Select(v => v.ddy).ToArray())}";
                            if (ddxVar.Length > 60)
                            {
                                splatFunction.AppendLine($"{typeString} {ddxVar};");
                                splatFunction.AppendLine($"{typeString} {ddyVar};");
                            }
                            else
                                splatFunction.AppendLine($"{typeString} {ddxVar}, {ddyVar};");
                        }

                        splatFunction.ReplaceInCurrentMapping(PrecisionUtil.Token, node.Node.concretePrecision.ToShaderString());
                    }

                    // Assign the output from nodes to the out parameters.
                    foreach (var splatOutput in m_SplatGraph.SplatFunctionOutputsPerOrder[order].Where(output => !output.IsSplatLoopNode))
                        splatFunction.AppendLine($"out{splatOutput.VariableName} = {splatOutput.VariableName};");
                }

                registry.ProvideFunction($"SplatFunction_{GetVariableNameForNode()}_Order{order}", s => s.Concat(splatFunction));
            }
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            if (m_SplatGraph != null)
            {
                foreach (var node in m_SplatGraph.Nodes)
                    node.Node.CollectPreviewMaterialProperties(properties);
            }
            base.CollectPreviewMaterialProperties(properties);
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (m_SplatGraph != null)
            {
                foreach (var node in m_SplatGraph.Nodes)
                    node.Node.CollectShaderProperties(properties, generationMode);
            }

            // base.CollectShaderProperties collect properties only for disconnected slots, so that in preview mode these values
            // are not baked into shader source.
            // In Conditional mode we actually want to collect default values from connected slots as well.
            if (m_Conditional)
            {
                // TODO: how about CollectPreviewMaterialProeprties?
                foreach (var inputSlot in EnumerateSplatInputSlots())
                    inputSlot.AddDefaultProperty(properties, generationMode);
                return;
            }

            base.CollectShaderProperties(properties, generationMode);
        }

        NeededCoordinateSpace IMayRequirePosition.RequiresPosition(ShaderStageCapability stageCapability)
            => m_SplatGraph != null ? m_SplatGraph.Nodes.Select(v => v.Node).OfType<IMayRequirePosition>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresPosition(stageCapability)) : NeededCoordinateSpace.None;

        NeededCoordinateSpace IMayRequireNormal.RequiresNormal(ShaderStageCapability stageCapability)
            => m_SplatGraph != null ? m_SplatGraph.Nodes.Select(v => v.Node).OfType<IMayRequireNormal>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresNormal(stageCapability)) : NeededCoordinateSpace.None;

        NeededCoordinateSpace IMayRequireTangent.RequiresTangent(ShaderStageCapability stageCapability)
            => m_SplatGraph != null ? m_SplatGraph.Nodes.Select(v => v.Node).OfType<IMayRequireTangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresTangent(stageCapability)) : NeededCoordinateSpace.None;

        NeededCoordinateSpace IMayRequireBitangent.RequiresBitangent(ShaderStageCapability stageCapability)
            => m_SplatGraph != null ? m_SplatGraph.Nodes.Select(v => v.Node).OfType<IMayRequireBitangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresBitangent(stageCapability)) : NeededCoordinateSpace.None;

        bool IMayRequireVertexColor.RequiresVertexColor(ShaderStageCapability stageCapability)
            => m_SplatGraph != null && m_SplatGraph.Nodes.Select(v => v.Node).OfType<IMayRequireVertexColor>().Any(node => node.RequiresVertexColor());

        bool IMayRequireMeshUV.RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
            => m_SplatGraph != null && m_SplatGraph.Nodes.Select(v => v.Node).OfType<IMayRequireMeshUV>().Any(node => node.RequiresMeshUV(channel));

        NeededCoordinateSpace IMayRequireViewDirection.RequiresViewDirection(ShaderStageCapability stageCapability)
            => m_SplatGraph != null ? m_SplatGraph.Nodes.Select(v => v.Node).OfType<IMayRequireViewDirection>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresViewDirection(stageCapability)) : NeededCoordinateSpace.None;

        bool IMayRequireScreenPosition.RequiresScreenPosition(ShaderStageCapability stageCapability)
            => m_SplatGraph != null && m_SplatGraph.Nodes.Select(v => v.Node).OfType<IMayRequireScreenPosition>().Any(node => node.RequiresScreenPosition());

        bool IMayRequireFaceSign.RequiresFaceSign(ShaderStageCapability stageCapability)
            => m_SplatGraph != null && m_SplatGraph.Nodes.Select(v => v.Node).OfType<IMayRequireFaceSign>().Any(node => node.RequiresFaceSign());

        bool IMayRequireDepthTexture.RequiresDepthTexture(ShaderStageCapability stageCapability)
            => m_SplatGraph != null && m_SplatGraph.Nodes.Select(v => v.Node).OfType<IMayRequireDepthTexture>().Any(node => node.RequiresDepthTexture());

        bool IMayRequireCameraOpaqueTexture.RequiresCameraOpaqueTexture(ShaderStageCapability stageCapability)
            => m_SplatGraph != null && m_SplatGraph.Nodes.Select(v => v.Node).OfType<IMayRequireCameraOpaqueTexture>().Any(node => node.RequiresCameraOpaqueTexture());

        bool IMayRequireTime.RequiresTime()
            => m_SplatGraph != null && m_SplatGraph.Nodes.Select(v => v.Node).OfType<IMayRequireTime>().Any(node => node.RequiresTime());
    }
}
