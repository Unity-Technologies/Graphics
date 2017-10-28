using System.Reflection;
using UnityEngine.Experimental.UIElements;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    public interface IControlAttribute
    {
        VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo);
    }
}
