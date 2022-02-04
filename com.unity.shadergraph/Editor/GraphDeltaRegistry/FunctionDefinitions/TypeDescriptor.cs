using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{
    /// <summary>
    /// </summary>
    internal readonly struct TypeDescriptor
    {
        public Primitive Primitive { get; }
        public Precision Precision { get; }
        public Length Length { get; }
        public Height Height { get; }

        public TypeDescriptor(
            Primitive primitive,
            Precision precision,
            Length length,
            Height height)
        {
            Primitive = primitive;
            Precision = precision;
            Length = length;
            Height = height;
        }

        public override string ToString()
        {
            // TODO Make this not a stub.
            return base.ToString();
        }
    }
}
