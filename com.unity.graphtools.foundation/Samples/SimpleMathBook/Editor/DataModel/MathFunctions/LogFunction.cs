using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class LogFunction : MathFunction
    {
        public override string Title
        {
            get => "Log";
            set {}
        }

        public LogFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "f", "p" };
            }
        }

        public override float EvaluateFloat()
        {
            return Mathf.Log(GetParameterValue(0), GetParameterValue(1));
        }
    }
}
