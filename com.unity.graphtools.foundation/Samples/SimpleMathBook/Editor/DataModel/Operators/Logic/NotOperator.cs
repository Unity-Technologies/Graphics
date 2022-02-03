using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class NotOperator : MathNode
    {
        public override string Title
        {
            get => "Not";
            set {}
        }

        public override ValueType[] ValueInputTypes => new[] { ValueType.Bool };

        public IPortModel Input { get; private set; }

        public override Value Evaluate()
        {
            return !GetValue(Input).Bool;
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            Input = this.AddDataInputPort<bool>("input");
            this.AddDataOutputPort<bool>("output");
        }
    }
}
