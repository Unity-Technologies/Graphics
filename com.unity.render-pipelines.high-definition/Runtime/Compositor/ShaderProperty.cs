using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition.Compositor
{
    [System.Serializable]
    internal class ShaderProperty
    {
        public string propertyName;
        public ShaderPropertyType propertyType;
        public Vector4 value;
        public Vector2 rangeLimits;
        public ShaderPropertyFlags flags;

        public static ShaderProperty Create(Shader shader, Material material, int index)
        {
            ShaderProperty sp = new ShaderProperty();
            {
                sp.propertyName = shader.GetPropertyName(index);
                sp.propertyType = shader.GetPropertyType(index);
                sp.flags = shader.GetPropertyFlags(index);
                sp.value = Vector4.zero;

                if (sp.propertyType == ShaderPropertyType.Range)
                {
                    sp.rangeLimits = shader.GetPropertyRangeLimits(index);
                    sp.value = new Vector4(material.GetFloat(Shader.PropertyToID(shader.GetPropertyName(index))), 0.0f, 0.0f, 0.0f);
                }
                else if (sp.propertyType == ShaderPropertyType.Color)
                {
                    sp.value = material.GetColor(Shader.PropertyToID(shader.GetPropertyName(index)));
                }
                else if (sp.propertyType == ShaderPropertyType.Vector)
                {
                    sp.value = material.GetVector(Shader.PropertyToID(shader.GetPropertyName(index)));
                }
            }
            return sp;
        }
    }
}
