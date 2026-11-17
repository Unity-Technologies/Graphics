using System;
using System.Collections.Generic;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    [Serializable]
    internal abstract class AbstractVirtualFunctionNode : AbstractProviderNode<IVirtualFunction>
    {
        internal const string kNoContextLabel = "No Context";

        internal abstract string shortName { get; }

        private protected abstract SlotLayout slotLayout { get; }

        private protected override string missingProviderMessage => NotDeclaredMessage("virtual function");

        public override bool hasPreview => false;

        public override bool canSetPrecision => false;

        public override string documentationURL => Provider?.Definition?.Presentation.HelpUrl is { Length: > 0 } url ? url : base.documentationURL;

        internal static string ContextOf(IVirtualFunction definition) => definition.Presentation.Context ?? string.Empty;

        internal static string ContextLabel(string context) => string.IsNullOrEmpty(context) ? kNoContextLabel : context;

        internal static bool HasBase(IVirtualFunction definition) => !definition.IsPureVirtual;

        internal static List<AbstractVirtualFunctionNode> NodesFor(IVirtualFunction definition)
        {
            List<AbstractVirtualFunctionNode> nodes = new() { new VirtualFunctionInputNode() };
            if (HasBase(definition))
                nodes.Add(new VirtualFunctionBaseNode());
            return nodes;
        }

        internal virtual string TitleFor(IVirtualFunction definition) => $"{definition.Presentation.DisplayName} {shortName}";

        private protected override bool TryFind(ShaderInterfaceProvider shaderInterface, string key, out IProvider<IVirtualFunction> provider)
            => shaderInterface.TryGetVirtualFunctionProvider(key, out provider);

        private protected sealed override void BuildModel(IVirtualFunction definition)
        {
            name = TitleFor(definition);
            synonyms = SearchTerms(definition);
            ProviderSlotUtils.Rebuild(this, definition, slotLayout);
        }

        string[] SearchTerms(IVirtualFunction definition)
        {
            var presentation = definition.Presentation;
            List<string> terms = new() { presentation.DisplayName, "virtual function", "local", shortName.ToLowerInvariant() };
            var context = ContextOf(definition);
            if (context.Length > 0)
                terms.Add(context);
            foreach (var term in presentation.SearchTerms ?? Array.Empty<string>())
                if (!terms.Contains(term))
                    terms.Add(term);
            return terms.ToArray();
        }

        // Until virtual functions generate code, nodes downstream read the default values of the outputs.
        protected internal override string GetOutputForSlot(SlotReference fromSocketRef, ConcreteSlotValueType valueType, GenerationMode generationMode)
            => FindOutputSlot<MaterialSlot>(fromSocketRef.slotId)?.GetDefaultValue(generationMode) ?? string.Empty;
    }
}
