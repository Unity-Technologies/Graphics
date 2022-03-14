using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public abstract class MathOperator : MathNode
    {
        [SerializeField, HideInInspector]
        int m_InputPortCount = 2;

        public List<Value> Values => this.GetInputPorts().Select(portModel => portModel == null ? 0 : GetValue(portModel)).ToList();

        public int InputPortCount
        {
            get => m_InputPortCount;
            set => m_InputPortCount = Math.Max(2, value);
        }

        public IPortModel DataOut { get; private set; }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            AddOutputPorts();
            AddInputPorts();
        }

        protected virtual void AddInputPorts()
        {
            for (var i = 0; i < InputPortCount; ++i)
                this.AddDataInputPort<float>("Port " + i);
        }

        protected virtual void AddOutputPorts()
        {
            this.AddDataOutputPort<float>("Output");
        }
    }
}
