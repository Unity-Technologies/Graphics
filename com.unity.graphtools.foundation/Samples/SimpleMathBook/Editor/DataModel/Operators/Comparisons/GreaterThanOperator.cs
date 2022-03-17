using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Operators/Comparisons/Greater Than")]
    [SeacherHelp("Outputs true only if the second input is greater than the first one.")]
    public class GreaterThanOperator : CompareOperator
    {
        public override string Title
        {
            get => "Greater Than";
            set {}
        }

        public override bool Compare(float a, float b)
        {
            return a > b;
        }

        /// <inheritdoc />
        public override string CSharpCompareOperator()
        {
            return ">";
        }
    }
}
