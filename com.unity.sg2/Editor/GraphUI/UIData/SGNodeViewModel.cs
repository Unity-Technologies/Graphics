using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// This struct holds the Application/Tool-side info. about the UI data relevant to a node
    /// </summary>
    struct SGNodeViewModel
    {
        public int Version { get; }
        public string Name { get; }
        public string DisplayName { get; }
        public string Tooltip { get; }
        public bool HasPreview { get; }
        public IReadOnlyDictionary<string, string> SelectableFunctions { get; }
        public IReadOnlyCollection<SGPortViewModel> PortUIData { get; }
        public IReadOnlyCollection<string> Synonyms { get; }
        public string Category { get; }
        public string FunctionSelectorLabel { get; }
        public string SelectedFunctionID { get; }

        public IEnumerable<SGPortViewModel> StaticPortUIData
        {
            get
            {
                foreach (var param in PortUIData)
                {
                    if (param.IsStatic) yield return param;
                }
            }
        }

        public SGNodeViewModel(
            int version,
            string name, // should match the name in a FunctionDesctriptor
            string tooltip,
            string category,
            string[] synonyms,
            string displayName = null,
            bool hasPreview = true, // By default we assume all nodes should have previews,
            Dictionary<string, string> selectableFunctions = null,
            SGPortViewModel[] parameters = null,
            string functionSelectorLabel = "",
            string selectedFunctionID = ""
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
            var parametersList = parameters ?? new SGPortViewModel[0];
            PortUIData = parametersList.ToList().AsReadOnly();
            FunctionSelectorLabel = functionSelectorLabel;
            SelectedFunctionID = selectedFunctionID;
        }

        public SGPortViewModel GetParameterInfo(string parameterName)
        {
            if (PortUIData == null)
                return new SGPortViewModel();

            foreach (var parameter in PortUIData)
                if (parameter.Name == parameterName)
                    return parameter;

            return new SGPortViewModel();
        }
    }
}
