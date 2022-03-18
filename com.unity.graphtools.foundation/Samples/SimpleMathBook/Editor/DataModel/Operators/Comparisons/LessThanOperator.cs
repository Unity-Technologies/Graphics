using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Operators/Comparisons/Less Than")]
    [SeacherHelp("Outputs true only if the second input is lower than the first one.")]
    public class LessThanOperator : CompareOperator
    {
        public override string Title
        {
            get => "Less Than";
            set {}
        }

        public override bool Compare(float a, float b)
        {
            return a < b;
        }

        /// <inheritdoc />
        public override string CSharpCompareOperator()
        {
            return "<";
        }
    }
}
