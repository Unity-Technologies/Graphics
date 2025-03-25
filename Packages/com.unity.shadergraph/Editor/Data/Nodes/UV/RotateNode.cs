using System.Reflection;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    enum RotationUnit
    {
        Radians,
        Degrees
    };

    [Title("UV", "Rotate")]
    class RotateNode : CodeFunctionNode
    {
        [SerializeField]
        private RotationUnit m_Unit = RotationUnit.Radians;

        [EnumControl("Unit")]
        public RotationUnit unit
        {
            get { return m_Unit; }
            set
            {
                if (m_Unit == value)
                    return;

                m_Unit = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public RotateNode()
        {
            name = "Rotate";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            if (m_Unit == RotationUnit.Radians)
                return GetType().GetMethod("Unity_Rotate_Radians", BindingFlags.Static | BindingFlags.NonPublic);
            else
                return GetType().GetMethod("Unity_Rotate_Degrees", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Rotate_Radians(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector2 Center,
            [Slot(2, Binding.None)] Vector1 Rotation,
            [Slot(3, Binding.None)] out Vector2 Out)
        {
            Out = Vector2.zero;


            return
@"
{
    UV -= Center;
    $precision s, c;
    sincos(Rotation, s, c);
    $precision3 r3 = $precision3(-s, c, s);
    $precision2 r1;
    r1.y = dot(UV, r3.xy);
    r1.x = dot(UV, r3.yz);
    Out = r1 + Center;
}";
        }

        static string Unity_Rotate_Degrees(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector2 Center,
            [Slot(2, Binding.None)] Vector1 Rotation,
            [Slot(3, Binding.None)] out Vector2 Out)
        {
            Out = Vector2.zero;

            return @"
{
    Rotation = Rotation * (3.1415926f/180.0f);
    UV -= Center;
    $precision s, c;
    sincos(Rotation, s, c);
    $precision3 r3 = $precision3(-s, c, s);
    $precision2 r1;
    r1.y = dot(UV, r3.xy);
    r1.x = dot(UV, r3.yz);
    Out = r1 + Center;
}";
        }
    }
}
