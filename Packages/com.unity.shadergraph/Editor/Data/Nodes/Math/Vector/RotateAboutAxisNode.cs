using System.Reflection;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Vector", "Rotate About Axis")]
    class RotateAboutAxisNode : CodeFunctionNode
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

        public RotateAboutAxisNode()
        {
            name = "Rotate About Axis";
            synonyms = new string[] { "pivot" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            if (m_Unit == RotationUnit.Radians)
                return GetType().GetMethod("Unity_Rotate_About_Axis_Radians", BindingFlags.Static | BindingFlags.NonPublic);
            else
                return GetType().GetMethod("Unity_Rotate_About_Axis_Degrees", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Rotate_About_Axis_Degrees(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] Vector3 Axis,
            [Slot(2, Binding.None)] Vector1 Rotation,
            [Slot(3, Binding.None)] out Vector3 Out)
        {
            Out = In;
            return
@"
{
    Rotation = radians(Rotation);
    $precision s, c;
    sincos(Rotation, s, c);
    Axis = normalize(Axis);
    Out = In * c + cross(Axis, In) * s + Axis * dot(Axis, In) * (1 - c);
}
";
        }

        static string Unity_Rotate_About_Axis_Radians(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] Vector3 Axis,
            [Slot(2, Binding.None)] Vector1 Rotation,
            [Slot(3, Binding.None)] out Vector3 Out)
        {
            Out = In;
            return
@"
{
    $precision s, c;
    sincos(Rotation, s, c);
    Axis = normalize(Axis);
    Out = In * c + cross(Axis, In) * s + Axis * dot(Axis, In) * (1 - c);
}
";
        }
    }
}
