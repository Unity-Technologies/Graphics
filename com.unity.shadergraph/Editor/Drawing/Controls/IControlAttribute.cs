using System.Reflection;

using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    interface IControlAttribute
    {
        VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo);
    }
}
