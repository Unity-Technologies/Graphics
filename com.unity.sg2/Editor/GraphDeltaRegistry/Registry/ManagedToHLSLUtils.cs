


using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace unity.shadergraph.utils
{
    internal static class ManagedToHLSLUtils
    {
        static string ColorKeyToHLSL(GradientColorKey key) => $"float4({key.color.r},{key.color.g},{key.color.b},{key.time})";
        static string AlphaKeyToHLSL(GradientAlphaKey key) => $"float2({key.alpha},{key.time})";
        internal static string GradientToHLSL(UnityEngine.Gradient val)
        {
            var colorCount = val.colorKeys.Length;
            var alphaCount = val.alphaKeys.Length;
            var gradientMode = val.mode;


            string alpha = "";
            string color = "";
            for (int i = 0; i < 8; ++i)
            {
                var localColor = "float4(0,0,0,0)";
                var localAlpha = "float2(0,0)";

                if (i < colorCount)
                {
                    var colorKey = val.colorKeys[i];
                    localColor = ColorKeyToHLSL(colorKey);
                }
                if (i < alphaCount)
                {
                    var alphaKey = val.alphaKeys[i];
                    localAlpha = AlphaKeyToHLSL(alphaKey);
                }


                color += localColor + (i < 7 ? ", " : "");
                alpha += localAlpha + (i < 7 ? ", " : "");
            }

            return $"NewGradient({(int)gradientMode}, {colorCount}, {alphaCount}, {color}, {alpha})";
        }


        internal static string ParametricToHLSL(string name, int length, int height, float[] data)
        {
            string result = name + "(";

            for (int i = 0; i < length * height; ++i)
            {
                result += $"{data[i]}";
                if (i != length * height - 1)
                    result += ", ";
            }
            result += ")";
            return result;
        }

        internal static void ManagedToParametric(object ovalue, out string name, out int length, out int height, out float[] data)
        {
            name = "float";
            height = 1;
            switch (ovalue)
            {
                default: throw new System.Exception("Type not supported.");

                case Vector4 v4: length = 4; data = new float[] { v4.x, v4.y, v4.z, v4.w }; break;
                case Vector3 v3: length = 3; data = new float[] { v3.x, v3.y, v3.z }; break;
                case Vector2 v2: length = 2; data = new float[] { v2.x, v2.y }; break;
                case float    f: length = 1; data = new float[] { f }; break;
                case int      i: length = 1; name = "int"; data = new float[] { i }; break;
                case bool     b: length = 1; name = "bool"; data = new float[] { (b ? 1f : 0f) }; break;
                case Matrix4x4 m:
                    height = 4; length = 4;
                    var c0 = m.GetColumn(0);
                    var c1 = m.GetColumn(1);
                    var c2 = m.GetColumn(2);
                    var c3 = m.GetColumn(3);
                    data = new float[] {
                        c0.x, c0.y, c0.z, c0.w,
                        c1.x, c1.y, c1.z, c1.w,
                        c2.x, c2.y, c2.z, c2.w,
                        c3.x, c3.y, c3.z, c3.w
                    };
                    break;
                case float[] a: // Should only be hit if this is a value for a vector/matrix, not if it's actually an array.
                    switch(a.Length)
                    {
                        case 9: length = height = 3; break;
                        case 16: length = height = 4; break;
                        default: length = a.Length; break;
                    }
                    data = (float[])a.Clone();
                    break;
            }
            if (length > 1 || height != 1)
                name += $"{length}";
            if (height > 1)
                name += $"x{height}";
        }

        internal static string ManagedParametricToHLSL(object ovalue)
        {
            ManagedToParametric(ovalue, out var name, out var length, out var height, out var data);
            return ParametricToHLSL(name, length, height, data);
        }

        internal static bool CanBeLocal(object o)
        {
            switch (o)
            {
                case Texture:
                case UnityEditor.ShaderGraph.Defs.ReferenceValueDescriptor:
                    return false;
                case Gradient: return true;
            }
            try
            {
                ManagedToParametric(o, out _, out _, out _, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // TODO: It's probably better to simulate an HLSL type in C#, have that HLSL type definition own its ToHLSL method ->
        // Should revisit this to the benefit of the scripting APIs, ShaderFoundry, GTF Constants, Descriptors, and TypeDefinitions;
        // there is some significant pain that can be eased there.
        internal static string ToHLSL(object o)
        {
            try
            {
                switch (o)
                {
                    case Gradient og: return GradientToHLSL(og);
                    default: return ManagedParametricToHLSL(o);
                    // case Texture: Can't convert directly to HLSL because it must be promoted to a property first- there is no inline conversion.
                    // case ReferenceValueDescriptor: Can't convert directly to HLSL because it must be pulled from a uniform- there is no inline conversion.
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
