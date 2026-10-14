using Unity.GraphAuthoring.Editor.ProviderSystem;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // TODO: Headers copy what the definition and provider already carry; read those directly and remove the headers.
    internal class FunctionHeader : HeaderBase<IShaderFunction>
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

        internal string helpUrl { get; private set; }

        internal string version { get; private set; }

        internal string groupKey { get; private set; }
        internal string groupDropdownEntryName { get; private set; }
        internal string groupLabel { get; private set; }

        internal string[] groupSearchTerms { get; private set; }

        internal (IProvider<IShaderFunction> provider, string label)[] groupCandidates { get; private set; }
        internal (IProvider<IShaderFunction> provider, string version)[] versionCandidates { get; private set; }


        bool skipGroup = false;
        private void OnProcessNoGroup(IShaderFunction func, IProvider provider)
        {
            skipGroup = true;
            OnProcess(func, provider);
            skipGroup = false;
        }

        protected override void OnProcess(IShaderFunction func, IProvider provider)
        {
            referenceName = func.Name;
            returnType    = func.ReturnType;
            version       = provider.Version;

            var presentation = func.Presentation;
            displayName = presentation.DisplayName;
            tooltip     = presentation.Tooltip;

            returnDisplayName = presentation.ReturnDisplayName;
            returnTooltip     = presentation.ReturnTooltip;

            searchName     = presentation.SearchName;
            searchTerms    = presentation.SearchTerms != null ? new List<string>(presentation.SearchTerms).ToArray() : null;
            searchCategory = presentation.SearchCategory;

            allowPrecision = func.Production.SupportsPrecision;

            helpUrl = presentation.HelpUrl;

            if (provider.Group != null)
            {
                groupDropdownEntryName = displayName;
                displayName = presentation.GroupDisplayName ?? provider.Group;
                searchName = displayName;
                groupLabel = presentation.GroupLabel ?? "Variant";

                List<string> searchTerms = new();
                List<(IProvider<IShaderFunction> provider, string label)> groupEntries = new();

                if (!skipGroup && ProviderLibrary.TryGetInstance(out var lib))
                {
                    var groupHeader = new FunctionHeader();
                    bool first = true;
                    foreach (var groupProvider in lib.ProvidersByGroup<IShaderFunction>(provider.Group))
                    {
                        groupHeader.OnProcessNoGroup(groupProvider.Definition, groupProvider);
                        if (first) // Main provider's information wins.
                        {
                            displayName = groupHeader.displayName;
                            groupLabel = groupHeader.groupLabel;
                            helpUrl ??= groupHeader.helpUrl;
                            first = false;
                        }

                        groupEntries.Add((groupProvider, groupHeader.groupDropdownEntryName));

                        // aggregate search terms from all headers.
                        searchTerms.AddRange(groupHeader.searchTerms);
                    }
                    groupCandidates = groupEntries.ToArray();
                    this.searchTerms = searchTerms.ToArray();
                }
                else
                {
                    groupEntries.Add(((IProvider<IShaderFunction>)provider, groupDropdownEntryName));
                }
            }

            // Version:
            List<(IProvider<IShaderFunction> provider, string version)> rawVersions = new();
            if (ProviderLibrary.TryGetInstance(out var versionLib))
            {                
                foreach(var versionedProvider in versionLib.ProvidersByVersion<IShaderFunction>(provider.ProviderKey))
                    rawVersions.Add((versionedProvider, versionedProvider.Version));                
            }
            else
            {
                rawVersions.Add((provider as IProvider<IShaderFunction>, provider.Version));
            }
            versionCandidates = rawVersions.ToArray();

            // Return:
            returnHeader = new ParameterHeader(returnDisplayName, func.ReturnType, returnTooltip, provider);
        }
    }
}
