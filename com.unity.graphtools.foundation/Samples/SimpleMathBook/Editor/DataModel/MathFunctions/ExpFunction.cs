using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class ExpFunction : MathFunction
    {
        public override string Title
        {
            get => "Exp";
            set {}
        }

        public ExpFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "f" };
            }
        }

        public override float EvaluateFloat()
        {
            return Mathf.Exp(GetParameterValue(0));
        }
    }
}
