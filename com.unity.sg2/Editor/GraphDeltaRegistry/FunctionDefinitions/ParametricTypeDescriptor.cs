using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ParametricTypeDescriptor: ITypeDescriptor
    {
        public GraphType.Primitive Primitive { get; }
        public GraphType.Precision Precision { get; }
        public GraphType.Length Length { get; }
        public GraphType.Height Height { get; }

        public ParametricTypeDescriptor(
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
