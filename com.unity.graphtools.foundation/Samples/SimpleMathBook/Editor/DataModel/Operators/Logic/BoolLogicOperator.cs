using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public abstract class BoolLogicOperator : MathOperator
    {
        public override TypeHandle[] ValueInputTypes => new[] { TypeHandle.Bool };

        protected List<bool> BoolValues => this.GetInputPorts().Select(portModel => portModel != null && GetValue(portModel).Bool).ToList();

        public abstract bool EvaluateLogic();

        public override Value Evaluate()
        {
            return EvaluateLogic();
        }

        protected override void AddInputPorts()
        {
            for (var i = 0; i < InputPortCount; ++i)
                this.AddDataInputPort<bool>("Port " + i);
        }

        protected override void AddOutputPorts()
        {
            this.AddDataOutputPort<bool>("Output");
        }
    }
}
