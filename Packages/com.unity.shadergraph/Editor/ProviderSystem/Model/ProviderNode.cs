using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Graphing;
using System;
using Unity.GraphAuthoring.Editor.ProviderSystem;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // Calls a function from any function provider; its slots follow the function's definition.

    // NOTE: There exists an ExpressionProvider currently, so it's not a problem to have Models that are specializations,
    // but ideally the expression input type would be gracefully handled in a generic way and it wouldn't be necessary.
    [Serializable]
    [HasDependencies(typeof(MinimalProviderNode))]
    internal class ProviderNode : AbstractProviderNode<IShaderFunction>, IGeneratesBodyCode, IGeneratesFunction
    {
        [Serializable]
        internal class MinimalProviderNode : IHasDependencies
        {
            [SerializeReference]
            IProvider<IShaderFunction> m_provider;

            public void GetSourceAssetDependencies(AssetCollection assetCollection)
            {
                if (m_provider == null || m_provider.AssetID == default)
                    return;

                assetCollection.AddAssetDependency(m_provider.AssetID,
                    AssetCollection.Flags.SourceDependency
                    | AssetCollection.Flags.ArtifactDependency
                    | AssetCollection.Flags.IncludeInExportPackage);
            }
        }


        // Reserves a control for the function selector. This is a bit of a hack.
        [GroupVariantControl]
        internal ProviderNode groupVariantSentinel => this;

        internal FunctionHeader Header { get; private set; }

        const int kReservedOutputSlot = ProviderSlotUtils.kReturnSlotId;

        public override bool hasPreview => true;

        public override bool hasUserAuthoredCode => true;

        public override bool canSetPrecision => Header?.allowPrecision ?? false;

        public override string documentationURL => Header?.helpUrl ?? base.documentationURL;

        public ProviderNode()
        {
            name = "Provider Based Node";
        }

        private protected override void BuildModel(IShaderFunction definition)
        {
            Header = new FunctionHeader(definition, Provider);
            name = Header.displayName;
            synonyms = Header.searchTerms;
            ProviderSlotUtils.Rebuild(this, definition);
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (isOrphaned)
                return;

            // Apply legacy precision/dynamic only before code generation/usage.
            HeaderUtils.TryApplyLegacyPrecisionTokens(Provider.Definition, PrecisionUtil.Token, lastKnownDynamicVectorLength, out var func);

            using (UnityEngine.Pool.ListPool<MaterialSlot>.Get(out var slots))
            {
                GetSlots<MaterialSlot>(slots);
                Dictionary<string, (MaterialSlot input, MaterialSlot output)> paramSlotMap = new();
                MaterialSlot returnSlot = null;

                // for various reasons, slots won't match one-to-one with parameters;
                // we'll need to make sure slots are matched to parameters in the order expected
                // while also accounting for inout duplicity.
                foreach (var slot in slots)
                {
                    if (slot.id == kReservedOutputSlot)
                        returnSlot = slot;
                    else
                    {
                        paramSlotMap.TryGetValue(slot.shaderOutputName, out var tup);
                        if (slot.isInputSlot) tup.input = slot;
                        if (slot.isOutputSlot) tup.output = slot;
                        paramSlotMap[slot.shaderOutputName] = tup;
                    }
                }

                // Adapts Shader Graph's slot values and variables to the producer and writer.
                Dictionary<string, ShaderArgument> arguments = new();
                Dictionary<string, IShaderField> outputVariables = new();
                foreach (var param in func.Parameters)
                {
                    paramSlotMap.TryGetValue(param.Name, out var paramSlots);
                    var inputSlot = paramSlots.input;
                    var outputSlot = paramSlots.output;

                    if (outputSlot != null)
                        outputVariables[param.Name] = DefinitionCodeGenerator.OutputVariable(GetVariableNameForSlot(outputSlot.id), HeaderUtils.ToShaderType(outputSlot));

                    if (inputSlot == null)
                        continue;

                    var valueString = GetSlotValue(inputSlot.id, generationMode);
                    if (inputSlot.bareResource) // Texture/Sampler slots are normally 'Unity' wrapper structures
                        valueString += inputSlot is SamplerStateMaterialSlot ? ".samplerstate" : ".tex";

                    // apply linkage override
                    if (param.Production.Access == FieldAccess.Linkage)
                    {
                        var targetSlots = paramSlotMap[param.Production.LinkTarget];

                        bool linked = targetSlots.input != null && targetSlots.input.isConnected
                            || targetSlots.output != null && targetSlots.output.isConnected;
                        valueString = linked ? "true" : "false";
                    }

                    arguments[param.Name] = ShaderArgument.Value(valueString);
                }

                if (returnSlot != null)
                    outputVariables[ShaderObjectUtils.kReturnName] = DefinitionCodeGenerator.OutputVariable(GetVariableNameForSlot(returnSlot.id), HeaderUtils.ToShaderType(returnSlot));

                DefinitionCodeGenerator.GenerateCall(func, objectId, arguments, outputVariables, sb);
            }
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            if (isOrphaned)
                return;

            HeaderUtils.TryApplyLegacyPrecisionTokens(Provider.Definition, PrecisionUtil.Token, lastKnownDynamicVectorLength, out var func);
            DefinitionCodeGenerator.CollectFunctions(func, registry);
        }
    }
}
