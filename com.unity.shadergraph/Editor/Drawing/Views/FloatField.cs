
using System.Globalization;

namespace UnityEditor.ShaderGraph.Drawing
{
    class FloatField : UnityEngine.UIElements.DoubleField
    {
        protected override string ValueToString(double v)
        {
            return ((float)v).ToString(CultureInfo.InvariantCulture.NumberFormat);
        }
    }
}
