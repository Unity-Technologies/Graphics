using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MaxOperator : MathFloatOperator
    {
        public override string Title
        {
            get => "Max";
            set {}
        }

        public override float EvaluateFloat()
        {
            return FloatValues.Skip(1).Aggregate(FloatValues.FirstOrDefault(), Mathf.Max);
        }

        protected override void AddInputPorts()
        {
            for (var i = 0; i < InputPortCount; ++i)
                this.AddDataInputPort<float>("Factor " + (i + 1));
        }
    }
}
