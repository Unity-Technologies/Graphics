using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class PowFunction : MathFunction
    {
        public override string Title
        {
            get => "Pow";
            set {}
        }

        public PowFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "f", "p" };
            }
        }

        public override float EvaluateFloat()
        {
            return Mathf.Pow(GetParameterValue(0), GetParameterValue(1));
        }
    }
}
