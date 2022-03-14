using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class TanFunction : MathFunction
    {
        public override string Title
        {
            get => "Tan";
            set {}
        }

        public TanFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "f" };
            }
        }

        public override float EvaluateFloat()
        {
            return Mathf.Tan(GetParameterValue(0));
        }
    }
}
