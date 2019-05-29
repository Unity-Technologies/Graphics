using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    struct PreviewProperty
    {
        public string name { get; set; }
        public ConcreteSlotValueType concreteShaderValueType { get; private set; }

        public PreviewProperty(ConcreteSlotValueType type) : this()
        {
            concreteShaderValueType = type;
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

        public Texture textureValue
        {
            get
            {
                if (concreteShaderValueType != ConcreteSlotValueType.Texture2D && concreteShaderValueType != ConcreteSlotValueType.Texture2DArray && concreteShaderValueType != ConcreteSlotValueType.Texture3D)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, ConcreteSlotValueType.Texture2D, concreteShaderValueType));
                return m_ClassData.textureValue;
            }
            set
            {
                if (concreteShaderValueType != ConcreteSlotValueType.Texture2D && concreteShaderValueType != ConcreteSlotValueType.Texture2DArray && concreteShaderValueType != ConcreteSlotValueType.Texture3D)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, ConcreteSlotValueType.Texture2D, concreteShaderValueType));
                m_ClassData.textureValue = value;
            }
        }

        public Cubemap cubemapValue
        {
            get
            {
                if (concreteShaderValueType != ConcreteSlotValueType.Cubemap)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, ConcreteSlotValueType.Cubemap, concreteShaderValueType));
                return m_ClassData.cubemapValue;
            }
            set
            {
                if (concreteShaderValueType != ConcreteSlotValueType.Cubemap)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, ConcreteSlotValueType.Cubemap, concreteShaderValueType));
                m_ClassData.cubemapValue = value;
            }
        }

        public Gradient gradientValue
        {
            get
            {
                if (concreteShaderValueType != ConcreteSlotValueType.Gradient)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, ConcreteSlotValueType.Gradient, concreteShaderValueType));
                return m_ClassData.gradientValue;
            }
            set
            {
                if (concreteShaderValueType != ConcreteSlotValueType.Gradient)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, ConcreteSlotValueType.Gradient, concreteShaderValueType));
                m_ClassData.gradientValue = value;
            }
        }

        public Vector4 vector4Value
        {
            get
            {
                if (concreteShaderValueType != ConcreteSlotValueType.Vector2 && concreteShaderValueType != ConcreteSlotValueType.Vector3 && concreteShaderValueType != ConcreteSlotValueType.Vector4)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, ConcreteSlotValueType.Vector4, concreteShaderValueType));
                return m_StructData.vector4Value;
            }
            set
            {
                if (concreteShaderValueType != ConcreteSlotValueType.Vector2 && concreteShaderValueType != ConcreteSlotValueType.Vector3 && concreteShaderValueType != ConcreteSlotValueType.Vector4
                    && concreteShaderValueType != ConcreteSlotValueType.Matrix2 && concreteShaderValueType != ConcreteSlotValueType.Matrix3 && concreteShaderValueType != ConcreteSlotValueType.Matrix4)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, ConcreteSlotValueType.Vector4, concreteShaderValueType));
                m_StructData.vector4Value = value;
            }
        }

        public float floatValue
        {
            get
            {
                if (concreteShaderValueType != ConcreteSlotValueType.Vector1)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, ConcreteSlotValueType.Vector1, concreteShaderValueType));
                return m_StructData.floatValue;
            }
            set
            {
                if (concreteShaderValueType != ConcreteSlotValueType.Vector1)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, ConcreteSlotValueType.Vector1, concreteShaderValueType));
                m_StructData.floatValue = value;
            }
        }

        public bool booleanValue
        {
            get
            {
                if (concreteShaderValueType != ConcreteSlotValueType.Boolean)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, ConcreteSlotValueType.Boolean, concreteShaderValueType));
                return m_StructData.booleanValue;
            }
            set
            {
                if (concreteShaderValueType != ConcreteSlotValueType.Boolean)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, ConcreteSlotValueType.Boolean, concreteShaderValueType));
                m_StructData.booleanValue = value;
            }
        }

        public Matrix4x4 matrixValue
        {
            get
            {
                if (concreteShaderValueType != ConcreteSlotValueType.Matrix2 && concreteShaderValueType != ConcreteSlotValueType.Matrix3 && concreteShaderValueType != ConcreteSlotValueType.Matrix4)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, ConcreteSlotValueType.Boolean, concreteShaderValueType));
                return m_StructData.matrixValue;
            }
            set
            {
                if (concreteShaderValueType != ConcreteSlotValueType.Matrix2 && concreteShaderValueType != ConcreteSlotValueType.Matrix3 && concreteShaderValueType != ConcreteSlotValueType.Matrix4)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, ConcreteSlotValueType.Boolean, concreteShaderValueType));
                m_StructData.matrixValue = value;
            }
        }

        const string k_SetErrorMessage = "Cannot set a {0} property on a PreviewProperty with type {1}.";
        const string k_GetErrorMessage = "Cannot get a {0} property on a PreviewProperty with type {1}.";

        public void SetMaterialPropertyBlockValue(Material mat)
        {
            if ((concreteShaderValueType == ConcreteSlotValueType.Texture2D || concreteShaderValueType == ConcreteSlotValueType.Texture2DArray || concreteShaderValueType == ConcreteSlotValueType.Texture3D) && textureValue != null)
                mat.SetTexture(name, m_ClassData.textureValue);
            else if (concreteShaderValueType == ConcreteSlotValueType.Cubemap && cubemapValue != null)
                mat.SetTexture(name, m_ClassData.cubemapValue);
            else if (concreteShaderValueType == ConcreteSlotValueType.Vector2 || concreteShaderValueType == ConcreteSlotValueType.Vector3 || concreteShaderValueType == ConcreteSlotValueType.Vector4)
                mat.SetVector(name, m_StructData.vector4Value);
            else if (concreteShaderValueType == ConcreteSlotValueType.Vector1)
                mat.SetFloat(name, m_StructData.floatValue);
            else if (concreteShaderValueType == ConcreteSlotValueType.Boolean)
                mat.SetFloat(name, m_StructData.booleanValue ? 1 : 0);
            else if (concreteShaderValueType == ConcreteSlotValueType.Matrix2 || concreteShaderValueType == ConcreteSlotValueType.Matrix3 || concreteShaderValueType == ConcreteSlotValueType.Matrix4)
                mat.SetMatrix(name, m_StructData.matrixValue);
            else if (concreteShaderValueType == ConcreteSlotValueType.Gradient)
            {
                mat.SetFloat(string.Format("{0}_Type", name), (int)m_ClassData.gradientValue.mode);
                mat.SetFloat(string.Format("{0}_ColorsLength", name), m_ClassData.gradientValue.colorKeys.Length);
                mat.SetFloat(string.Format("{0}_AlphasLength", name), m_ClassData.gradientValue.alphaKeys.Length);
                for (int i = 0; i < 8; i++)
                    mat.SetVector(string.Format("{0}_ColorKey{1}", name, i), i < m_ClassData.gradientValue.colorKeys.Length ? GradientUtils.ColorKeyToVector(m_ClassData.gradientValue.colorKeys[i]) : Vector4.zero);
                for (int i = 0; i < 8; i++)
                    mat.SetVector(string.Format("{0}_AlphaKey{1}", name, i), i < m_ClassData.gradientValue.alphaKeys.Length ? GradientUtils.AlphaKeyToVector(m_ClassData.gradientValue.alphaKeys[i]) : Vector2.zero);
            }
        }
    }

    static class PreviewPropertyExtensions
    {
        public static void SetPreviewProperty(this Material mat, PreviewProperty previewProperty)
        {
            previewProperty.SetMaterialPropertyBlockValue(mat);
        }
    }
}
