using System;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
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
    }
}
