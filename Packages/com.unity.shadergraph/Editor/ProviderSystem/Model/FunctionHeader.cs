
namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // A "Header" object is an abstraction layer that interprets domain/context-free
    // information from a ShaderObject (in this case a function) and translates it into
    // domain specific information-- in this case, the Shader Graph tooling and conventions.
    internal struct FunctionHeader
    {
        readonly internal bool isValid;

        readonly internal string referenceName;
        readonly internal string displayName;
        readonly internal string uniqueName;
        readonly internal string tooltip;

        readonly internal IShaderType returnType;
        internal bool hasReturnType => returnType.Name != "void";
        readonly internal string returnDisplayName;

        readonly internal string category;

        readonly internal string[] searchTerms;

        internal FunctionHeader(IProvider<IShaderFunction> provider)
        {
            referenceName = null;
            returnType = null;
            displayName = null;
            category = null;
            searchTerms = null;
            returnDisplayName = null;
            uniqueName = null;
            tooltip = null;

            isValid = provider != null && provider.Definition != null;

            if (!isValid)
                return;
            
            var func = provider.Definition;
            var path = AssetDatabase.GUIDToAssetPath(provider.AssetID);

            referenceName = func.Name;
            returnType = func.ReturnType;

            if (!func.Hints.TryGetValue(Hints.Common.kDisplayName, out displayName))
                displayName = ObjectNames.NicifyVariableName(referenceName);

            if (!func.Hints.TryGetValue(Hints.Func.kSearchName, out uniqueName))
                uniqueName = ShaderObjectUtils.QualifySignature(func, false, true);

            if (func.Hints.TryGetValue(Hints.Func.kCategory, out category)) { }
            else if (func.Namespace != null)
            {
                category = "Reflected by Namespace";
                foreach (var name in func.Namespace)
                    category += $"/{name}";
            }
            else
            {
                category = $"Reflected by Path/{path}";
            }

            if (func.Hints.TryGetValue(Hints.Func.kSearchName, out var terms))
                searchTerms = HeaderUtils.LazyTokenString(terms);
            else searchTerms = new[] { referenceName };

            if (!func.Hints.TryGetValue(Hints.Func.kReturnDisplayName, out returnDisplayName))
                returnDisplayName = "Out";
        }
    }
}
