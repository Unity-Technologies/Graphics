using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Operators/Comparisons/Less Or Equal")]
    [SeacherHelp("Outputs true only if the second input is less or equal to the first one.")]
    public class LessOrEqualOperator : CompareOperator
    {
        public override string Title
        {
            get => "Less Or Equal";
            set {}
        }

        public override bool Compare(float a, float b)
        {
            return a <= b;
        }

        /// <inheritdoc />
        public override string CSharpCompareOperator()
        {
            return "<=";
        }
    }
}
