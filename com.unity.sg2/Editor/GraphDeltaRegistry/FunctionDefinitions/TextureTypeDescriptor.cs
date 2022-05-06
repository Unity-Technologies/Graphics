using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    public class TextureTypeDescriptor : ITypeDescriptor
    {
        public enum TextureCategory { Texture2D, Texture3D, CubeMap, Texture2DArray }

        public BaseTextureType.TextureType TextureType { get; }

        public TextureTypeDescriptor(BaseTextureType.TextureType textureType)
            => TextureType = textureType;
    }
}
