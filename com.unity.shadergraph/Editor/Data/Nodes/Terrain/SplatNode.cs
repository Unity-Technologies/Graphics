using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Terrain", "Splat")]
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

        public const int kBlendWeight0InputSlotId = 0;
        public const int kBlendWeight1InputSlotId = 1;
        public const int kSplatInputSlotIdStart = 4;

        private IEnumerable<MaterialSlot> EnumerateSplatInputSlots()
            => this.GetInputSlots<MaterialSlot>().Where(slot => slot.id >= kSplatInputSlotIdStart);

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

        public override IEnumerable<ISlot> GetInputSlotsForGraphGeneration()
        {
            yield return FindInputSlot<ISlot>(kBlendWeight0InputSlotId);
            if (m_SplatCount > 4)
                yield return FindInputSlot<ISlot>(kBlendWeight1InputSlotId);
            // When generating shader grpah code, don't traverse further along the splat input slots.
            yield break;
        }

        public override void UpdateNodeAfterDeserialization()
        {
            CreateSplatSlots(null, null);
            var blendWeight0Name = m_SplatCount > 4 ? "Blend Weights 0" : "Blend Weights";
            AddSlot(new Vector4MaterialSlot(kBlendWeight0InputSlotId, blendWeight0Name, "BlendWeights0", SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));
            if (m_SplatCount > 4)
                AddSlot(new Vector4MaterialSlot(kBlendWeight1InputSlotId, "Blend Weights 1", "BlendWeights1", SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));
            RemoveSlotsNameNotMatching(Enumerable.Range(0, m_SplatSlotNames.Count * 2 + kSplatInputSlotIdStart));
        }

        public void OnSplatCountChange(int splatCount)
        {
            if (splatCount != m_SplatCount)
            {
                m_SplatCount = splatCount;
                if (m_SplatCount <= 4 && FindInputSlot<ISlot>(kBlendWeight1InputSlotId) != null)
                {
                    RemoveSlot(kBlendWeight1InputSlotId);

                    var blendWeight0Edges = owner.GetEdges(new SlotReference(guid, kBlendWeight0InputSlotId));
                    AddSlot(new Vector4MaterialSlot(kBlendWeight0InputSlotId, "Blend Weights", "BlendWeights0", SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));
                    foreach (var edge in blendWeight0Edges)
                        owner.Connect(edge.outputSlot, edge.inputSlot);
                }
                else if (m_SplatCount > 4 && FindInputSlot<ISlot>(kBlendWeight1InputSlotId) == null)
                {
                    var blendWeight0Edges = owner.GetEdges(new SlotReference(guid, kBlendWeight0InputSlotId));
                    AddSlot(new Vector4MaterialSlot(kBlendWeight0InputSlotId, "Blend Weights 0", "BlendWeights0", SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));
                    foreach (var edge in blendWeight0Edges)
                        owner.Connect(edge.outputSlot, edge.inputSlot);

                    AddSlot(new Vector4MaterialSlot(kBlendWeight1InputSlotId, "Blend Weights 1", "BlendWeights1", SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));
                }
                Dirty(ModificationScope.Node);
            }
        }

        private struct SplatGraphNode
        {
            public AbstractMaterialNode Node;
            public bool SplatDependent;
            public HashSet<int> DifferentiateOutputSlots;
        }

        private struct SplatGraphInput
        {
            public string VariableName;
            public string VariableType;
            public bool SplatProperty;

            // TODO: more general hashing
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
            public IReadOnlyList<SplatGraphInput> SplatFunctionInputs;
            public IReadOnlyList<SplatGraphInputDerivatives> InputDerivatives;
        }

        private SplatGraph m_SplatGraph;

        private bool FindSplatGraphNode(AbstractMaterialNode node, IReadOnlyList<SplatGraphNode> splatNodes, out SplatGraphNode result)
        {
            foreach (var i in splatNodes)
            {
                if (i.Node == node)
                {
                    result = i;
                    return true;
                }
            }

            result = default(SplatGraphNode);
            return false;
        }

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
            if (splatNodeIndex < 0 || !splatNodes[splatNodeIndex].SplatDependent)
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
            var splatGraphNodes = new List<AbstractMaterialNode>();
            NodeUtils.DepthFirstCollectNodesFromNode(splatGraphNodes, this, NodeUtils.IncludeSelf.Exclude, EnumerateSplatInputSlots().Select(slot => slot.id).ToList());

            // Determine for each node if it's dependent on the splat properties.
            var splatDependent = new bool[splatGraphNodes.Count];
            var splatFunctionInputs = new HashSet<SplatGraphInput>(); // gather the output slots of splat properties and non-splat inputs
            var varToDifferentiate = new HashSet<MaterialSlot>();
            for (int i = 0; i < splatGraphNodes.Count; ++i)
            {
                var node = splatGraphNodes[i];
                if (node is PropertyNode propertyNode)
                {
                    if (propertyNode.shaderProperty is ISplattableShaderProperty splatProperty && splatProperty.splat)
                    {
                        splatDependent[i] = true;
                        splatFunctionInputs.Add(new SplatGraphInput()
                        {
                            VariableName = propertyNode.GetVariableNameForSlot(PropertyNode.OutputSlotId),
                            VariableType = propertyNode.shaderProperty.concreteShaderValueType.ToShaderString(propertyNode.concretePrecision),
                            SplatProperty = true
                        });
                    }
                    continue;
                }
                else if (node is SplatNode splatNode)
                {
                    // concatenated SplatNodes are not splat dependent (because the blended results are not per splat)
                    splatDependent[i] = false;
                }

                var inputSlots = new List<MaterialSlot>();
                node.GetInputSlots(inputSlots);

                var outputSlots = new (bool splatDependent, MaterialSlot slot)[inputSlots.Count];
                for (int j = 0; j < inputSlots.Count; ++j)
                {
                    var slot = inputSlots[j];
                    var edge = node.owner.GetEdges(slot.slotReference).FirstOrDefault();
                    var outputNode = edge != null ? node.owner.GetNodeFromGuid(edge.outputSlot.nodeGuid) : null;
                    outputSlots[j] = (
                        outputNode != null && splatDependent[splatGraphNodes.IndexOf(outputNode)],
                        outputNode?.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId));
                }

                if (Conditional && node is IMayRequireDerivatives requireDerivatives)
                {
                    foreach (var slotId in requireDerivatives.GetDifferentiatingInputSlotIds())
                        varToDifferentiate.Add(node.FindInputSlot<MaterialSlot>(slotId));
                }

                splatDependent[i] = outputSlots.Any(v => v.splatDependent);
                if (splatDependent[i])
                {
                    foreach (var (splat, slot) in outputSlots)
                    {
                        if (!splat && slot != null)
                        {
                            splatFunctionInputs.Add(new SplatGraphInput()
                            {
                                VariableName = slot.owner.GetVariableNameForSlot(slot.id),
                                VariableType = slot.concreteValueType.ToShaderString(slot.owner.concretePrecision),
                                SplatProperty = false
                            });
                        }
                    }
                }
            }

            var graphNodes = new List<SplatGraphNode>();
            for (int i = 0; i < splatGraphNodes.Count; ++i)
            {
                graphNodes.Add(new SplatGraphNode()
                {
                    Node = splatGraphNodes[i],
                    SplatDependent = splatDependent[i],
                    DifferentiateOutputSlots = new HashSet<int>()
                });
            }

            var inputDerivatives = new List<SplatGraphInputDerivatives>();
            var processedOutputSlots = new HashSet<string>();
            foreach (var diffSlot in varToDifferentiate)
                RecurseBuildDifferentialFunction(diffSlot, graphNodes, inputDerivatives, processedOutputSlots);

            m_SplatGraph = new SplatGraph()
            {
                Nodes = graphNodes,
                SplatFunctionInputs = splatFunctionInputs.ToList(),
                InputDerivatives = inputDerivatives
            };
            return true;
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

            foreach (var node in m_SplatGraph.Nodes)
            {
                if (!node.SplatDependent && node.Node is IGeneratesBodyCode bodyCode)
                {
                    sb.currentNode = node.Node;
                    bodyCode.GenerateNodeCode(sb, graphContext, generationMode);
                    sb.ReplaceInCurrentMapping(PrecisionUtil.Token, node.Node.concretePrecision.ToShaderString());
                }
            }

            foreach (var derivative in m_SplatGraph.InputDerivatives)
            {
                sb.AppendLine($"{derivative.VariableType} ddx_{derivative.VariableName} = ddx({derivative.VariableValue(generationMode)});");
                sb.AppendLine($"{derivative.VariableType} ddy_{derivative.VariableName} = ddy({derivative.VariableValue(generationMode)});");
            }

            foreach (var slot in EnumerateSplatOutputSlots())
                sb.AppendLine($"{slot.concreteValueType.ToShaderString()} {GetVariableNameForSlot(slot.id)} = 0;");

            var blendWeights0 = GetSlotValue(kBlendWeight0InputSlotId, generationMode);
            var blendWeights1 = m_SplatCount > 4 ? GetSlotValue(kBlendWeight1InputSlotId, generationMode) : string.Empty;

            for (int i = 0; i < m_SplatCount; ++i)
            {
                if (i == 4)
                {
                    sb.AppendLine($"#ifdef {GraphData.kSplatCount8Keyword}");
                    sb.IncreaseIndent();
                }

                var blendWeight = $"{(i < 4 ? blendWeights0 : blendWeights1)}.{"xyzw"[i % 4]}";
                if (m_Conditional)
                {
                    sb.AppendLine($"UNITY_BRANCH if ({blendWeight} > 0)");
                    sb.AppendLine("{");
                    sb.IncreaseIndent();
                }

                foreach (var slot in EnumerateSplatOutputSlots())
                    sb.AppendLine($"{slot.concreteValueType.ToShaderString()} {GetVariableNameForSlot(slot.id)}_splat{i};");

                sb.AppendIndentation();
                sb.Append($"SplatFunction_{GetVariableNameForNode()}(IN");
                foreach (var splatInput in m_SplatGraph.SplatFunctionInputs)
                {
                    var varName = splatInput.VariableName;
                    if (splatInput.SplatProperty)
                        varName = $"{varName.Substring(0, varName.Length - 1)}{i}";
                    sb.Append($", {varName}");
                }
                foreach (var derivative in m_SplatGraph.InputDerivatives)
                    sb.Append($", ddx_{derivative.VariableName}, ddy_{derivative.VariableName}");
                foreach (var slot in EnumerateSplatOutputSlots())
                    sb.Append($", {GetVariableNameForSlot(slot.id)}_splat{i}");
                sb.Append(");");
                sb.AppendNewLine();

                foreach (var slot in EnumerateSplatOutputSlots())
                    sb.AppendLine($"{GetVariableNameForSlot(slot.id)} += {GetVariableNameForSlot(slot.id)}_splat{i} * {blendWeight};");

                if (m_Conditional)
                {
                    sb.DecreaseIndent();
                    sb.AppendLine("}");
                }

                if (i == 7)
                {
                    sb.DecreaseIndent();
                    sb.AppendLine("#endif");
                }
            }
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            if (m_SplatGraph == null)
                return;

            if (m_Conditional && !graphContext.conditional)
                graphContext = new GraphContext(graphContext.graphInputStructName, m_Conditional);

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

            // Generate the splat function from splat subgraph
            var splatFunction = new ShaderStringBuilder();
            splatFunction.Append($"void SplatFunction_{GetVariableNameForNode()}({graphContext.graphInputStructName} IN");
            foreach (var splatInput in m_SplatGraph.SplatFunctionInputs)
                splatFunction.Append($", {splatInput.VariableType} {splatInput.VariableName}");
            foreach (var d in m_SplatGraph.InputDerivatives)
                splatFunction.Append($", {d.VariableType} ddx_{d.VariableName}, {d.VariableType} ddy_{d.VariableName}");
            foreach (var slot in EnumerateSplatInputSlots())
                splatFunction.Append($", out {slot.concreteValueType.ToShaderString(concretePrecision)} outSplat{(slot.id - kSplatInputSlotIdStart) / 2}");
            splatFunction.AppendLine(")");
            using (splatFunction.BlockScope())
            {
                foreach (var node in m_SplatGraph.Nodes)
                {
                    splatFunction.currentNode = node.Node;

                    if (node.SplatDependent && node.Node is IGeneratesBodyCode bodyNode)
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
                foreach (var slot in EnumerateSplatInputSlots())
                    splatFunction.AppendLine($"outSplat{(slot.id - kSplatInputSlotIdStart) / 2} = {GetSlotValue(slot.id, generationMode)};");
            }
            registry.ProvideFunction($"SplatFunction_{GetVariableNameForNode()}", s => s.Concat(splatFunction));
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
