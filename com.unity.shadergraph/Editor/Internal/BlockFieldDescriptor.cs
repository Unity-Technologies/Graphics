using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
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

    public class FloatControl : IControl
    {
        public float value { get; private set; }

        public FloatControl(float value)
        {
            this.value = value;
        }
    }

    public class BlockFieldDescriptor
    {
        public string tag { get; }
        public string name { get; }
        public IControl control { get; }
        public ContextStage contextStage { get; }

        public BlockFieldDescriptor(string tag, string name, IControl control, ContextStage contextStage)
        {
            this.tag = tag;
            this.name = name;
            this.control = control;
            this.contextStage = contextStage;
        }
    }
}
