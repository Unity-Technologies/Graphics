using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class Vector3DotProduct : MathNode
    {
        public override string Title
        {
            get => "Vector3 Dot Product";
            set {}
        }

        public override ValueType[] ValueInputTypes => new[] { ValueType.Vector3 };

        public IPortModel InputA { get; private set; }
        public IPortModel InputB { get; private set; }

        public override Value Evaluate()
        {
            return Vector3.Dot(GetValue(InputA).Vector3, GetValue(InputB).Vector3);
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            InputA = this.AddDataInputPort<Vector3>("A");
            InputB = this.AddDataInputPort<Vector3>("B");
            this.AddDataOutputPort<float>("Output");
        }
    }
}
