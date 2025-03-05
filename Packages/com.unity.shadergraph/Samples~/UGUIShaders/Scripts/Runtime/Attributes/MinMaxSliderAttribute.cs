using UnityEngine;

namespace Unity.UI.Shaders.Sample
{
    public class MinMaxSliderAttribute : PropertyAttribute
    {
        public float min;
        public float max;

        public MinMaxSliderAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }
}
