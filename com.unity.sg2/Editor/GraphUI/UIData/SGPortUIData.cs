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
    }
}
