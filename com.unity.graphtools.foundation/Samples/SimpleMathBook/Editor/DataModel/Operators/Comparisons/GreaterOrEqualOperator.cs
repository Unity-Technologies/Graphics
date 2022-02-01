using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class GreaterOrEqualOperator : CompareOperator
    {
        public override string Title
        {
            get => "Greater Or Equal";
            set {}
        }

        public override bool Compare(float a, float b)
        {
            return a >= b;
        }
    }
}
