using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class RoundFunction : MathFunction
    {
        public override string Title
        {
            get => "Round";
            set {}
        }

        public RoundFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "f" };
            }
        }

        public override float EvaluateFloat()
        {
            return Mathf.Round(GetParameterValue(0));
        }
    }
}
