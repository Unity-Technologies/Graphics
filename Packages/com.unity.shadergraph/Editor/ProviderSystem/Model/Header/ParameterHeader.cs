using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    internal class ParameterHeader : StrongHeader<IShaderField>
    {
        internal string referenceName { get; private set; }
        internal IShaderType shaderType { get; private set; }
        internal string typeName => shaderType.Name;

        internal string displayName { get; private set; }
        internal string tooltip { get; private set; }

        internal bool isInput { get; private set; }
        internal bool isOutput { get; private set; }

        internal bool isColor { get; private set; }

        internal bool isStatic { get; private set; }
        internal bool isLocal { get; private set; }

        internal bool isDropdown { get; private set; }
        internal string[] options { get; private set; }

        internal bool isSlider { get; private set; }
        internal float sliderMin { get; private set; }
        internal float sliderMax { get; private set; }

        internal float[] defaultValue { get; private set; }
        internal string defaultString { get; private set; }

        internal string externalQualifiedTypeName { get; private set; }

        internal bool isLiteral { get; private set; }

        internal bool isDynamic { get; private set; }

        internal bool isBareResource { get; private set; }

        internal bool isReferable { get; private set; }
        internal string Referable { get; private set; }

        internal bool isLinkage { get; private set; }
        internal string linkTarget { get; private set; }

        internal bool hasCustomBinding { get; private set; }
        internal string customBinding { get; private set; }

        private static HintRegistry<IShaderField> s_HintRegistry;
        protected override HintRegistry<IShaderField> GetHintRegistry()
        {
            if (s_HintRegistry == null)
            {
                s_HintRegistry = new();
                s_HintRegistry.RegisterStrongHint(new Hints.DisplayName<IShaderField>());
                s_HintRegistry.RegisterStrongHint(new Hints.Tooltip<IShaderField>());

                s_HintRegistry.RegisterStrongHint(new Hints.Flag<IShaderField>(Hints.Param.kLocal, new string[] { Hints.Param.kAccessModifier }));

                s_HintRegistry.RegisterStrongHint(new Hints.Literal());
                s_HintRegistry.RegisterStrongHint(new Hints.Static());
                s_HintRegistry.RegisterStrongHint(new Hints.Color());
                s_HintRegistry.RegisterStrongHint(new Hints.Range());
                s_HintRegistry.RegisterStrongHint(new Hints.Dropdown());
                s_HintRegistry.RegisterStrongHint(new Hints.Default());
                s_HintRegistry.RegisterStrongHint(new Hints.External());
                s_HintRegistry.RegisterStrongHint(new Hints.Referable());
                s_HintRegistry.RegisterStrongHint(new Hints.Dynamic());
                s_HintRegistry.RegisterStrongHint(new Hints.Linkage());
                s_HintRegistry.RegisterStrongHint(new Hints.CustomBinding());
            }
            return s_HintRegistry;
        }

        // Read processed data and make it more accessible to work with.
        protected override void OnProcess(IShaderField param, IProvider provider)
        {
            referenceName = param.Name;

            isInput = param.IsInput;
            isOutput = param.IsOutput;
            shaderType = param.ShaderType;

            // TODO: improve type handling.
            string typeTest = shaderType.Name.ToLowerInvariant();
            bool isSampler = typeTest.Contains("sampler");
            bool isTexture = typeTest.Contains("texture");
            isBareResource = !typeTest.Contains("unity") && (isSampler || isTexture);

            displayName = Get<string>(Hints.Common.kDisplayName);
            tooltip = Get<string>(Hints.Common.kTooltip);

            isStatic = this.Has(Hints.Param.kStatic);
            isLocal = this.Has(Hints.Param.kLocal);

            if (isDropdown = this.Has(Hints.Param.kDropdown))
                options = this.Get<string[]>(Hints.Param.kDropdown);

            isColor = this.Has(Hints.Param.kColor);

            if (isSlider = this.Has(Hints.Param.kRange))
            {
                var range = this.Get<float[]>(Hints.Param.kRange);
                sliderMin = range[0];
                sliderMax = range[1];
            }

            if (this.Has(Hints.Param.kDefault))
            {
                defaultString = this.Get<string>(Hints.Param.kDefault);
                defaultValue = HeaderUtils.LazyTokenFloat(defaultString);
            }

            externalQualifiedTypeName = typeName;
            var externalNamespace = Get<string>(Hints.Param.kExternal);
            if (!string.IsNullOrWhiteSpace(externalNamespace))
                externalQualifiedTypeName = $"{externalNamespace}::{typeName}";

            isLiteral = Has(Hints.Param.kLiteral);

            if (isReferable = Has(Hints.Param.kReferable))
            {
                Referable = Get<string>(Hints.Param.kReferable);
            }

            isDynamic = Has(Hints.Param.kDynamic);

            if (isLinkage = Has(Hints.Param.kLinkage))
            {
                linkTarget = Get<string>(Hints.Param.kLinkage);
                isLocal = true;
            }

            if (hasCustomBinding = Has(Hints.Param.kCustomBinding))
            {
                customBinding = Get<string>(Hints.Param.kCustomBinding);
            }
        }

        internal ParameterHeader(IShaderField param, IProvider provider)
        {
            Process(param, provider);
        }

        // For returns.
        internal ParameterHeader(string displayName, IShaderType shaderType, string tooltip, IProvider provider)
        {
            var hints = new Dictionary<string, string>() {
                { Hints.Common.kDisplayName, displayName },
                { Hints.Common.kTooltip, tooltip },
            };

            var field = new ShaderField("__UNITY_SHADERGRAPH_UNUSED", false, true, shaderType, hints);

            Process(field, provider);
        }
    }
}
