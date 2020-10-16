using static UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers.ShaderInputPropertyDrawer;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    interface IShaderPropertyDrawer
    {
        internal void HandlePropertyField(PropertySheet propertySheet, PreChangeValueCallback preChangeValueCallback, PostChangeValueCallback postChangeValueCallback);
    }
} 
