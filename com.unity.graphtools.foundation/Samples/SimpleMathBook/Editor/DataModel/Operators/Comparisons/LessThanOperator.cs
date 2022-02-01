using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class LessThanOperator : CompareOperator
    {
        public override string Title
        {
            get => "Less Than";
            set {}
        }

        public override bool Compare(float a, float b)
        {
            return a < b;
        }
    }
}
