using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.GraphUI
{
    enum ComponentLength
    {
        One,
        Two,
        Three,
        Four,
        Unknown
    }

    enum NumericType
    {
        Bool,
        Int,
        Float,
        Unknown
    }

    /// <summary>
    /// This struct holds the Application/Tool-side info. about the UI data relevant to a port
    /// </summary>
    struct SGPortViewModel
    {
        public string Name { get; }
        public string DisplayName { get; }
        public string Tooltip { get; }
        public bool UseColor { get; }
        public bool IsHdr { get; }
        public bool IsStatic { get; }
        public bool IsGradient { get; }
        public ComponentLength ComponentLength { get; }
        public NumericType NumericType { get; }
        public int MatrixHeight { get; }
        public bool IsMatrix { get; }
        public bool UseSlider { get; }
        public bool InspectorOnly { get; }
        public readonly List<(string, object)> Options { get; }


        public SGPortViewModel(
            string name,
            string displayName = null,
            string tooltip = "",
            bool useColor = false,
            bool isHdr = false,
            bool isStatic = false,
            bool isGradient = false,
            ComponentLength componentLength = ComponentLength.Unknown,
            NumericType numericType = NumericType.Float,
            bool isMatrix = false,
            int matrixHeight = 0,
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
            ComponentLength = componentLength;
            NumericType = numericType;
            IsMatrix = isMatrix;
            MatrixHeight = matrixHeight;
            UseSlider = useSlider;
            InspectorOnly = inspectorOnly;
            Options = options;
        }
    }
}
