using UnityEngine;

namespace UnityEditor.ShaderGraph.Defs
{
    internal static class ParametricTypeUtils
    {
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

        private static void ManagedToParametric(object ovalue, out string name, out int length, out int height, out float[] data)
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

        internal static string ManagedToParametricToHLSL(object ovalue)
        {
            if (ovalue == null)
                return null;

            ManagedToParametric(ovalue, out var name, out var length, out var height, out var data);
            return ParametricToHLSL(name, length, height, data);
        }

        internal static bool IsParametric(ParameterDescriptor desc)
        {
            return desc.TypeDescriptor is ParametricTypeDescriptor && CouldBeParametric(desc.DefaultValue);
        }

        private static bool CouldBeParametric(object o)
        {
            if (o == null)
            {
                return true;
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
    }
}
