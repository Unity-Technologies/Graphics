using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Preview Node")]
    public class PreviewNode : CodeFunctionNode
    {
        public override bool hasPreview { get { return true; } }

        [SerializeField]
        float m_Width;

        [SerializeField]
        float m_Height;

        public void SetDimensions(float width, float height)
        {
            m_Width = width;
            m_Height = height;
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
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Preview", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Preview(
            [Slot(0, Binding.None)] DynamicDimensionVector input,
            [Slot(1, Binding.None)] out DynamicDimensionVector output)
        {
            return
                @"
{
    output = input;
}
";
        }
    }
}
