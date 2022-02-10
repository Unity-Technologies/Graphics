using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class Vector3MagnitudeFunction : MathNode
    {
        public override string Title
        {
            get => "Vector3 Magnitude";
            set {}
        }

        public override ValueType[] ValueInputTypes => new[] { ValueType.Vector3 };

        public IPortModel Input { get; private set; }

        public override Value Evaluate()
        {
            return GetValue(Input).Vector3.magnitude;
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            Input = this.AddDataInputPort<Vector3>("Input");
            this.AddDataOutputPort<float>("Output");
        }
    }
}
