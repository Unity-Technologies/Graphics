using System;
using Editor.GraphUI.Utilities;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.DataModel;

namespace UnityEditor.ShaderGraph.GraphUI.EditorCommon.Preview
{
    public class VariablePreviewHandler
    {
        string m_Name;

        public string Name
        {
            get => m_Name;
            private set => m_Name = value;
        }

        public IConstant m_VariableConstant;

        object m_VariableValue = null;

        public object VariableValue
        {
            get => m_VariableValue;
            set => m_VariableValue = value;
        }

        public VariablePreviewHandler(IConstant variableConstant)
        {
            Name = Mock_GetHLSLParameterName(variableConstant);
            m_VariableConstant = variableConstant;
        }

        const string k_SetErrorMessage = "Cannot set a {0} property on a PreviewProperty with type {1}.";
        const string k_GetErrorMessage = "Cannot get a {0} property on a PreviewProperty with type {1}.";

        string Mock_GetHLSLParameterName(IConstant variableConstant)
        {
            // TODO: How to get the actual type of a port?
            // Explore once Esme finishes merging in types
            return String.Empty;
        }

        Type Mock_GetMaterialPropertyTypeOfPort(IConstant variableConstant)
        {
            // TODO: How to get the actual type of a port?
            // Explore once Esme finishes merging in types
            return null;
        }

        object Mock_GetMaterialPropertyValueOfPort(IConstant variableConstant)
        {
            // TODO: How to get the actual value of a port?
            // Esme/Liz will add generic object getters, we just need type to cast down to concrete types
            VariableValue = variableConstant.ObjectValue;
            return null;
        }

        DefaultTextureType Mock_GetDefaultTextureType(IConstant variableConstant)
        {
            // TODO: How to get the actual value of a default texture type?
            // Will probably be a field on the port/node
            return DefaultTextureType.White;
        }

        public void SetValueOnMaterialPropertyBlock(MaterialPropertyBlock mat)
        {
            var type = Mock_GetMaterialPropertyTypeOfPort(m_VariableConstant);
            var value = Mock_GetMaterialPropertyValueOfPort(m_VariableConstant);

            if ((type == typeof(Texture2D) /*|| propertyType == PropertyType.Texture2DArray*/ || type == typeof(Texture3D)))
            {
                if (value == null)
                {
                    // there's no way to set the texture back to NULL
                    // and no way to delete the property either
                    // so instead we set the value to what we know the default will be
                    // (all textures in ShaderGraph default to white)

                    DefaultTextureType defaultTextureType = Mock_GetDefaultTextureType(m_VariableConstant);

                    switch (defaultTextureType)
                    {
                        case DefaultTextureType.White:
                            mat.SetTexture(Name, Texture2D.whiteTexture);
                            break;
                        case DefaultTextureType.Black:
                            mat.SetTexture(Name, Texture2D.blackTexture);
                            break;
                        case DefaultTextureType.NormalMap:
                            mat.SetTexture(Name, Texture2D.normalTexture);
                            break;
                    }
                }
                else
                {
                    var textureValue = value as Texture;
                    mat.SetTexture(Name, textureValue);
                }
            }
            else if (type == typeof(Cubemap))
            {
                if (value == null)
                {
                    // there's no Cubemap.whiteTexture, but this seems to work
                    mat.SetTexture(Name, Texture2D.whiteTexture);
                }
                else
                {
                    var cubemapValue = value as Cubemap;
                    mat.SetTexture(Name, cubemapValue);
                }
            }
            else if (type == typeof(Color))
            {
                var colorValue = value is Color colorVal ? colorVal : default;
                mat.SetColor(Name, colorValue);
            }
            else if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector3))
            {
                var vector4Value = value is Vector4 vector4Val ? vector4Val : default;
                mat.SetVector(Name, vector4Value);
            }
            else if (type == typeof(float))
            {
                var floatValue = value is float floatVal ? floatVal : default;
                mat.SetFloat(Name, floatValue);
            }
            else if (type == typeof(Boolean))
            {
                var boolValue = value is Boolean boolVal ? boolVal : default;
                mat.SetFloat(Name, boolValue ? 1 : 0);
            }
            // TODO: How to handle Matrix2/Matrix3 types?
            // Will probably be registry defined, how will we compare against them?
            else if (type == typeof(Matrix4x4)/*propertyType == PropertyType.Matrix2 || propertyType == PropertyType.Matrix3 || */)
            {
                var matrixValue = value is Matrix4x4 matrixVal ? matrixVal : default;
                mat.SetMatrix(Name, matrixValue);
            }
            else if (type == typeof(Gradient))
            {
                var gradientValue = value as Gradient;
                mat.SetFloat(string.Format("{0}_Type", Name), (int)gradientValue.mode);
                mat.SetFloat(string.Format("{0}_ColorsLength", Name), gradientValue.colorKeys.Length);
                mat.SetFloat(string.Format("{0}_AlphasLength", Name), gradientValue.alphaKeys.Length);
                for (int i = 0; i < 8; i++)
                    mat.SetVector(string.Format("{0}_ColorKey{1}", Name, i), i < gradientValue.colorKeys.Length ? GradientUtil.ColorKeyToVector(gradientValue.colorKeys[i]) : Vector4.zero);
                for (int i = 0; i < 8; i++)
                    mat.SetVector(string.Format("{0}_AlphaKey{1}", Name, i), i < gradientValue.alphaKeys.Length ? GradientUtil.AlphaKeyToVector(gradientValue.alphaKeys[i]) : Vector2.zero);
            }
            // TODO: Virtual textures handling
            /*else if (type == typeof(VirtualTexture))
            {
                // virtual texture assignments are not supported via the material property block, we must assign them to the materials
            }*/
        }
    }
}
