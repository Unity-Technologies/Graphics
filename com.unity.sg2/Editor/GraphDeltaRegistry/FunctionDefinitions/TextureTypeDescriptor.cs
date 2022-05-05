namespace UnityEditor.ShaderGraph.Defs
{
    public class TextureTypeDescriptor : ITypeDescriptor
    {
        public enum TextureCategory { Texture2D, Texture3D, CubeMap, Texture2DArray }

        public TextureCategory Category { get; }

        public TextureTypeDescriptor(TextureCategory category)
            => Category = category;
    }
}
