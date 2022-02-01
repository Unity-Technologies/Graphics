using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class AsinFunction : MathFunction
    {
        public override string Title
        {
            get => "Asin";
            set {}
        }

        public AsinFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "f" };
            }
        }

        public override float EvaluateFloat()
        {
            return Mathf.Asin(GetParameterValue(0));
        }
    }
}
