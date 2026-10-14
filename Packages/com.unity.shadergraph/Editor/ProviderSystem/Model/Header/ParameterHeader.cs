using Unity.GraphAuthoring.Editor.ProviderSystem;
using static Unity.GraphAuthoring.Editor.ProviderSystem.ShaderValueUtils;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // TODO: Headers copy what the definition already carries; read it directly and remove the headers.
    internal class ParameterHeader : HeaderBase<IShaderField>
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

            var presentation = param.Presentation;
            var production = param.Production;
            displayName = presentation.DisplayName;
            tooltip     = presentation.Tooltip;

            isStatic = production.Access == FieldAccess.Static;
            isLocal  = production.Access == FieldAccess.Local;

            var editor = presentation.Editor;
            isDropdown = editor is DropdownEditorData;
            if (editor is DropdownEditorData dropdown)
                options = new System.Collections.Generic.List<string>(dropdown.Options).ToArray();

            isColor = editor is ColorEditorData;

            isSlider = editor is RangeEditorData;
            if (editor is RangeEditorData range)
            {
                sliderMin = range.Min;
                sliderMax = range.Max;
            }

            var defaultText = production.Default switch
            {
                LiteralDefault literal => literal.Text,
                ReferableDefault referable => referable.Option,
                _ => null,
            };
            if (defaultText != null)
            {
                defaultString = defaultText;
                defaultValue  = LazyTokenFloat(defaultString);
            }

            externalQualifiedTypeName = ShaderObjectUtils.QualifyName(shaderType);

            isReferable = production.Default is ReferableDefault;
            if (production.Default is ReferableDefault referableDefault)
                Referable = referableDefault.Referable;

            isDynamic = production.IsDynamicVector;

            if (isLinkage = production.Access == FieldAccess.Linkage)
            {
                linkTarget = production.LinkTarget;
                isLocal    = true;
            }

            hasCustomBinding = editor is CustomBindingEditorData;
            if (editor is CustomBindingEditorData binding)
                customBinding = binding.Label;
        }

        internal ParameterHeader(IShaderField param, IProvider provider)
        {
            Process(param, provider);
        }

        // For returns.
        internal ParameterHeader(string displayName, IShaderType shaderType, string tooltip, IProvider provider)
        {
            var field = new ShaderField("__UNITY_SHADERGRAPH_UNUSED", false, true, shaderType, presentation: new FieldPresentationData(displayName, tooltip));

            Process(field, provider);
        }
    }
}
