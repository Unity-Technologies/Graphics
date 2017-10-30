using System.Reflection;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public enum Unit
    {
        Radians,
        Degrees
    };
    [Title("UV/Rotate")]
    public class UVRotatorNode : CodeFunctionNode
    {

        [SerializeField]
        private Unit m_constant = Unit.Radians;

        [EnumControl("")]
        public Unit constant
        {
            get { return m_constant; }
            set
            {
                if (m_constant == value)
                    return;

                m_constant = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        public UVRotatorNode()
        {
            name = "Rotate";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            if(m_constant == Unit.Radians)
                return GetType().GetMethod("Unity_UVRotator_Radians", BindingFlags.Static | BindingFlags.NonPublic);
            else
                return GetType().GetMethod("Unity_UVRotator_Degrees", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_UVRotator_Radians(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector2 Center,
            [Slot(2, Binding.None)] Vector1 Rotation,
            [Slot(3, Binding.None)] out Vector2 Out)
        {
            Out = Vector2.zero;

            
            return
                @"
{
    //rotation matrix
    UV -= Center;
    {precision} s = sin(Rotation);
    {precision} c = cos(Rotation);


    //center rotation matrix
    {precision}2x2 rMatrix = float2x2(c, -s, s, c);
    rMatrix *= 0.5;
    rMatrix += 0.5;
    rMatrix = rMatrix*2 - 1;

    //multiply the UVs by the rotation matrix
    UV.xy = mul(UV.xy, rMatrix);
    UV += Center;
    
    Out = UV;
}";
        }

        static string Unity_UVRotator_Degrees(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector2 Center,
            [Slot(2, Binding.None)] Vector1 Rotation,
            [Slot(3, Binding.None)] out Vector2 Out)
        {
            Out = Vector2.zero;
           
            return @"
{
    //rotation matrix

    Rotation = Rotation * (3.1415926f/180.0f);
    UV -= Center;
    {precision} s = sin(Rotation);
    {precision} c = cos(Rotation);


    //center rotation matrix
    {precision}2x2 rMatrix = float2x2(c, -s, s, c);
    rMatrix *= 0.5;
    rMatrix += 0.5;
    rMatrix = rMatrix*2 - 1;

    //multiply the UVs by the rotation matrix
    UV.xy = mul(UV.xy, rMatrix);
    UV += Center;
    
    Out = UV;
}";

        }
    }
}
