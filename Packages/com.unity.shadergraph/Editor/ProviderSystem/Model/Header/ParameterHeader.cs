using Unity.GraphAuthoring.Editor.ProviderSystem;
using Unity.GraphAuthoring.Editor.ProviderSystem.Hints;
using static Unity.GraphAuthoring.Editor.ProviderSystem.HintUtils;

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

        protected override void OnProcess(IShaderField param, IProvider provider)
        {
            referenceName = param.Name;

            isInput  = param.IsInput;
            isOutput = param.IsOutput;
            shaderType = param.ShaderType;

            string typeTest = shaderType.Name.ToLowerInvariant();
            bool isSampler = typeTest.Contains("sampler");
            bool isTexture = typeTest.Contains("texture");
            isBareResource = !typeTest.Contains("unity") && (isSampler || isTexture);

            displayName = param.Hints.GetHint<DisplayName>()?.Value as string;
            tooltip     = param.Hints.GetHint<Tooltip>()?.Value as string;

            isStatic = param.Hints.GetHint<Static>()?.IsResolved ?? false;
            isLocal  = param.Hints.GetHint<Local>()?.IsResolved ?? false;

            var dropdown = param.Hints.GetHint<Dropdown>();
            if (isDropdown = dropdown?.IsResolved ?? false)
                options = dropdown.Value as string[];

            isColor = param.Hints.GetHint<Color>()?.IsResolved ?? false;

            var range = param.Hints.GetHint<Range>();
            if (isSlider = range?.IsResolved ?? false)
            {
                var r = range.Value as float[];
                sliderMin = r[0];
                sliderMax = r[1];
            }

            var defaultHint = param.Hints.GetHint<Default>();
            if (defaultHint?.IsResolved ?? false)
            {
                defaultString = defaultHint.Value as string;
                defaultValue  = LazyTokenFloat(defaultString);
            }

            externalQualifiedTypeName = typeName;
            var externalNamespace = param.Hints.GetHint<External>()?.Value as string;
            if (!string.IsNullOrWhiteSpace(externalNamespace))
                externalQualifiedTypeName = $"{externalNamespace}::{typeName}";

            isLiteral = param.Hints.GetHint<Literal>()?.IsResolved ?? false;

            var referableHint = param.Hints.GetHint<Referable>();
            if (isReferable = referableHint?.IsResolved ?? false)
                Referable = referableHint.Value as string;

            isDynamic = param.Hints.GetHint<Dynamic>()?.IsResolved ?? false;

            var linkage = param.Hints.GetHint<Linkage>();
            if (isLinkage = linkage?.IsResolved ?? false)
            {
                linkTarget = linkage.Value as string;
                isLocal    = true;
            }

            var customBindingHint = param.Hints.GetHint<CustomBinding>();
            if (hasCustomBinding = customBindingHint?.IsResolved ?? false)
                customBinding = customBindingHint.Value as string;
        }

        internal ParameterHeader(IShaderField param, IProvider provider)
        {
            Process(param, provider);
        }

        // For returns.
        internal ParameterHeader(string displayName, IShaderType shaderType, string tooltip, IProvider provider)
        {
            var hints = new IStrongHint[]
            {
                new DisplayName(Common.kDisplayName, displayName, true),
                new Tooltip(Common.kTooltip, tooltip, true),
            };

            var field = new ShaderField("__UNITY_SHADERGRAPH_UNUSED", false, true, shaderType, hints);

            Process(field, provider);
        }
    }
}
