using System;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEditor.ShaderGraph.GraphUI.DataModel;

namespace UnityEditor.ShaderGraph.GraphUI.EditorCommon.Preview
{
    struct PreviewProperty<PropertyType>
    {
        public string name { get; set; }
        public Type propertyType => typeof(PropertyType);

        public PropertyType value { get; private set; }

        public PreviewProperty(IFieldReader fieldReader) : this()
        {
            //value = fieldReader.TryGetValue<PropertyType>();
        }

        //Texture2DShaderProperty.DefaultType m_texture2dDefaultType;

        const string k_SetErrorMessage = "Cannot set a {0} property on a PreviewProperty with type {1}.";
        const string k_GetErrorMessage = "Cannot get a {0} property on a PreviewProperty with type {1}.";

        public void SetValueOnMaterialPropertyBlock(MaterialPropertyBlock mat)
        {
           /* if ((propertyType == PropertyType.Texture2D || propertyType == PropertyType.Texture2DArray || propertyType == PropertyType.Texture3D))
            {
                if (m_ClassData.textureValue == null)
                {
                    // there's no way to set the texture back to NULL
                    // and no way to delete the property either
                    // so instead we set the value to what we know the default will be
                    // (all textures in ShaderGraph default to white)
                    switch (m_texture2dDefaultType)
                    {
                        case Texture2DShaderProperty.DefaultType.White:
                            mat.SetTexture(name, Texture2D.whiteTexture);
                            break;
                        case Texture2DShaderProperty.DefaultType.Black:
                            mat.SetTexture(name, Texture2D.blackTexture);
                            break;
                        case Texture2DShaderProperty.DefaultType.Grey:
                            mat.SetTexture(name, Texture2D.grayTexture);
                            break;
                        case Texture2DShaderProperty.DefaultType.NormalMap:
                            mat.SetTexture(name, Texture2D.normalTexture);
                            break;
                        case Texture2DShaderProperty.DefaultType.LinearGrey:
                            mat.SetTexture(name, Texture2D.linearGrayTexture);
                            break;
                        case Texture2DShaderProperty.DefaultType.Red:
                            mat.SetTexture(name, Texture2D.redTexture);
                            break;
                    }
                }
                else
                    mat.SetTexture(name, m_ClassData.textureValue);
            }
            else if (propertyType == PropertyType.Cubemap)
            {
                if (m_ClassData.cubemapValue == null)
                {
                    // there's no way to set the texture back to NULL
                    // and no way to delete the property either
                    // so instead we set the value to what we know the default will be
                    // (all textures in ShaderGraph default to white)
                    // there's no Cubemap.whiteTexture, but this seems to work
                    mat.SetTexture(name, Texture2D.whiteTexture);
                }
                else
                    mat.SetTexture(name, m_ClassData.cubemapValue);
            }
            else if (propertyType == PropertyType.Color)
                mat.SetColor(name, m_StructData.colorValue);
            else if (propertyType == PropertyType.Vector2 || propertyType == PropertyType.Vector3 || propertyType == PropertyType.Vector4)
                mat.SetVector(name, m_StructData.vector4Value);
            else if (propertyType == PropertyType.Float)
                mat.SetFloat(name, m_StructData.floatValue);
            else if (propertyType == PropertyType.Boolean)
                mat.SetFloat(name, m_StructData.booleanValue ? 1 : 0);
            else if (propertyType == PropertyType.Matrix2 || propertyType == PropertyType.Matrix3 || propertyType == PropertyType.Matrix4)
                mat.SetMatrix(name, m_StructData.matrixValue);
            else if (propertyType == PropertyType.Gradient)
            {
                mat.SetFloat(string.Format("{0}_Type", name), (int)m_ClassData.gradientValue.mode);
                mat.SetFloat(string.Format("{0}_ColorsLength", name), m_ClassData.gradientValue.colorKeys.Length);
                mat.SetFloat(string.Format("{0}_AlphasLength", name), m_ClassData.gradientValue.alphaKeys.Length);
                for (int i = 0; i < 8; i++)
                    mat.SetVector(string.Format("{0}_ColorKey{1}", name, i), i < m_ClassData.gradientValue.colorKeys.Length ? GradientUtil.ColorKeyToVector(m_ClassData.gradientValue.colorKeys[i]) : Vector4.zero);
                for (int i = 0; i < 8; i++)
                    mat.SetVector(string.Format("{0}_AlphaKey{1}", name, i), i < m_ClassData.gradientValue.alphaKeys.Length ? GradientUtil.AlphaKeyToVector(m_ClassData.gradientValue.alphaKeys[i]) : Vector2.zero);
            }
            else if (propertyType == PropertyType.VirtualTexture)
            {
                // virtual texture assignments are not supported via the material property block, we must assign them to the materials
            }*/
        }
    }
}
