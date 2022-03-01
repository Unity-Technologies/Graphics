using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class LessOrEqualOperator : CompareOperator
    {
        public override string Title
        {
            get => "Less Or Equal";
            set {}
        }

        public override bool Compare(float a, float b)
        {
            return a <= b;
        }
    }
}
