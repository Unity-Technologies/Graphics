using System;

namespace UnityEditor.ShaderGraph.Hlsl
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class AnyDimensionAttribute : Attribute
    {
        public string Group;
        public AnyDimensionAttribute(string group = "")
        {
            Group = group;
        }
    }

    //public struct UntypedVector
    //{
    //    public string Code;

    //    public static implicit operator UntypedVector(Float v)
    //        => new UntypedVector() { Code = $"float4({v.Code}, 0, 0, 0)" };

    //    public static implicit operator UntypedVector(Float2 v)
    //        => new UntypedVector() { Code = $"float4({v.Code}, 0, 0)" };

    //    public static implicit operator UntypedVector(Float3 v)
    //        => new UntypedVector() { Code = $"float4({v.Code}, 0)" };

    //    public static implicit operator UntypedVector(Float4 v)
    //        => new UntypedVector() { Code = $"float4({v.Code})" };

    //    public void AssignFrom(UntypedVector other)
    //    {
    //        Code = other.Code;
    //    }

    //    public static string Extract(int components)
    //    {
    //        if (components == 1)
    //            return ".x";
    //        else if (components == 2)
    //            return ".xy";
    //        else if (components == 3)
    //            return ".xyz";
    //        else
    //            return ".xyzw";
    //    }
    //}
}
