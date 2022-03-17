using System;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Operators/Boolean Logic/And")]
    [SeacherHelp("Outputs <i>true</i> if <u>all</u> boolean inputs are <i>true</i>.")]
    public class AndOperator : BoolLogicOperator
    {
        public override string Title
        {
            get => "And";
            set {}
        }

        public override bool EvaluateLogic()
        {
            return BoolValues.Aggregate(true, (a, b) => a && b);
        }

        /// <inheritdoc />
        protected override (string op, bool isInfix) GetCSharpOperator()
        {
            return ("&&", true);
        }
    }
}
