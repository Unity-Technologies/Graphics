using System.Linq;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Terrain", "Splat")]
    partial class SplatNode : AbstractMaterialNode, IHasSettings, IGeneratesBodyCode, IGeneratesFunction,
        IMayRequirePosition, IMayRequireNormal, IMayRequireTangent, IMayRequireBitangent, IMayRequireVertexColor, IMayRequireMeshUV,
        IMayRequireViewDirection, IMayRequireScreenPosition, IMayRequireFaceSign,
        IMayRequireDepthTexture, IMayRequireCameraOpaqueTexture, IMayRequireTime
    {
        public SplatNode()
        {
            name = "Splat";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview => true;

        public const int kBlendWeightInputSlotId = 0;

        private IEnumerable<MaterialSlot> EnumerateSplatInputSlots()
            => this.GetInputSlots<MaterialSlot>().Where(slot => slot.id != kBlendWeightInputSlotId);

        private IEnumerable<MaterialSlot> EnumerateSplatOutputSlots()
            => this.GetOutputSlots<MaterialSlot>();

        [SerializeField]
        private List<string> m_SplatSlotNames = new List<string>();

        public override IEnumerable<ISlot> GetInputSlotsForGraphGeneration()
        {
            yield return FindInputSlot<ISlot>(kBlendWeightInputSlotId);
            // When generating shader grpah code, don't traverse further along the splat input slots.
            yield break;
        }

        public override void UpdateNodeAfterDeserialization()
        {
            CreateSplatSlots(null, null);
            AddSlot(new Vector4MaterialSlot(kBlendWeightInputSlotId, "Blend Weights", "BlendWeights", SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));
            RemoveSlotsNameNotMatching(Enumerable.Range(0, m_SplatSlotNames.Count * 2 + 1));
        }

        public override void ValidateNode()
        {
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

                if (inputSlot.id != kBlendWeightInputSlotId && inputSlot is DynamicVectorMaterialSlot dynamicVector)
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
                    dynamicVector.SetConcreteType(FindInputSlot<DynamicVectorMaterialSlot>(dynamicVector.id - 1).concreteValueType);
            }

            var errorMessage = k_validationErrorMessage;
            hasError = CalculateNodeHasError(ref errorMessage) || hasError;
            hasError = ValidateConcretePrecision(ref errorMessage) || hasError;

            if (hasError)
                owner.AddValidationError(tempId, errorMessage);
            else
                ++version;
        }

        private struct SplatGraph
        {
            public IReadOnlyList<(AbstractMaterialNode node, bool splatDependent)> Nodes;
            public IReadOnlyList<(MaterialSlot slot, bool splatProperty)> SplatFunctionInputs;
        }

        // TODO: cache the resulting SplatGraph until next graph topology change?
        private SplatGraph CompileSplatGraph()
        {
            var splatGraphNodes = new List<AbstractMaterialNode>();
            NodeUtils.DepthFirstCollectNodesFromNode(splatGraphNodes, this, NodeUtils.IncludeSelf.Exclude, EnumerateSplatInputSlots().Select(slot => slot.id).ToList());

            // Determine for each node if it's dependent on the splat properties.
            var splatDependent = new bool[splatGraphNodes.Count];
            var splatFunctionInputs = new HashSet<(MaterialSlot, bool)>(); // gather the output slots of splat properties and non-splat inputs
            for (int i = 0; i < splatGraphNodes.Count; ++i)
            {
                var node = splatGraphNodes[i];
                if (node is PropertyNode propertyNode)
                {
                    if (propertyNode.shaderProperty is ISplattableShaderProperty splatProperty && splatProperty.splat)
                    {
                        splatDependent[i] = true;
                        splatFunctionInputs.Add((propertyNode.GetOutputSlots<MaterialSlot>().First(), true));
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

                splatDependent[i] = outputSlots.Any(v => v.splatDependent);
                if (splatDependent[i])
                {
                    foreach (var (splat, slot) in outputSlots)
                    {
                        if (!splat && slot != null)
                            splatFunctionInputs.Add((slot, false));
                    }
                }
            }

            var graphNodes = new List<(AbstractMaterialNode, bool)>();
            for (int i = 0; i < splatGraphNodes.Count; ++i)
                graphNodes.Add((splatGraphNodes[i], splatDependent[i]));

            return new SplatGraph()
            {
                Nodes = graphNodes,
                SplatFunctionInputs = splatFunctionInputs.ToList()
            };
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            var splatGraph = CompileSplatGraph();

            foreach (var (node, splatDependent) in splatGraph.Nodes)
            {
                if (!splatDependent && node is IGeneratesBodyCode bodyCode)
                {
                    sb.currentNode = node;
                    bodyCode.GenerateNodeCode(sb, graphContext, generationMode);
                    sb.ReplaceInCurrentMapping(PrecisionUtil.Token, node.concretePrecision.ToShaderString());
                }
            }

            for (int i = 0; i < 4; ++i)
            {
                foreach (var slot in EnumerateSplatOutputSlots())
                    sb.AppendLine($"{slot.concreteValueType.ToShaderString()} {GetVariableNameForSlot(slot.id)}_splat{i};");

                sb.AppendIndentation();
                sb.Append($"SplatFunction_{GetVariableNameForNode()}(IN");
                foreach (var (slot, splatProp) in splatGraph.SplatFunctionInputs)
                {
                    var varName = slot.owner.GetVariableNameForSlot(slot.id);
                    if (splatProp)
                        varName = $"{varName.Substring(0, varName.Length - 1)}{i}";
                    sb.Append($", {varName}");
                }
                foreach (var slot in EnumerateSplatOutputSlots())
                    sb.Append($", {GetVariableNameForSlot(slot.id)}_splat{i}");
                sb.Append(");");
                sb.AppendNewLine();
            }

            var blendWeights = GetSlotValue(kBlendWeightInputSlotId, generationMode);
            foreach (var slot in EnumerateSplatOutputSlots())
                sb.AppendLine(string.Format("{0} {1} = {1}_splat0 * ({2}).x + {1}_splat1 * ({2}).y + {1}_splat2 * ({2}).z + {1}_splat3 * ({2}).w;", slot.concreteValueType.ToShaderString(), GetVariableNameForSlot(slot.id), blendWeights));
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            var splatGraph = CompileSplatGraph();

            // Generate global functions from splat graph.
            foreach (var (node, splatDependent) in splatGraph.Nodes)
            {
                if (node is IGeneratesFunction functionNode)
                {
                    registry.builder.currentNode = node;
                    functionNode.GenerateNodeFunction(registry, graphContext, generationMode);
                    registry.builder.ReplaceInCurrentMapping(PrecisionUtil.Token, node.concretePrecision.ToShaderString());
                }
            }

            // Generate the splat function from splat subgraph
            var splatFunction = new ShaderStringBuilder();
            splatFunction.Append($"void SplatFunction_{GetVariableNameForNode()}({graphContext.graphInputStructName} IN");
            foreach (var (slot, splatProp) in splatGraph.SplatFunctionInputs)
                splatFunction.Append($", {slot.concreteValueType.ToShaderString(concretePrecision)} {slot.owner.GetVariableNameForSlot(slot.id)}");
            foreach (var slot in EnumerateSplatInputSlots())
                splatFunction.Append($", out {slot.concreteValueType.ToShaderString(concretePrecision)} outSplat{slot.id}");
            splatFunction.AppendLine(")");
            using (splatFunction.BlockScope())
            {
                foreach (var (node, splatDependent) in splatGraph.Nodes)
                {
                    if (splatDependent && node is IGeneratesBodyCode bodyNode)
                    {
                        splatFunction.currentNode = node;
                        bodyNode.GenerateNodeCode(splatFunction, graphContext, generationMode);
                        splatFunction.ReplaceInCurrentMapping(PrecisionUtil.Token, node.concretePrecision.ToShaderString());
                    }
                }
                foreach (var slot in EnumerateSplatInputSlots())
                    splatFunction.AppendLine($"outSplat{slot.id} = {GetSlotValue(slot.id, generationMode)};");
            }
            registry.ProvideFunction("SplatFunction", s => s.Concat(splatFunction));
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            foreach (var (node, splatDependent) in CompileSplatGraph().Nodes)
                node.CollectPreviewMaterialProperties(properties);
            base.CollectPreviewMaterialProperties(properties);
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            foreach (var (node, splatDependent) in CompileSplatGraph().Nodes)
                node.CollectShaderProperties(properties, generationMode);
            base.CollectShaderProperties(properties, generationMode);
        }

        NeededCoordinateSpace IMayRequirePosition.RequiresPosition(ShaderStageCapability stageCapability)
            => CompileSplatGraph().Nodes.Select(v => v.node).OfType<IMayRequirePosition>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresPosition(stageCapability));

        NeededCoordinateSpace IMayRequireNormal.RequiresNormal(ShaderStageCapability stageCapability)
            => CompileSplatGraph().Nodes.Select(v => v.node).OfType<IMayRequireNormal>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresNormal(stageCapability));

        NeededCoordinateSpace IMayRequireTangent.RequiresTangent(ShaderStageCapability stageCapability)
            => CompileSplatGraph().Nodes.Select(v => v.node).OfType<IMayRequireTangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresTangent(stageCapability));

        NeededCoordinateSpace IMayRequireBitangent.RequiresBitangent(ShaderStageCapability stageCapability)
            => CompileSplatGraph().Nodes.Select(v => v.node).OfType<IMayRequireBitangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresBitangent(stageCapability));

        bool IMayRequireVertexColor.RequiresVertexColor(ShaderStageCapability stageCapability)
            => CompileSplatGraph().Nodes.Select(v => v.node).OfType<IMayRequireVertexColor>().Any(node => node.RequiresVertexColor());

        bool IMayRequireMeshUV.RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
            => CompileSplatGraph().Nodes.Select(v => v.node).OfType<IMayRequireMeshUV>().Any(node => node.RequiresMeshUV(channel));

        NeededCoordinateSpace IMayRequireViewDirection.RequiresViewDirection(ShaderStageCapability stageCapability)
            => CompileSplatGraph().Nodes.Select(v => v.node).OfType<IMayRequireViewDirection>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresViewDirection(stageCapability));

        bool IMayRequireScreenPosition.RequiresScreenPosition(ShaderStageCapability stageCapability)
            => CompileSplatGraph().Nodes.Select(v => v.node).OfType<IMayRequireScreenPosition>().Any(node => node.RequiresScreenPosition());

        bool IMayRequireFaceSign.RequiresFaceSign(ShaderStageCapability stageCapability)
            => CompileSplatGraph().Nodes.Select(v => v.node).OfType<IMayRequireFaceSign>().Any(node => node.RequiresFaceSign());

        bool IMayRequireDepthTexture.RequiresDepthTexture(ShaderStageCapability stageCapability)
            => CompileSplatGraph().Nodes.Select(v => v.node).OfType<IMayRequireDepthTexture>().Any(node => node.RequiresDepthTexture());

        bool IMayRequireCameraOpaqueTexture.RequiresCameraOpaqueTexture(ShaderStageCapability stageCapability)
            => CompileSplatGraph().Nodes.Select(v => v.node).OfType<IMayRequireCameraOpaqueTexture>().Any(node => node.RequiresCameraOpaqueTexture());

        bool IMayRequireTime.RequiresTime()
            => CompileSplatGraph().Nodes.Select(v => v.node).OfType<IMayRequireTime>().Any(node => node.RequiresTime());
    }
}
