using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class ClampFunction : MathFunction
    {
        public override string Title
        {
            get => "Clamp";
            set {}
        }

        public ClampFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "val", "min", "max" };
            }
        }

        public override float EvaluateFloat()
        {
            return Mathf.Clamp(GetParameterValue(0), GetParameterValue(1), GetParameterValue(2));
        }
    }
}
