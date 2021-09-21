using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    struct PreviewProperty
    {
        public string name { get; set; }
        public PropertyType propType { get; private set; }

        public PreviewProperty(PropertyType type) : this()
        {
            propType = type;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct ClassData
        {
            [FieldOffset(0)]
            public Texture textureValue;
            [FieldOffset(0)]
            public Cubemap cubemapValue;
            [FieldOffset(0)]
            public Gradient gradientValue;
            [FieldOffset(0)]
            public VirtualTextureShaderProperty vtProperty;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct StructData
        {
            [FieldOffset(0)]
            public Color colorValue;
            [FieldOffset(0)]
            public Vector4 vector4Value;
            [FieldOffset(0)]
            public float floatValue;
            [FieldOffset(0)]
            public bool booleanValue;
            [FieldOffset(0)]
            public Matrix4x4 matrixValue;
        }

        ClassData m_ClassData;
        StructData m_StructData;
        Texture2DShaderProperty.DefaultType m_texture2dDefaultType;

        public Color colorValue
        {
            get
            {
                if (propType != PropertyType.Color)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Color, propType));
                return m_StructData.colorValue;
            }
            set
            {
                if (propType != PropertyType.Color)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Color, propType));
                m_StructData.colorValue = value;
            }
        }

        public Texture textureValue
        {
            get
            {
                if (propType != PropertyType.Texture2D && propType != PropertyType.Texture2DArray && propType != PropertyType.Texture3D)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Texture2D, propType));
                return m_ClassData.textureValue;
            }
            set
            {
                if (propType != PropertyType.Texture2D && propType != PropertyType.Texture2DArray && propType != PropertyType.Texture3D)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Texture2D, propType));
                m_ClassData.textureValue = value;
            }
        }

        public Texture2DShaderProperty.DefaultType texture2DDefaultType
        {
            get
            {
                if (propType != PropertyType.Texture2D)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, "Texture2DShaderProperty.DefaultType", propType));
                return m_texture2dDefaultType;
            }
            set
            {
                if (propType != PropertyType.Texture2D)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, "Texture2DShaderProperty.DefaultType", propType));
                m_texture2dDefaultType = value;
            }
        }

        public Cubemap cubemapValue
        {
            get
            {
                if (propType != PropertyType.Cubemap)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Cubemap, propType));
                return m_ClassData.cubemapValue;
            }
            set
            {
                if (propType != PropertyType.Cubemap)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Cubemap, propType));
                m_ClassData.cubemapValue = value;
            }
        }

        public Gradient gradientValue
        {
            get
            {
                if (propType != PropertyType.Gradient)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Gradient, propType));
                return m_ClassData.gradientValue;
            }
            set
            {
                if (propType != PropertyType.Gradient)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Gradient, propType));
                m_ClassData.gradientValue = value;
            }
        }

        public VirtualTextureShaderProperty vtProperty
        {
            get
            {
                if (propType != PropertyType.VirtualTexture)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Gradient, propType));
                return m_ClassData.vtProperty;
            }
            set
            {
                if (propType != PropertyType.VirtualTexture)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Gradient, propType));
                m_ClassData.vtProperty = value;
            }
        }

        public Vector4 vector4Value
        {
            get
            {
                if (propType != PropertyType.Vector2 && propType != PropertyType.Vector3 && propType != PropertyType.Vector4)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Vector4, propType));
                return m_StructData.vector4Value;
            }
            set
            {
                if (propType != PropertyType.Vector2 && propType != PropertyType.Vector3 && propType != PropertyType.Vector4
                    && propType != PropertyType.Matrix2 && propType != PropertyType.Matrix3 && propType != PropertyType.Matrix4)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Vector4, propType));
                m_StructData.vector4Value = value;
            }
        }

        public float floatValue
        {
            get
            {
                if (propType != PropertyType.Float)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Float, propType));
                return m_StructData.floatValue;
            }
            set
            {
                if (propType != PropertyType.Float)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Float, propType));
                m_StructData.floatValue = value;
            }
        }

        public bool booleanValue
        {
            get
            {
                if (propType != PropertyType.Boolean)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Boolean, propType));
                return m_StructData.booleanValue;
            }
            set
            {
                if (propType != PropertyType.Boolean)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Boolean, propType));
                m_StructData.booleanValue = value;
            }
        }

        public Matrix4x4 matrixValue
        {
            get
            {
                if (propType != PropertyType.Matrix2 && propType != PropertyType.Matrix3 && propType != PropertyType.Matrix4)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Boolean, propType));
                return m_StructData.matrixValue;
            }
            set
            {
                if (propType != PropertyType.Matrix2 && propType != PropertyType.Matrix3 && propType != PropertyType.Matrix4)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Boolean, propType));
                m_StructData.matrixValue = value;
            }
        }

        const string k_SetErrorMessage = "Cannot set a {0} property on a PreviewProperty with type {1}.";
        const string k_GetErrorMessage = "Cannot get a {0} property on a PreviewProperty with type {1}.";

        public void SetValueOnMaterialPropertyBlock(MaterialPropertyBlock mat)
        {
            if ((propType == PropertyType.Texture2D || propType == PropertyType.Texture2DArray || propType == PropertyType.Texture3D))
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
            else if (propType == PropertyType.Cubemap)
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
            else if (propType == PropertyType.Color)
                mat.SetColor(name, m_StructData.colorValue);
            else if (propType == PropertyType.Vector2 || propType == PropertyType.Vector3 || propType == PropertyType.Vector4)
                mat.SetVector(name, m_StructData.vector4Value);
            else if (propType == PropertyType.Float)
                mat.SetFloat(name, m_StructData.floatValue);
            else if (propType == PropertyType.Boolean)
                mat.SetFloat(name, m_StructData.booleanValue ? 1 : 0);
            else if (propType == PropertyType.Matrix2 || propType == PropertyType.Matrix3 || propType == PropertyType.Matrix4)
                mat.SetMatrix(name, m_StructData.matrixValue);
            else if (propType == PropertyType.Gradient)
            {
                mat.SetFloat(string.Format("{0}_Type", name), (int)m_ClassData.gradientValue.mode);
                mat.SetFloat(string.Format("{0}_ColorsLength", name), m_ClassData.gradientValue.colorKeys.Length);
                mat.SetFloat(string.Format("{0}_AlphasLength", name), m_ClassData.gradientValue.alphaKeys.Length);
                for (int i = 0; i < 8; i++)
                    mat.SetVector(string.Format("{0}_ColorKey{1}", name, i), i < m_ClassData.gradientValue.colorKeys.Length ? GradientUtil.ColorKeyToVector(m_ClassData.gradientValue.colorKeys[i]) : Vector4.zero);
                for (int i = 0; i < 8; i++)
                    mat.SetVector(string.Format("{0}_AlphaKey{1}", name, i), i < m_ClassData.gradientValue.alphaKeys.Length ? GradientUtil.AlphaKeyToVector(m_ClassData.gradientValue.alphaKeys[i]) : Vector2.zero);
            }
            else if (propType == PropertyType.VirtualTexture)
            {
                // virtual texture assignments are not supported via the material property block, we must assign them to the materials
            }
        }
    }
}
