using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Graphing;
using System;
using Unity.GraphAuthoring.Editor.ProviderSystem;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // A Base Node serving as the entry point for Provider based function definitions.
    // This Model object can work with any sort of function provider, interpret the function definition,
    // and generate a valid model representation of that object. This abstracts the need for node definitions
    // to be aware of how the model functions.

    // NOTE: There exists an ExpressionProvider currently, so it's not a problem to have Models that are specializations,
    // but ideally the expression input type would be gracefully handled in a generic way and it wouldn't be necessary.
    [Serializable]
    [HasDependencies(typeof(MinimalProviderNode))]
    internal class ProviderNode : AbstractMaterialNode, IHasAssetDependencies, IGeneratesBodyCode, IGeneratesFunction
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

        [SerializeReference]
        IProvider<IShaderFunction> m_provider;

        internal FunctionHeader Header { get; private set; }

        internal IProvider<IShaderFunction> Provider => m_provider;

        const int kReservedOutputSlot = ProviderSlotUtils.kReturnSlotId;

        public override bool hasPreview => true;

        public override bool hasUserAuthoredCode => true;

        public override bool canSetPrecision => Header?.allowPrecision ?? false;

        public override string documentationURL => Header?.helpUrl ?? base.documentationURL;

        internal override bool ExposeToSearcher => false;

        public ProviderNode()
        {
            name = "Provider Based Node";
        }

        internal void InitializeFromProvider(IProvider<IShaderFunction> provider)
        {
            this.m_provider = (IProvider<IShaderFunction>)provider.Clone();
            UpdateModel();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            Refresh();
        }

        public override void Concretize()
        {
            UpdateModel();
            base.Concretize();
        }

        public bool Reload(HashSet<string> changedAssetGuids)
        {
            // Scripted providers don't react to asset changes — their definition is in-memory.
            if (Provider.AssetID != default && changedAssetGuids.Contains(Provider.AssetID.ToString()))
            {
                Refresh();
                return true;
            }
            return false;
        }

        protected void Refresh()
        {
            Provider?.Reload();
            UpdateModel();
            ValidateNode();
            Dirty(ModificationScope.Graph);
        }

        internal void UpdateModel()
        {
            if (Provider == null || Provider.Definition == null || !Provider.IsValid)
                return;

            Header = new FunctionHeader(Provider.Definition, Provider);

            var header = Header;
            this.name = header.displayName;
            this.synonyms = header.searchTerms;

            ProviderSlotUtils.Rebuild(this, Provider.Definition);
        }

        static Rendering.ShaderCompilerMessageSeverity ToCompilerSeverity(MessageType severity) => severity switch
        {
            MessageType.Error => Rendering.ShaderCompilerMessageSeverity.Error,
            _                  => Rendering.ShaderCompilerMessageSeverity.Warning,
        };

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (this.Provider == null || this.Provider.Definition == null)
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
            if (Provider == null || !Provider.IsValid || Provider.Definition == null)
                return;

            HeaderUtils.TryApplyLegacyPrecisionTokens(Provider.Definition, PrecisionUtil.Token, lastKnownDynamicVectorLength, out var func);
            DefinitionCodeGenerator.CollectFunctions(func, registry);
        }

        public override void ValidateNode()
        {
            base.ValidateNode();
            if (Provider == null)
            {
                owner?.AddSetupError(this.objectId, "Node is in an invalid state and cannot recover.");
                return;
            }
            else if (Provider.AssetID != default && (Provider.Definition == null || !Provider.Definition.IsValid))
            {
                var path = AssetDatabase.GUIDToAssetPath(Provider.AssetID);
                owner?.AddSetupError(this.objectId, $"Data for '{Provider.ProviderKey}' expected in '{path}'.");
                return;
            }
            else if (Header != null)
            {
                foreach (var (_, text, severity) in Header.messages)
                    owner?.AddValidationError(this.objectId, text, ToCompilerSeverity(severity));
            }
        }
    }
}
