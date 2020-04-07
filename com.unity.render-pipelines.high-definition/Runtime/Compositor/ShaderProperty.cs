using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition.Compositor
{
    [System.Serializable]
    internal class ShaderProperty
    {
        public string m_PropertyName;
        public ShaderPropertyType m_Type;
        public Vector4 m_Value;
        public Vector2 m_RangeLimits;
        public ShaderPropertyFlags m_Flags;

        public static ShaderProperty Create(Shader shader, Material material, int index)
        {
            ShaderProperty sp = new ShaderProperty();
            {
                sp.m_PropertyName = shader.GetPropertyName(index);
                sp.m_Type = shader.GetPropertyType(index);
                sp.m_Flags = shader.GetPropertyFlags(index);
                sp.m_Value = Vector4.zero;

                if (sp.m_Type == ShaderPropertyType.Range)
                {
                    sp.m_RangeLimits = shader.GetPropertyRangeLimits(index);
                    sp.m_Value = new Vector4(material.GetFloat(Shader.PropertyToID(shader.GetPropertyName(index))), 0.0f, 0.0f, 0.0f);
                }
                else if (sp.m_Type == ShaderPropertyType.Color)
                {
                    sp.m_Value = material.GetColor(Shader.PropertyToID(shader.GetPropertyName(index)));
                }
                else if (sp.m_Type == ShaderPropertyType.Vector)
                {
                    sp.m_Value = material.GetVector(Shader.PropertyToID(shader.GetPropertyName(index)));
                }
            }
            return sp;
        }
    }
}
