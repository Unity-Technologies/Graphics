using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public struct PreviewProperty
    {
        public string name { get; set; }
        public PropertyType propType { get; private set; }

        public PreviewProperty(PropertyType type) : this()
        {
            propType = type;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct Data
        {
            [FieldOffset(0)]
            public Color colorValue;
            [FieldOffset(0)]
            public Texture textureValue;
            [FieldOffset(0)]
            public Cubemap cubemapValue;
            [FieldOffset(0)]
            public Gradient gradientValue;
            [FieldOffset(0)]
            public Vector4 vector4Value;
            [FieldOffset(0)]
            public float floatValue;
            [FieldOffset(0)]
            public bool booleanValue;
        }

        Data m_Data;

        public Color colorValue
        {
            get
            {
                if (propType != PropertyType.Color)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Color, propType));
                return m_Data.colorValue;
            }
            set
            {
                if (propType != PropertyType.Color)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Color, propType));
                m_Data.colorValue = value;
            }
        }

        public Texture textureValue
        {
            get
            {
                if (propType != PropertyType.Texture)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Texture, propType));
                return m_Data.textureValue;
            }
            set
            {
                if (propType != PropertyType.Texture)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Texture, propType));
                m_Data.textureValue = value;
            }
        }

        public Cubemap cubemapValue
        {
            get
            {
                if (propType != PropertyType.Cubemap)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Cubemap, propType));
                return m_Data.cubemapValue;
            }
            set
            {
                if (propType != PropertyType.Cubemap)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Cubemap, propType));
                m_Data.cubemapValue = value;
            }
        }

        public Gradient gradientValue
        {
            get
            {
                if (propType != PropertyType.Gradient)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Gradient, propType));
                return m_Data.gradientValue;
            }
            set
            {
                if (propType != PropertyType.Gradient)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Gradient, propType));
                m_Data.gradientValue = value;
            }
        }

        public Vector4 vector4Value
        {
            get
            {
                if (propType != PropertyType.Vector2 && propType != PropertyType.Vector3 && propType != PropertyType.Vector4)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Vector4, propType));
                return m_Data.vector4Value;
            }
            set
            {
                if (propType != PropertyType.Vector2 && propType != PropertyType.Vector3 && propType != PropertyType.Vector4 
                    && propType != PropertyType.Matrix2 && propType != PropertyType.Matrix3 && propType != PropertyType.Matrix4)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Vector4, propType));
                m_Data.vector4Value = value;
            }
        }

        public float floatValue
        {
            get
            {
                if (propType != PropertyType.Vector1)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Vector1, propType));
                return m_Data.floatValue;
            }
            set
            {
                if (propType != PropertyType.Vector1)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Vector1, propType));
                m_Data.floatValue = value;
            }
        }

        public bool booleanValue
        {
            get
            {
                if (propType != PropertyType.Boolean)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Boolean, propType));
                return m_Data.booleanValue;
            }
            set
            {
                if (propType != PropertyType.Boolean)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Boolean, propType));
                m_Data.booleanValue = value;
            }
        }

        const string k_SetErrorMessage = "Cannot set a {0} property on a PreviewProperty with type {1}.";
        const string k_GetErrorMessage = "Cannot get a {0} property on a PreviewProperty with type {1}.";

        public void SetMaterialPropertyBlockValue(MaterialPropertyBlock block)
        {
            if (propType == PropertyType.Texture && textureValue != null)
                block.SetTexture(name, m_Data.textureValue);
            else if (propType == PropertyType.Cubemap && cubemapValue != null)
                block.SetTexture(name, m_Data.cubemapValue);
            else if (propType == PropertyType.Color)
                block.SetColor(name, m_Data.colorValue);
            else if (propType == PropertyType.Vector2 || propType == PropertyType.Vector3 || propType == PropertyType.Vector4)
                block.SetVector(name, m_Data.vector4Value);
            else if (propType == PropertyType.Vector1)
                block.SetFloat(name, m_Data.floatValue);
            else if (propType == PropertyType.Boolean)
                block.SetFloat(name, m_Data.booleanValue ? 1 : 0);
        }
    }

    public static class PreviewPropertyExtensions
    {
        public static void SetPreviewProperty(this MaterialPropertyBlock block, PreviewProperty previewProperty)
        {
            previewProperty.SetMaterialPropertyBlockValue(block);
        }
    }
}
