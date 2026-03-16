namespace UnityEditor.ShaderGraph.ProviderSystem
{
    internal class FunctionHeader : StrongHeader<IShaderFunction>
    {
        internal string referenceName { get; private set; }

        internal string displayName { get; private set; }
        internal string tooltip { get; private set; }

        internal string returnDisplayName { get; private set; }
        internal string returnTooltip { get; private set; }
        internal IShaderType returnType { get; private set; }
        internal bool hasReturnValueType => returnType.Name != "void";

        internal string searchName { get; private set; }
        internal string[] searchTerms { get; private set; }
        internal string searchCategory { get; private set; }

        internal ParameterHeader returnHeader { get; private set; }

        internal bool allowPrecision { get; private set; }

        private static HintRegistry<IShaderFunction> s_HintRegistry;
        protected override HintRegistry<IShaderFunction> GetHintRegistry()
        {
            if (s_HintRegistry == null)
            {
                s_HintRegistry = new();
                s_HintRegistry.RegisterStrongHint(new Hints.ProviderKey());

                s_HintRegistry.RegisterStrongHint(new Hints.DisplayName<IShaderFunction>());
                s_HintRegistry.RegisterStrongHint(new Hints.Tooltip<IShaderFunction>());

                // Duplicate Synonyms are getting registered and we aren't handling conflicts;
                // but we should allow multimapping!
                s_HintRegistry.RegisterStrongHint(new Hints.DisplayName<IShaderFunction>(Hints.Func.kReturnDisplayName, "Out"));
                s_HintRegistry.RegisterStrongHint(new Hints.Tooltip<IShaderFunction>(Hints.Func.kReturnTooltip));

                s_HintRegistry.RegisterStrongHint(new Hints.SearchName());
                s_HintRegistry.RegisterStrongHint(new Hints.SearchTerms());
                s_HintRegistry.RegisterStrongHint(new Hints.SearchCategory());

                s_HintRegistry.RegisterStrongHint(new Hints.Flag<IShaderFunction>(Hints.Func.kPrecision, null, new string[] { "Precision" }));
            }
            return s_HintRegistry;
        }

        // Read processed data and make it more accessible to work with.
        protected override void OnProcess(IShaderFunction func, IProvider provider)
        {
            referenceName = func.Name;
            returnType = func.ReturnType;

            displayName = Get<string>(Hints.Common.kDisplayName);
            tooltip = Get<string>(Hints.Common.kTooltip);

            returnDisplayName = Get<string>(Hints.Func.kReturnDisplayName);
            returnTooltip = Get<string>(Hints.Func.kReturnTooltip);

            searchName = Get<string>(Hints.Func.kSearchName);
            searchTerms = Get<string[]>(Hints.Func.kSearchTerms);
            searchCategory = Get<string>(Hints.Func.kSearchCategory);

            allowPrecision = Has(Hints.Func.kPrecision);

            returnHeader = new ParameterHeader(returnDisplayName, func.ReturnType, returnTooltip, provider);
        }
    }
}
