using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class PIConstant : MathNode
    {
        public override string Title
        {
            get => "π";
            set {}
        }

        public override Value Evaluate()
        {
            return Mathf.PI;
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            this.AddDataOutputPort<float>("");
        }
    }
}
