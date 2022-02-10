using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class GreaterThanOperator : CompareOperator
    {
        public override string Title
        {
            get => "Greater Than";
            set {}
        }

        public override bool Compare(float a, float b)
        {
            return a > b;
        }
    }
}
