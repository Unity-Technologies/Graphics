using System.Reflection;
using Data.Interfaces;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;

namespace Drawing.Inspector
{
    [SGPropertyDrawer(typeof(GraphData))]
    public class GraphDataPropertyDrawer : IPropertyDrawer
    {
        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            throw new System.NotImplementedException();
        }
    }
}
