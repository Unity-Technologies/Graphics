using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry
{
    internal struct Workflow
    {
        string m_Name;
        List<CustomizationPoint> m_CustomizationPoints;
        internal string Name => m_Name;
        internal IEnumerable<CustomizationPoint> CustomizationPoints => m_CustomizationPoints;

        internal bool IsValid => m_Name != null;
        internal static Workflow Invalid => new Workflow(null, null);

        internal Workflow(string name, List<CustomizationPoint> customizationPoints)
        {
            m_Name = name;
            m_CustomizationPoints = customizationPoints;
        }

        internal class Builder
        {
            internal string Name { get; set; }
            List<CustomizationPoint> customizationPoints = new List<CustomizationPoint>();

            internal void AddCustomizationPoint(CustomizationPoint customizationPoint)
            {
                customizationPoints.Add(customizationPoint);
            }

            internal Workflow Build(ShaderContainer container)
            {
                return new Workflow(Name, customizationPoints);
            }
        }
    }
}
