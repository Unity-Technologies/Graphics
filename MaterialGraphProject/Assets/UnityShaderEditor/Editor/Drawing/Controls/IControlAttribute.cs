using System.Reflection;
using UnityEngine.Experimental.UIElements;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Controls
{
    public interface IControlAttribute
    {
        VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo);
    }
}
