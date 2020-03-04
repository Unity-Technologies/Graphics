using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public enum ContextStage
    {
        Vertex,
        Fragment,
    }

    public interface IControl
    {
    }

    public class ObjectSpacePositionControl : IControl
    {
    }

    public class ObjectSpaceNormalControl : IControl
    {
    }

    public class ObjectSpaceTangentControl : IControl
    {
    }

    public class TangentSpaceNormalControl : IControl
    {
    }

    public class ColorControl : IControl
    {
        public Color value { get; private set; }
        public bool hdr { get; private set; }

        public ColorControl(Color value, bool hdr)
        {
            this.value = value;
            this.hdr = hdr;
        }
    }

    public class ColorRGBAControl : IControl
    {
        public Color value { get; private set; }

        public ColorRGBAControl(Color value)
        {
            this.value = value;
        }
    }

    public class FloatControl : IControl
    {
        public float value { get; private set; }

        public FloatControl(float value)
        {
            this.value = value;
        }
    }

    internal class BlockFieldDescriptor : FieldDescriptor
    {
        public IControl control { get; }
        public ContextStage contextStage { get; }
        public ShaderGraphRequirements requirements { get; }

        public BlockFieldDescriptor(string tag, string name, string define,
            IControl control, ContextStage contextStage, ShaderGraphRequirements requirements = default(ShaderGraphRequirements))
            : base (tag, name, define)
        {
            this.control = control;
            this.contextStage = contextStage;
            this.requirements = requirements;
        }
    }
}
