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
            [Slot(0, Binding.MeshUV0)] Vector2 uv,
            [Slot(1, Binding.None)] Vector1 rotation,
            [Slot(2, Binding.None)] out Vector2 result)
        {
            result = Vector2.zero;

            
            return
                @"
{
    //rotation matrix
    uv.xy -= 0.5;
    {precision} s = sin(rotation);
    {precision} c = cos(rotation);


    //center rotation matrix
    {precision}2x2 rMatrix = float2x2(c, -s, s, c);
    rMatrix *= 0.5;
    rMatrix += 0.5;
    rMatrix = rMatrix*2 - 1;

    //multiply the UVs by the rotation matrix
    uv.xy = mul(uv.xy, rMatrix);
    uv.xy += 0.5;
    
    result = uv;
}";
        }

        static string Unity_UVRotator_Degrees(
            [Slot(0, Binding.MeshUV0)] Vector2 uv,
            [Slot(1, Binding.None)] Vector1 rotation,
            [Slot(2, Binding.None)] out Vector2 result)
        {
            result = Vector2.zero;
            //Unity_UVRotator_Radians(uv, rotation, out result)
            //rotation = rotation * (3.1415926f/180.0f)
            return @"
{
    //rotation matrix
    rotation = rotation * (3.1415926f/180.0f);
    uv.xy -= 0.5;
    {precision} s = sin(rotation);
    {precision} c = cos(rotation);


    //center rotation matrix
    {precision}2x2 rMatrix = float2x2(c, -s, s, c);
    rMatrix *= 0.5;
    rMatrix += 0.5;
    rMatrix = rMatrix*2 - 1;

    //multiply the UVs by the rotation matrix
    uv.xy = mul(uv.xy, rMatrix);
    uv.xy += 0.5;
    
    result = uv;
}";

        }
    }
}
