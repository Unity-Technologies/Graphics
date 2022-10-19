using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.GraphUI.UIData
{
    /// <summary>
    /// This struct holds the Application/Tool-side info. about the UI data relevant to a port
    /// </summary>
    public struct SGPortUIData
    {
        public string Name { get; }
        public string DisplayName { get; }
        public string Tooltip { get; }
        public bool UseColor { get; }
        public bool IsHdr { get; }
        public bool IsStatic { get; }
        public bool IsGradient { get; }
        public bool UseSlider { get; }
        public bool InspectorOnly { get; }
        public readonly List<(string, object)> Options { get; }


        public SGPortUIData(
            string name,
            string displayName = null,
            string tooltip = "",
            bool useColor = false,
            bool isHdr = false,
            bool isStatic = false,
            bool isGradient = false,
            bool useSlider = false,
            bool inspectorOnly = false,
            List<(string, object)> options = null
        )
        {
            Name = name;
            DisplayName = displayName ?? name;
            Tooltip = tooltip;
            UseColor = useColor;
            IsHdr = isHdr;
            IsStatic = isStatic;
            IsGradient = isGradient;
            UseSlider = useSlider;
            InspectorOnly = inspectorOnly;
            Options = options;
        }
    }
}
