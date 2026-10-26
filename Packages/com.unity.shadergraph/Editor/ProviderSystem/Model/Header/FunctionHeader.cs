using Unity.GraphAuthoring.Editor.ProviderSystem;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // Formats information from the Definition for easier use by the Node.
    internal class FunctionHeader
    {
        internal string referenceName { get; private set; }

        internal string displayName { get; private set; }
        internal string tooltip { get; private set; }

        internal string searchName { get; private set; }
        internal string[] searchTerms { get; private set; }
        internal string searchCategory { get; private set; }

        internal bool allowPrecision { get; private set; }

        internal string helpUrl { get; private set; }

        internal string version { get; private set; }

        internal string groupKey { get; private set; }
        internal string groupDropdownEntryName { get; private set; }
        internal string groupLabel { get; private set; }

        internal string[] groupSearchTerms { get; private set; }

        internal (IProvider<IShaderFunction> provider, string label)[] groupCandidates { get; private set; }
        internal (IProvider<IShaderFunction> provider, string version)[] versionCandidates { get; private set; }

        internal IReadOnlyList<(string subject, string text, MessageType severity)> messages { get; private set; }

        internal FunctionHeader(IShaderFunction func, IProvider provider) : this(func, provider, false) { }

        FunctionHeader(IShaderFunction func, IProvider provider, bool skipGroup)
        {
            referenceName = func.Name;
            version       = provider.Version;

            var presentation = func.Presentation;
            displayName = presentation.DisplayName;
            tooltip     = presentation.Tooltip;

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
                    bool first = true;
                    foreach (var groupProvider in lib.ProvidersByGroup<IShaderFunction>(provider.Group))
                    {
                        var groupHeader = new FunctionHeader(groupProvider.Definition, groupProvider, true);
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

            if (skipGroup)
                return;

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

            messages = CollectMessages(func, provider);
        }

        static List<(string, string, MessageType)> CollectMessages(IShaderFunction func, IProvider provider)
        {
            List<(string, string, MessageType)> messages = new();
            if (provider.Messages == null)
                return messages;

            List<string> subjects = new() { null };
            foreach (var param in func.Parameters)
                if (!subjects.Contains(param.Name))
                    subjects.Add(param.Name);

            foreach (var subject in subjects)
                foreach (var message in provider.Messages)
                    if (message.Subject == subject)
                        messages.Add((subject, message.Text, (MessageType)message.Severity));
            return messages;
        }
    }
}
