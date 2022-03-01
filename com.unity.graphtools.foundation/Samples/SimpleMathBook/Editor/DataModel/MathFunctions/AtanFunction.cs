using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class AtanFunction : MathFunction
    {
        public override string Title
        {
            get => "Atan";
            set {}
        }

        public AtanFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "f" };
            }
        }

        public override float EvaluateFloat()
        {
            return Mathf.Atan(GetParameterValue(0));
        }
    }
}
