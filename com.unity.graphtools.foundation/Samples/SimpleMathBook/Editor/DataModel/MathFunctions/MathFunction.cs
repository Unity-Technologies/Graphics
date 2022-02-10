using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public abstract class MathFunction : MathNode
    {
        [SerializeField]
        protected string[] m_ParameterNames = new string[0];

        public float GetParameterValue(int index)
        {
            if (InputsByDisplayOrder.Count <= index || InputsByDisplayOrder[index] == null)
            {
                Debug.LogError("Access to unavailable port " + index);
                return 0;
            }

            return GetValue(InputsByDisplayOrder[index]).Float;
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            foreach (var name in m_ParameterNames)
            {
                this.AddDataInputPort<float>(name);
            }
            this.AddDataOutputPort<float>("out");
        }

        public abstract float EvaluateFloat();

        public override Value Evaluate()
        {
            return EvaluateFloat();
        }
    }
}
