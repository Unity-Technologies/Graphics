using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace UnityEditor.ShaderGraph.Defs
{

    /// <summary>
    /// A NodeUIDescriptor stores parameters needed to display a shader
    /// function as a node.
    /// </summary>
    readonly struct NodeUIDescriptor
    {
        public int Version { get; }
        public string Name { get; }
        public string DisplayName { get; }
        public string Tooltip { get; }
        public bool HasPreview { get; }
        public IReadOnlyDictionary<string, string> SelectableFunctions { get; }
        public IReadOnlyCollection<ParameterUIDescriptor> Parameters { get; }
        public IReadOnlyCollection<string> Synonyms { get; }
        public IReadOnlyCollection<string> Categories { get; }

        public NodeUIDescriptor(
            int version,
            string name, // should match the name in a FunctionDesctriptor
            string tooltip,
            string[] categories,
            string[] synonyms,
            string displayName = null,
            bool hasPreview = true, // By default we assume all nodes should have previews,
            Dictionary<string, string> selectableFunctions = null,
            ParameterUIDescriptor[] parameters = null
        )
        {
            Version = version;
            Name = name;
            DisplayName = displayName ?? name;
            Tooltip = tooltip;
            Synonyms = synonyms.ToList().AsReadOnly();
            Categories = categories.ToList().AsReadOnly();
            HasPreview = hasPreview;
            var functionDictionary = selectableFunctions ?? new Dictionary<string, string>();
            SelectableFunctions = new ReadOnlyDictionary<string, string>(functionDictionary);
            var parametersList = parameters ?? new ParameterUIDescriptor[0];
            Parameters = parametersList.ToList().AsReadOnly();
        }

        public ParameterUIDescriptor GetParameterInfo(string parameterName)
        {
            if(Parameters == null)
                return new ParameterUIDescriptor(parameterName);

            foreach(var parameter in Parameters)
                if (parameter.Name == parameterName)
                    return parameter;

			return new ParameterUIDescriptor(parameterName);
        }

    }
}
