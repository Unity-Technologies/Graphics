using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class CosFunction : MathFunction
    {
        public override string Title
        {
            get => "Cos";
            set {}
        }

        public CosFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "f" };
            }
        }

        public override float EvaluateFloat()
        {
            return Mathf.Cos(GetParameterValue(0));
        }
    }
}
