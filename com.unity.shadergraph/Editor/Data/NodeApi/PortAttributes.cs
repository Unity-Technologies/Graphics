using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public abstract class PortAttribute : Attribute
    {
        // This makes it so that the base class cannot be inherited outside the assembly.
        internal abstract PortValue value { get; }
    }

    public sealed class Vector1PortAttribute : PortAttribute
    {
        readonly float m_X;

        public Vector1PortAttribute(float x = default)
        {
            m_X = x;
        }

        internal override PortValue value => PortValue.Vector1(m_X);
    }

    public sealed class Vector3PortAttribute : PortAttribute
    {
        readonly float m_X;
        readonly float m_Y;
        readonly float m_Z;

        public Vector3PortAttribute(float x = default, float y = default, float z = default)
        {
            m_X = x;
            m_Y = y;
            m_Z = z;
        }

        internal override PortValue value => PortValue.Vector3(new Vector3(m_X, m_Y, m_Z));
    }
}
