using System;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MathMultiplicationOperator : MathFloatOperator
    {
        public override string Title
        {
            get => "Multiply";
            set {}
        }

        public override float EvaluateFloat()
        {
            return FloatValues.Skip(1).Aggregate(FloatValues.FirstOrDefault(), (current, value) => current * value);
        }

        protected override void AddInputPorts()
        {
            for (var i = 0; i < InputPortCount; ++i)
                this.AddDataInputPort<float>("Factor " + (i + 1));
        }
    }
}
