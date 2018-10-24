using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class PortCollection : ICollection<MaterialSlot>
    {
        List<BitangentMaterialSlot> m_Bitangent = new List<BitangentMaterialSlot>();
        List<BitangentMaterialSlot> m_Boolean = new List<BitangentMaterialSlot>();
        List<ColorRGBAMaterialSlot> m_ColorRGBA = new List<ColorRGBAMaterialSlot>();
        List<ColorRGBMaterialSlot> m_ColorRGB = new List<ColorRGBMaterialSlot>();
        List<CubemapInputMaterialSlot> m_CubemapInput = new List<CubemapInputMaterialSlot>();
        List<CubemapMaterialSlot> m_Cubemap = new List<CubemapMaterialSlot>();
        List<DynamicMatrixMaterialSlot> m_DynamicMatrix = new List<DynamicMatrixMaterialSlot>();
        List<DynamicValueMaterialSlot> m_DynamicValue = new List<DynamicValueMaterialSlot>();
        List<DynamicVectorMaterialSlot> m_DynamicVector = new List<DynamicVectorMaterialSlot>();
        List<GradientInputMaterialSlot> m_GradientInput = new List<GradientInputMaterialSlot>();
        List<GradientMaterialSlot> m_Gradient = new List<GradientMaterialSlot>();
        List<Matrix2MaterialSlot> m_Matrix2 = new List<Matrix2MaterialSlot>();
        List<Matrix3MaterialSlot> m_Matrix3 = new List<Matrix3MaterialSlot>();
        List<Matrix4MaterialSlot> m_Matrix4 = new List<Matrix4MaterialSlot>();
        List<NormalMaterialSlot> m_Normal = new List<NormalMaterialSlot>();
        List<PositionMaterialSlot> m_Position = new List<PositionMaterialSlot>();
        List<SamplerStateMaterialSlot> m_SamplerState = new List<SamplerStateMaterialSlot>();
        List<ScreenPositionMaterialSlot> m_ScreenPosition = new List<ScreenPositionMaterialSlot>();
        List<SpaceMaterialSlot> m_Space = new List<SpaceMaterialSlot>();
        List<TangentMaterialSlot> m_Tangent = new List<TangentMaterialSlot>();
        List<Texture2DArrayInputMaterialSlot> m_Texture2DArrayInput = new List<Texture2DArrayInputMaterialSlot>();
        List<Texture2DArrayMaterialSlot> m_Texture2DArray = new List<Texture2DArrayMaterialSlot>();
        List<Texture2DInputMaterialSlot> m_Texture2DInput = new List<Texture2DInputMaterialSlot>();
        List<Texture2DMaterialSlot> m_Texture2D = new List<Texture2DMaterialSlot>();
        List<Texture3DInputMaterialSlot> m_Texture3DInput = new List<Texture3DInputMaterialSlot>();
        List<Texture3DMaterialSlot> m_Texture3D = new List<Texture3DMaterialSlot>();
        List<UVMaterialSlot> m_UV = new List<UVMaterialSlot>();
        List<Vector1MaterialSlot> m_Vector1 = new List<Vector1MaterialSlot>();
        List<Vector2MaterialSlot> m_Vector2 = new List<Vector2MaterialSlot>();
        List<Vector3MaterialSlot> m_Vector3 = new List<Vector3MaterialSlot>();
        List<Vector4MaterialSlot> m_Vector4 = new List<Vector4MaterialSlot>();
        List<VertexColorMaterialSlot> m_VertexColor = new List<VertexColorMaterialSlot>();
        List<ViewDirectionMaterialSlot> m_ViewDirection = new List<ViewDirectionMaterialSlot>();

        IList[] m_Lists;

        public PortCollection()
        {
//            m_Lists = new[] { m_Bitangent, m_Boolean, m_ColorRGBA, m_ColorRGB, m_CubemapInput, m_Cubemap, m_DynamicMatrix, m_DynamicValue };
        }


        public IEnumerator<MaterialSlot> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(MaterialSlot item)
        {
            throw new System.NotImplementedException();
        }

        public void Clear()
        {
            throw new System.NotImplementedException();
        }

        public bool Contains(MaterialSlot item)
        {
            throw new System.NotImplementedException();
        }

        public void CopyTo(MaterialSlot[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(MaterialSlot item)
        {
            throw new System.NotImplementedException();
        }

        public int Count { get; }
        public bool IsReadOnly { get; }
    }
}
