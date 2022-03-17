using System;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Operators/Boolean Logic/Or")]
    [SeacherHelp("Outputs true if at least one boolean input is true.")]
    public class OrOperator : BoolLogicOperator
    {
        public override string Title
        {
            get => "Or";
            set {}
        }

        public override bool EvaluateLogic()
        {
            return BoolValues.Aggregate(false, (a, b) => a || b);
        }

        /// <inheritdoc />
        protected override (string op, bool isInfix) GetCSharpOperator()
        {
            return ("||", true);
        }
    }
}
