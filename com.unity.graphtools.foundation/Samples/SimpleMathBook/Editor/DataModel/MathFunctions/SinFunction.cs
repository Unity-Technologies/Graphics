using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class SinFunction : MathFunction
    {
        public override string Title
        {
            get => "Sin";
            set {}
        }

        public SinFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "f" };
            }
        }

        public override float EvaluateFloat()
        {
            return Mathf.Sin(GetParameterValue(0));
        }
    }
}
