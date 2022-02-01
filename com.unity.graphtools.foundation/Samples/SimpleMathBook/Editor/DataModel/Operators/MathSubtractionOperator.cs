using System;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MathSubtractionOperator : MathFloatOperator
    {
        public override string Title
        {
            get => "Subtract";
            set {}
        }

        public override float EvaluateFloat()
        {
            return FloatValues.Skip(1).Aggregate(FloatValues.FirstOrDefault(), (current, value) => current - value);
        }

        protected override void AddInputPorts()
        {
            for (var i = 0; i < InputPortCount; ++i)
                this.AddDataInputPort<float>(i == 0 ? "Minuend" : "Subtrahend " + i);
        }
    }
}
