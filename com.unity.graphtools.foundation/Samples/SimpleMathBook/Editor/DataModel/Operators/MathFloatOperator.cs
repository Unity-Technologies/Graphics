using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    public abstract class MathFloatOperator : MathOperator
    {
        protected List<float> FloatValues => this.GetInputPorts().Select(portModel => portModel == null ? 0 : GetValue(portModel).Float).ToList();
        public abstract float EvaluateFloat();

        public override Value Evaluate()
        {
            return EvaluateFloat();
        }
    }
}
