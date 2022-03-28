using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    /// <summary>
    /// Experimenting with different helper function interfaces.
    /// </summary>
    public static class GraphTypeHelpers
    {
        public static GraphType.Precision GetPrecision(FieldHandler field) =>
            field.GetSubField<GraphType.Precision>(GraphType.kPrecision).GetData();

        public static GraphType.Primitive GetPrimitive(FieldHandler field) =>
            field.GetSubField<GraphType.Primitive>(GraphType.kPrimitive).GetData();

        public static GraphType.Length GetLength(FieldHandler field) =>
            field.GetSubField<GraphType.Length>(GraphType.kLength).GetData();

        public static GraphType.Height GetHeight(FieldHandler field) =>
            field.GetSubField<GraphType.Height>(GraphType.kHeight).GetData();

        public static float GetComponent(FieldHandler field, int idx) =>
            field.GetSubField<float>(GraphType.kC(idx))?.GetData() ?? 0;


        public static IEnumerable<float> GetComponents(FieldHandler field, int idx = 0)
        {
            for (; idx < (int)GetLength(field) * (int)GetHeight(field); ++idx)
                yield return GetComponent(field, idx);
        }

        public static Type GetManagedType(FieldHandler field)
        {
            var primitive = GetPrimitive(field);
            var length = GetLength(field);
            var height = GetHeight(field);
            if (height == GraphType.Height.Four && length == GraphType.Length.Four)
                return typeof(Matrix4x4);

            return length switch
            {
                GraphType.Length.One => primitive switch
                {
                    GraphType.Primitive.Bool => typeof(bool),
                    GraphType.Primitive.Int => typeof(int),
                    _ => typeof(float),
                },
                GraphType.Length.Two => typeof(Vector2),
                GraphType.Length.Three => typeof(Vector3),
                _ => typeof(Vector4),
            };
        }

        public static bool GetAsBool(FieldHandler field) =>
            GetComponent(field, 0) != 0;

        public static float GetAsFloat(FieldHandler field) =>
            GetComponent(field, 0);

        public static int GetAsInt(FieldHandler field) =>
            (int)GetComponent(field, 0);

        public static Vector2 GetAsVec2(FieldHandler field) =>
            new(GetComponent(field, 0), GetComponent(field, 1));

        public static Vector3 GetAsVec3(FieldHandler field) =>
            new(GetComponent(field, 0), GetComponent(field, 1), GetComponent(field, 2));

        public static Vector4 GetAsVec4(FieldHandler field, int col = 0) =>
            new(
                GetComponent(field, 0 + col * 4),
                GetComponent(field, 1 + col * 4),
                GetComponent(field, 2 + col * 4),
                GetComponent(field, 3 + col * 4)
            );
        public static Matrix4x4 GetAsMat4(FieldHandler field) =>
            new(
                GetAsVec4(field, 0),
                GetAsVec4(field, 1),
                GetAsVec4(field, 2),
                GetAsVec4(field, 3)
            );

        public static void SetComponent(FieldHandler field, int idx, float val)
        {
            var sub = field.GetSubField<float>(GraphType.kC(idx)) ?? field.AddSubField(GraphType.kC(idx), val);
            sub.SetData(val);
        }

        public static void SetComponents(FieldHandler field, int idx, params float[] values)
        {
            foreach(var val in values)
                SetComponent(field, idx++, val);
        }

        public static void SetAsFloat(FieldHandler field, float val) =>
            SetComponent(field, 0, val);

        public static void SetAsBool(FieldHandler field, bool val) =>
            SetComponent(field, 0, val ? 1f : 0f);

        public static void SetAsInt(FieldHandler field, int val) =>
            SetComponent(field, 0, val);

        public static void SetAsVec2(FieldHandler field, Vector2 val) =>
            SetComponents(field, 0, val.x, val.y);

        public static void SetAsVec3(FieldHandler field, Vector3 val) =>
            SetComponents(field, 0, val.x, val.y, val.z);

        public static void SetAsVec4(FieldHandler field, Vector4 val, int col = 0) =>
            SetComponents(field, col * 4, val.x, val.y, val.z);

        public static void SetAsMat4(FieldHandler field, Matrix4x4 val)
        {
            for (int i = 0; i < 4; ++i)
                SetAsVec4(field, val.GetColumn(i), i);
        }
    }

    /// <summary>
    /// Base 'GraphType' representing templated HLSL Types, eg. vector <float, 3>, matrix <float 4, 4>, int3, etc.
    /// </summary>
    public class GraphType : ITypeDefinitionBuilder
    {
        public static RegistryKey kRegistryKey => new() { Name = "GraphType", Version = 1 };
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

        // TODO: this is used by the interpreter and filled out by the context builder,
        // should be moved into a CLDS header when possible.
        public const string kEntry = "_Entry";
        public static string kC(int i) => $"c{i}";
        #endregion

        public void BuildType(FieldHandler field, Registry registry)
        {
            // default initialize to a float4;
            field.AddSubField(kPrecision, Precision.Single);
            field.AddSubField(kPrimitive, Primitive.Float);
            field.AddSubField(kLength, Length.Four);
            field.AddSubField(kHeight, Height.One);


            var lenField = field.GetSubField<Length>(kLength);
            var hgtField = field.GetSubField<Height>(kHeight);
            var length = Length.Four;
            var height = Height.One;

            // read userdata and make sure we have enough fields.
            if (lenField != null) length = lenField.GetData();
            if (hgtField != null) height = hgtField.GetData();

            // ensure that enough subfield values exist to represent userdata's current data.
            // we could just ignore userData though and just fill out all 16 possible components...
            for (int i = 0; i < (int)length * (int)height; ++i)
                GraphTypeHelpers.SetComponent(field, i, 0);
        }

        string ITypeDefinitionBuilder.GetInitializerList(FieldHandler data, Registry registry)
        {
            var height = GraphTypeHelpers.GetHeight(data);
            var length = GraphTypeHelpers.GetLength(data);
            int l = Mathf.Clamp((int)length, 1, 4);
            int h = Mathf.Clamp((int)height, 1, 4);

            string result = $"{((ITypeDefinitionBuilder)this).GetShaderType(data, new ShaderFoundry.ShaderContainer(), registry).Name}" + "(";

            for (int i = 0; i < l * h; ++i)
            {
                result += $"{GraphTypeHelpers.GetComponent(data, i)}";
                if (i != l * h - 1)
                    result += ", ";
            }
            result += ")";
            return result;
        }

        ShaderFoundry.ShaderType ITypeDefinitionBuilder.GetShaderType(FieldHandler data, ShaderFoundry.ShaderContainer container, Registry registry)
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

    internal class GraphTypeAssignment : ICastDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() =>
            new() { Name = "GraphTypeAssignment", Version = 1 };

        public RegistryFlags GetRegistryFlags() =>
            RegistryFlags.Cast;

        public (RegistryKey, RegistryKey) GetTypeConversionMapping() =>
            (GraphType.kRegistryKey, GraphType.kRegistryKey);

        public bool CanConvert(FieldHandler src, FieldHandler dst)
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
            return i switch
            {
                0 => "x",
                1 => "y",
                2 => "z",
                3 => "w",
                _ => throw new Exception("Invalid vector index."),
            };
        }


        public ShaderFoundry.ShaderFunction GetShaderCast(
            FieldHandler src,
            FieldHandler dst,
            ShaderFoundry.ShaderContainer container,
            Registry registry)
        {
            // In this case, we can determine a casting operation purely from the built types.
            // We don't actually need to analyze field data, this is because it's all already
            // been encapsulated in the previously built ShaderType.
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
