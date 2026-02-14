using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Graphing;
using System;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // A Base Node serving as the entry point for Provider based function definitions.
    // This Model object can work with any sort of function provider, interpret the function definition,
    // and generate a valid model representation of that object. This abstracts the need for node definitions
    // to be aware of how the model functions.
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
                if (m_provider?.AssetID != default)
                    assetCollection.AddAssetDependency(m_provider.AssetID,
                        AssetCollection.Flags.SourceDependency
                        | AssetCollection.Flags.ArtifactDependency
                        | AssetCollection.Flags.IncludeInExportPackage);
            }
        }

        [SerializeReference]
        IProvider<IShaderFunction> m_provider;

        internal FunctionHeader Header { get; private set; }

        internal IProvider<IShaderFunction> Provider => m_provider;

        const int kReservedOutputSlot = 0;

        public override bool hasPreview => true;

        public override bool canSetPrecision => false;

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
            UpdateModel();
        }

        public override void Concretize()
        {
            UpdateModel();
            base.Concretize();
        }

        public bool Reload(HashSet<string> changedAssetGuids)
        {
            if (changedAssetGuids.Contains(Provider.AssetID.ToString()))
            {
                Provider?.Reload();
                UpdateModel();
                owner.ClearErrorsForNode(this);
                ValidateNode();
                Dirty(ModificationScope.Topological);
                Dirty(ModificationScope.Graph);
                return true;
            }
            return false;
        }

        internal void UpdateModel()
        {
            if (Provider == null || Provider.Definition == null)
                return;

            Header = new FunctionHeader(Provider);
            var header = Header;
            this.name = header.displayName;
            this.synonyms = header.searchTerms;
            var parameters = Provider.Definition.Parameters;

            List<MaterialSlot> previousSlots = new();
            GetSlots(previousSlots);

            Dictionary<string, (int inputId, int outputId)> oldSlotMap = new();
            HashSet<int> usedSlotIds = new();

            // build a map of parameter names to old slot ids (tuple is to support inout params).
            foreach (var oldSlot in previousSlots)
            {
                if (!oldSlotMap.TryGetValue(oldSlot.shaderOutputName, out var idTuple))
                    idTuple = (-1, -1);

                if (oldSlot.isInputSlot) idTuple.inputId = oldSlot.id;
                if (oldSlot.isOutputSlot) idTuple.outputId = oldSlot.id;

                oldSlotMap[oldSlot.shaderOutputName] = idTuple;
            }

            List<ParameterHeader> paramHeaders = new();
            List<int> desiredSlotOrder = new();

            // return type is a special case, because it has no parameter but still needs a slot
            // we reserve the 0 slotId for this and always make sure it's added first/at the top.
            if (header.hasReturnType)
            {
                var returnParam = new ParameterHeader(header.returnDisplayName, header.returnType, header.tooltip);
                usedSlotIds.Add(kReservedOutputSlot);
                desiredSlotOrder.Add(kReservedOutputSlot);
                AddSlotFromParameter(returnParam, kReservedOutputSlot, SlotType.Output);
            }

            // build the header data for our parameters and mark which slot ids are being reused.
            foreach(var param in parameters)
            {
                paramHeaders.Add(new ParameterHeader(param, Provider.Definition));
                if (oldSlotMap.TryGetValue(param.Name, out var idTuple))
                {
                    if (idTuple.inputId > -1) usedSlotIds.Add(idTuple.inputId);
                    if (idTuple.outputId > -1) usedSlotIds.Add(idTuple.outputId);
                }
            }

            // walk through our header data and build the actual slots.
            int nextSlot = kReservedOutputSlot;
            foreach (var paramHeader in paramHeaders)
            {
                if (!oldSlotMap.TryGetValue(paramHeader.referenceName, out var idTuple))
                    idTuple = (-1, -1);

                void DoSlot(int slotId, SlotType dir)
                {
                    if (slotId == -1)
                    {
                        while (usedSlotIds.Contains(++nextSlot));
                        usedSlotIds.Add(nextSlot);
                        slotId = nextSlot;
                    }
                    AddSlotFromParameter(paramHeader, slotId, dir);
                    desiredSlotOrder.Add(slotId);
                }

                if (paramHeader.isInput)
                    DoSlot(idTuple.inputId, SlotType.Input);
                if (paramHeader.isOutput)
                    DoSlot(idTuple.outputId, SlotType.Output);
            }
            RemoveSlotsNameNotMatching(usedSlotIds, true);
            SetSlotOrder(desiredSlotOrder);
        }

        void AddSlotFromParameter(ParameterHeader header, int slotId, SlotType dir)
        {
            var slot = HeaderUtils.MakeSlotFromParameter(header, slotId, dir);
            if (slot != null)
                AddSlot(slot);
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (this.Provider == null || this.Provider.Definition == null)
                return;

            using (var slots = PooledList<MaterialSlot>.Get())
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

                ShaderStringBuilder call = new(); // for the callsite.
                ShaderStringBuilder args = new(); // for building up the argument list.

                // The return result is an output slot, but will instead get initialized by the function call.
                if (returnSlot != null)
                {
                    call.Append($"{HeaderUtils.ToShaderType(returnSlot)} {GetVariableNameForSlot(returnSlot.id)} = ");
                }

                bool first = true;
                foreach (var param in Provider.Definition.Parameters)
                {
                    var inputSlot = paramSlotMap[param.Name].input;
                    var outputSlot = paramSlotMap[param.Name].output;

                    var typeString = outputSlot != null ? HeaderUtils.ToShaderType(outputSlot) : HeaderUtils.ToShaderType(inputSlot);

                    var variableName = outputSlot != null ? GetVariableNameForSlot(outputSlot.id) : null;
                    var valueString = inputSlot != null ? GetSlotValue(inputSlot.id, generationMode) : null;

                    if (inputSlot != null && inputSlot.bareResource) // Texture/Sampler slots are normally 'Unity' wrapper structures
                        valueString += inputSlot is SamplerStateMaterialSlot ? ".samplerstate" : ".tex";

                    var argument = valueString; // assume it's an input, in which case the arg will be the upstream value/connection.

                    if (outputSlot != null)
                    {
                        // inout and out define a variable to be used as the argument.
                        // but only inout gets initialized by the value.
                        argument = variableName;
                        sb.AddLine($"{typeString} {variableName}{(inputSlot != null ? $" = {valueString}" : "")};");
                    }

                    if (!first)
                        args.Append(", ");
                    first = false;

                    args.Append(argument);
                }

                foreach (var name in Provider.Definition.Namespace)
                    call.Append($"{name}::");
                call.Append(Provider.Definition.Name);
                call.Append("(");
                call.Append(args.ToString());
                call.Append(");");


                sb.AddLine(call.ToString());
            }
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            var includePath = AssetDatabase.GUIDToAssetPath(Provider.AssetID);
            registry.RequiresIncludePath(includePath, false);
        }

        public override void ValidateNode()
        {
            base.ValidateNode();
            if (Provider == null)
            {
                owner.AddSetupError(this.objectId, "Node is in an invalid state and cannot recover.");
                return;
            }
            else if (Provider.AssetID != default && (Provider.Definition == null || !Provider.Definition.IsValid))
            {
                var path = AssetDatabase.GUIDToAssetPath(Provider.AssetID);
                owner.AddSetupError(this.objectId, $"Data for '{Provider.ProviderKey}' expected in '{path}'.");
                return;
            }
            else
            {
                foreach(var param in Provider.Definition.Parameters)
                {
                    var header = new ParameterHeader(param, Provider.Definition);
                    if (!string.IsNullOrWhiteSpace(header.knownIssue))
                    {
                        owner.AddValidationError(this.objectId, header.knownIssue, Rendering.ShaderCompilerMessageSeverity.Warning);
                        break; // We can only show one error badge at a time.
                    }
                }
            }
        }
    }
}
