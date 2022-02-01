using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public abstract class CompareOperator : MathNode
    {
        public IPortModel InputA { get; private set; }
        public IPortModel InputB { get; private set; }

        public abstract bool Compare(float a, float b);

        public override Value Evaluate()
        {
            return Compare(GetValue(InputA).Float, GetValue(InputB).Float);
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            InputA = this.AddDataInputPort<float>("A");
            InputB = this.AddDataInputPort<float>("B");
            this.AddDataOutputPort<bool>("out");
        }
    }
}
