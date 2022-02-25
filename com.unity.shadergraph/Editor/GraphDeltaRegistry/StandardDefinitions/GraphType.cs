

using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Registry.Types
{
    /// <summary>
    /// Experimenting with different helper function interfaces.
    /// </summary>

    internal static class GraphTypeHelpers
    {
        public static GraphType.Precision GetPrecision(IFieldReader field)
        {
            field.GetField(GraphType.kPrecision, out GraphType.Precision precision);
            return precision;
        }
        public static GraphType.Primitive GetPrimitive(IFieldReader field)
        {
            field.GetField(GraphType.kPrimitive, out GraphType.Primitive primitive);
            return primitive;
        }
        public static GraphType.Length GetLength(IFieldReader field)
        {
            field.GetField(GraphType.kLength, out GraphType.Length length);
            return length;
        }
        public static GraphType.Height GetHeight(IFieldReader field)
        {
            field.GetField(GraphType.kHeight, out GraphType.Height height);
            return height;
        }

        public static float GetComponent(IFieldReader field, int idx)
        {
            field.GetField(GraphType.kC(idx), out float val);
            return val;
        }
        public static IEnumerable<float> GetComponents(IFieldReader field, int idx = 0)
        {
            for (; idx < (int)GetLength(field) * (int)GetHeight(field); ++idx)
                yield return GetComponent(field, idx);
        }

        public static Type GetManagedType(IFieldReader field)
        {
            var primitive = GetPrimitive(field);
            var length = GetLength(field);
            var height = GetHeight(field);
            if (height == GraphType.Height.Four && length == GraphType.Length.Four)
                return typeof(Matrix4x4);

            switch (length)
            {
                case GraphType.Length.One:
                    switch (primitive)
                    {
                        case GraphType.Primitive.Bool: return typeof(bool);
                        case GraphType.Primitive.Int: return typeof(int);
                        default: return typeof(float);
                    }
                case GraphType.Length.Two: return typeof(Vector2);
                case GraphType.Length.Three: return typeof(Vector3);
                default: return typeof(Vector4);
            }
        }

        public static bool GetAsBool(IFieldReader field) => GetComponent(field, 0) != 0;
        public static float GetAsFloat(IFieldReader field) => GetComponent(field, 0);
        public static int GetAsInt(IFieldReader field) => (int)GetComponent(field, 0);
        public static Vector2 GetAsVec2(IFieldReader field) => new Vector2(GetComponent(field, 0), GetComponent(field, 1));
        public static Vector3 GetAsVec3(IFieldReader field) => new Vector3(GetComponent(field, 0), GetComponent(field, 1), GetComponent(field, 2));
        public static Vector4 GetAsVec4(IFieldReader field, int col = 0) => new Vector4(
                GetComponent(field, 0 + col * 4),
                GetComponent(field, 1 + col * 4),
                GetComponent(field, 2 + col * 4),
                GetComponent(field, 3 + col * 4)
            );
        public static Matrix4x4 GetAsMat4(IFieldReader field) => new Matrix4x4(
                GetAsVec4(field, 0),
                GetAsVec4(field, 1),
                GetAsVec4(field, 2),
                GetAsVec4(field, 3)
            );

        public static void SetComponent(IFieldWriter field, int idx, float val) => field.SetField(GraphType.kC(idx), val);
        public static void SetComponents(IFieldWriter field, int idx, params float[] values)
        {
            foreach(var val in values)
                SetComponent(field, idx++, val);
        }

        public static void SetAsFloat(IFieldWriter field, float val) => SetComponent(field, 0, val);
        public static void SetAsBool(IFieldWriter field, bool val) => SetComponent(field, 0, val ? 1f : 0f);
        public static void SetAsInt(IFieldWriter field, int val) => SetComponent(field, 0, val);
        public static void SetAsVec2(IFieldWriter field, Vector2 val) => SetComponents(field, 0, val.x, val.y);
        public static void SetAsVec3(IFieldWriter field, Vector3 val) => SetComponents(field, 0, val.x, val.y, val.z);
        public static void SetAsVec4(IFieldWriter field, Vector4 val, int col = 0) => SetComponents(field, col * 4, val.x, val.y, val.z);
        public static void SetAsMat4(IFieldWriter field, Matrix4x4 val)
        {
            for (int i = 0; i < 4; ++i)
                SetAsVec4(field, val.GetColumn(i), i);
        }
    }

    /// <summary>
    /// Base 'GraphType' representing templated HLSL Types, eg. vector <float, 3>, matrix <float 4, 4>, int3, etc.
    /// </summary>
    internal class GraphType : Defs.ITypeDefinitionBuilder
    {
        public static RegistryKey kRegistryKey => new RegistryKey { Name = "GraphType", Version = 1 };
        public RegistryKey GetRegistryKey() => kRegistryKey;
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Type;

        public enum Precision { Fixed, Half, Single, Any }
        public enum Primitive { Bool, Int, Float, Any }
        public enum Length { One = 1, Two = 2, Three = 3, Four = 4, Any = -1 }
        public enum Height { One = 1, Two = 2, Three = 3, Four = 4, Any = -1 }

        // TODO: This is used by node builders and is general to all ports, should be moved into a CLDS header when possible.
        public enum Usage { In, Out, Static, Local }


        #region Priorities
        // Values here represent a resolving priority.
        // The highest numeric value has the highest priority.
        public static readonly Dictionary<Precision, int> PrecisionToPriority = new()
        {
            { Precision.Fixed, 1 },
            { Precision.Half, 2 },
            { Precision.Single, 3 },
            { Precision.Any, -1 }
        };
        public static readonly Dictionary<Primitive, int> PrimitiveToPriority = new()
        {
            { Primitive.Bool, 1 },
            { Primitive.Int, 2 },
            { Primitive.Float, 3 },
            { Primitive.Any, -1 }
        };
        public static readonly Dictionary<Length, int> LengthToPriority = new()
        {
            { Length.One, 1 },
            { Length.Two, 4 },
            { Length.Three, 3 },
            { Length.Four, 2 },
            { Length.Any, -1 }
        };
        public static readonly Dictionary<Height, int> HeightToPriority = new()
        {
            { Height.One, 1 },
            { Height.Two, 4 },
            { Height.Three, 3 },
            { Height.Four, 2 },
            { Height.Any, -1 }
        };
        #endregion

        #region LocalNames
        public const string kPrimitive = "Primitive";
        public const string kPrecision = "Precision";
        public const string kLength = "Length";
        public const string kHeight = "Height";

        // TODO: this is used by the interpreter and filled out by the context builder, should be moved into a CLDS header when possible.
        public const string kEntry = "_Entry";
        public static string kC(int i) => $"c{i}";
        #endregion

        public void BuildType(IFieldReader userData, IFieldWriter typeWriter, Registry registry)
        {
            // default initialize to a float4;
            typeWriter.SetField(kPrecision, Precision.Single);
            typeWriter.SetField(kPrimitive, Primitive.Float);
            typeWriter.SetField(kLength, Length.Four);
            typeWriter.SetField(kHeight, Height.One);

            // read userdata and make sure we have enough fields.
            if (!userData.GetField(kLength, out Length length))
                length = Length.Four;
            if (!userData.GetField(kHeight, out Height height))
                height = Height.One;

            // ensure that enough subfield values exist to represent userdata's current data.
            for (int i = 0; i < (int)length * (int)height; ++i)
                GraphTypeHelpers.SetComponent(typeWriter, i, 0);
        }

        string Defs.ITypeDefinitionBuilder.GetInitializerList(IFieldReader data, Registry registry)
        {
            var height = GraphTypeHelpers.GetHeight(data);
            var length = GraphTypeHelpers.GetLength(data);
            int l = Mathf.Clamp((int)length, 1, 4);
            int h = Mathf.Clamp((int)height, 1, 4);

            string result = $"{((Defs.ITypeDefinitionBuilder)this).GetShaderType(data, new ShaderFoundry.ShaderContainer(), registry).Name}" + "(";

            for (int i = 0; i < l * h; ++i)
            {
                result += $"{GraphTypeHelpers.GetComponent(data, i)}";
                if (i != l * h - 1)
                    result += ", ";
            }
            result += ")";
            return result;
        }

        ShaderFoundry.ShaderType Defs.ITypeDefinitionBuilder.GetShaderType(IFieldReader data, ShaderFoundry.ShaderContainer container, Registry registry)
        {
            var height = GraphTypeHelpers.GetHeight(data);
            var length = GraphTypeHelpers.GetLength(data);
            var primitive = GraphTypeHelpers.GetPrimitive(data);
            var precision = GraphTypeHelpers.GetPrecision(data);

            int l = Mathf.Clamp((int)length, 1, 4);
            int h = Mathf.Clamp((int)height, 1, 4);

            string name = "float";

            switch (primitive)
            {
                case Primitive.Bool: name = "bool"; break;
                case Primitive.Int: name = "int"; break;
                case Primitive.Float:
                    switch (precision)
                    {
                        case Precision.Fixed: name = "fixed"; break;
                        case Precision.Half: name = "half"; break;
                    }
                    break;
            }

            var shaderType = ShaderFoundry.ShaderType.Scalar(container, name);

            if (h != 1 && l != 1)
            {
                shaderType = ShaderFoundry.ShaderType.Matrix(container, shaderType, l, h);
            }
            else
            {
                shaderType = ShaderFoundry.ShaderType.Vector(container, shaderType, Mathf.Max(l, h));
            }
            return shaderType;
        }
    }

    internal class GraphTypeAssignment : Defs.ICastDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() => new RegistryKey { Name = "GraphTypeAssignment", Version = 1 };
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Cast;
        public (RegistryKey, RegistryKey) GetTypeConversionMapping() => (GraphType.kRegistryKey, GraphType.kRegistryKey);
        public bool CanConvert(IFieldReader src, IFieldReader dst)
        {
            var srcHgt = GraphTypeHelpers.GetHeight(src);
            var srcLen = GraphTypeHelpers.GetLength(src);

            var dstHgt = GraphTypeHelpers.GetHeight(dst);
            var dstLen = GraphTypeHelpers.GetLength(dst);

            return srcHgt == dstHgt || srcHgt == GraphType.Height.One && srcLen >= dstLen;
        }

        private static string MatrixCompNameFromIndex(int i, int d)
        {
            return $"_mm{ i / d }{ i % d }";
        }

        private static string VectorCompNameFromIndex(int i)
        {
            switch (i)
            {
                case 0: return "x";
                case 1: return "y";
                case 2: return "z";
                case 3: return "w";
                default: throw new Exception("Invalid vector index.");
            }
        }


        public ShaderFoundry.ShaderFunction GetShaderCast(IFieldReader src, IFieldReader dst, ShaderFoundry.ShaderContainer container, Registry registry)
        {
            // In this case, we can determine a casting operation purely from the built types. We don't actually need to analyze field data,
            // this is because it's all already been encapsulated in the previously built ShaderType.
            var srcType = registry.GetTypeBuilder(src.GetRegistryKey()).GetShaderType(src, container, registry);
            var dstType = registry.GetTypeBuilder(dst.GetRegistryKey()).GetShaderType(dst, container, registry);

            string castName = $"Cast{srcType.Name}_{dstType.Name}";
            var builder = new ShaderFoundry.ShaderFunction.Builder(container, castName);
            builder.AddInput(srcType, "In");
            builder.AddOutput(dstType, "Out");

            // CanConvert should prevent srcSize from being smaller than dstSize, but we will 0 fill just in case.
            var srcSize = srcType.IsVector ? srcType.VectorDimension : srcType.IsMatrix ? srcType.MatrixColumns * srcType.MatrixRows : 1;
            var dstSize = dstType.IsVector ? dstType.VectorDimension : dstType.IsMatrix ? dstType.MatrixColumns * dstType.MatrixRows : 1;

            string body = $"Out = {srcType.Name} {{ ";

            for (int i = 0; i < dstSize; ++i)
            {
                if (i < srcSize)
                {
                    if (dstType.IsMatrix) body += $"In.{MatrixCompNameFromIndex(i, dstType.MatrixColumns)}"; // are we row or column major?
                    if (dstType.IsVector) body += $"In.{VectorCompNameFromIndex(i)}";
                    if (dstType.IsScalar) body += $"In";
                }
                else body += "0";
                if (i != dstSize - 1) body += ", ";
            }
            body += " };";

            builder.AddLine(body);
            return builder.Build();
        }
    }
}
