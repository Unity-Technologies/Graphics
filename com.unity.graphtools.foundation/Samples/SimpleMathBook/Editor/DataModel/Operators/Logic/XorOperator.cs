using System;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Operators/Boolean Logic/Xor")]
    [SeacherHelp("Outputs true only if each boolean input is different from the previous one.")]
    public class XorOperator : BoolLogicOperator
    {
        public override string Title
        {
            get => "Xor";
            set {}
        }

        public override bool EvaluateLogic()
        {
            return BoolValues.Aggregate(false, (a, b) => a ^ b);
        }

        /// <inheritdoc />
        protected override (string op, bool isInfix) GetCSharpOperator()
        {
            return ("^", true);
        }
    }
}
