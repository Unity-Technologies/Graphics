using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Defs
{
    /// <summary>
    /// A ParameterUIDescriptor indicates how a shader function's parameter should
    /// be displayed in a node UI.
    /// </summary>
    internal readonly struct ParameterUIDescriptor
    {
        public string Name { get; }
        public string DisplayName { get; }
        public string Tooltip { get; }
        public bool UseColor { get; }
        public bool IsHdr { get; }
        public bool UseSlider { get; }
        public bool InspectorOnly { get; }
        public readonly List<(string, object)> Options { get; }
        public string DefaultOption { get; }

        public ParameterUIDescriptor(
            string name,
            string displayName = null,
            string tooltip = "",
            bool useColor = false,
            bool isHdr = false,
            bool useSlider = false,
            bool inspectorOnly = false,
            List<(string, object)> options = null,
            string defaultOption = null
        )
        {
            Name = name;
            DisplayName = (displayName ?? name ?? string.Empty).Trim();
            Tooltip = tooltip;
            UseColor = useColor;
            IsHdr = isHdr;
            UseSlider = useSlider;
            InspectorOnly = inspectorOnly;
            Options = options;
            DefaultOption = defaultOption;
        }
    }
}
