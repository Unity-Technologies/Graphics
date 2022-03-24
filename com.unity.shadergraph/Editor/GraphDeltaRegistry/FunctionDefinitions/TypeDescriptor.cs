using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    /// <summary>
    /// </summary>
    internal readonly struct TypeDescriptor
    {
        public GraphType.Primitive Primitive { get; }
        public GraphType.Precision Precision { get; }
        public GraphType.Length Length { get; }
        public GraphType.Height Height { get; }

        public TypeDescriptor(
            GraphType.Precision precision,
            GraphType.Primitive primitive,
            GraphType.Length length,
            GraphType.Height height)
        {
            Primitive = primitive;
            Precision = precision;
            Length = length;
            Height = height;
        }
    }
}
