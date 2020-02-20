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

        public static ShaderProperty Create(Shader shader, Material material, int indx)
        {
            ShaderProperty sp = new ShaderProperty();
            {
                sp.m_PropertyName = shader.GetPropertyName(indx);
                sp.m_Type = shader.GetPropertyType(indx);
                sp.m_Flags = shader.GetPropertyFlags(indx);
                sp.m_Value = Vector4.zero;

                if (sp.m_Type == ShaderPropertyType.Range)
                {
                    sp.m_RangeLimits = shader.GetPropertyRangeLimits(indx);
                    sp.m_Value = new Vector4(material.GetFloat(Shader.PropertyToID(shader.GetPropertyName(indx))), 0.0f, 0.0f, 0.0f);
                }
                else if (sp.m_Type == ShaderPropertyType.Color)
                {
                    sp.m_Value = material.GetColor(Shader.PropertyToID(shader.GetPropertyName(indx)));
                }
                else if (sp.m_Type == ShaderPropertyType.Vector)
                {
                    sp.m_Value = material.GetVector(Shader.PropertyToID(shader.GetPropertyName(indx)));
                }
            }
            return sp;
        }
    }
}
