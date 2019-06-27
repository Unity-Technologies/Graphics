using UnityEngine;
using UnityEditor.ShaderGraph.Hlsl;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Preview")]
    class PreviewNode : CodeFunctionNode
    {
        public override bool hasPreview { get { return true; } }

        [SerializeField]
        float m_Width;

        [SerializeField]
        float m_Height;

        public void SetDimensions(float width, float height)
        {
            float newSize = Mathf.Clamp(Mathf.Min(width, height), 150f, 1000f);

            m_Width = newSize;
            m_Height = newSize;
        }

        public float width
        {
            get { return m_Width; }
        }

        public float height
        {
            get { return m_Height; }
        }

        public PreviewNode()
        {
            name = "Preview";

            m_Width = 208f;
            m_Height = 208f;
        }

        [HlslCodeGen]
        static void Unity_Preview(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = In;
        }
    }
}
