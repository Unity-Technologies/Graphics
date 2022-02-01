using System;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
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
    }
}
