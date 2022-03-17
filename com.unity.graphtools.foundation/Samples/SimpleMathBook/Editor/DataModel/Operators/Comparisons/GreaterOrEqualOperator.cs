using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Operators/Comparisons/Greater Or Equal")]
    [SeacherHelp("Outputs true only if the second input is greater or equal to the first one.")]
    public class GreaterOrEqualOperator : CompareOperator
    {
        public override string Title
        {
            get => "Greater Or Equal";
            set {}
        }

        public override bool Compare(float a, float b)
        {
            return a >= b;
        }

        /// <inheritdoc />
        public override string CSharpCompareOperator()
        {
            return ">=";
        }
    }
}
