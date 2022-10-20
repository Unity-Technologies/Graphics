using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor.ShaderGraph.Defs;

namespace UnityEditor.ShaderGraph.GraphUI.UIData
{
    /// <summary>
    /// This struct holds the Application/Tool-side info. about the UI data relevant to a node
    /// </summary>
    public struct SGNodeUIData
    {
        public int Version { get; }
        public string Name { get; }
        public string DisplayName { get; }
        public string Tooltip { get; }
        public bool HasPreview { get; }
        public IReadOnlyDictionary<string, string> SelectableFunctions { get; }
        public IReadOnlyCollection<SGPortUIData> Parameters { get; }
        public IReadOnlyCollection<string> Synonyms { get; }
        public string Category { get; }
        public string FunctionSelectorLabel { get; }

        public SGNodeUIData(
            int version,
            string name, // should match the name in a FunctionDesctriptor
            string tooltip,
            string category,
            string[] synonyms,
            string displayName = null,
            bool hasPreview = true, // By default we assume all nodes should have previews,
            Dictionary<string, string> selectableFunctions = null,
            SGPortUIData[] parameters = null,
            string functionSelectorLabel = ""
        )
        {
            Version = version;
            Name = name;
            DisplayName = displayName ?? name;
            Tooltip = tooltip;
            Synonyms = synonyms.ToList().AsReadOnly();
            Category = category;
            HasPreview = hasPreview;
            var functionDictionary = selectableFunctions ?? new Dictionary<string, string>();
            SelectableFunctions = new ReadOnlyDictionary<string, string>(functionDictionary);
            var parametersList = parameters ?? new SGPortUIData[0];
            Parameters = parametersList.ToList().AsReadOnly();
            FunctionSelectorLabel = functionSelectorLabel;
        }

    }
}
