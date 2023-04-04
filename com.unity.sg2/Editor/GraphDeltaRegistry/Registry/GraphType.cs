using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor.ShaderGraph.Defs;
using System.Collections;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    /// <summary>
    /// Experimenting with different helper function interfaces.
    /// </summary>
    public static class GraphTypeHelpers
    {
        #region DynamicPortResolution
        private static int Truncate(int a, int b) // scalars always lose.
            => Mathf.Max(
                a <= 1 ? b : b <= 1 ? a : // scalars always lose
                Mathf.Min(a, b),          // otherwise smallest wins
                1);                       // sanitize.
        private static void GetDim(FieldHandler field, out int length, out int height)
        {
            height = (int)GetHeight(field);
            length = (int)GetLength(field);
        }

        private static bool CalcResolve(NodeHandler node, out int length, out int height, out int precision, out int primitive)
        {
            length = 1;
            height = 1;
            precision = 1;
            primitive = 1;

            // Use only input ports who are actually graphType.
            var inputPorts = new List<PortHandler>();
            var connectedFields = new List<FieldHandler>();

            foreach(var port in node.GetPorts())
            {
                if(port.IsInput && port.GetTypeField().GetRegistryKey().Name.Equals(GraphType.kRegistryKey.Name, StringComparison.Ordinal))
                {
                    inputPorts.Add(port);
                }
            }

            foreach(var input in inputPorts)
            {
                PortHandler connected = null;
                using (var enumerator = input.GetConnectedPorts().GetEnumerator())
                {
                    if(enumerator.MoveNext())
                    {
                        connected = enumerator.Current;
                    }
                }
                if(connected != null && connected.GetTypeField() != null)
                {
                    connectedFields.Add(connected.GetTypeField());
                }
            }

            bool hasVector = inputPorts.Count != connectedFields.Count;


            foreach (var field in connectedFields)
            {
                GetDim(field, out var fieldLength, out var fieldHeight);
                length = Truncate(length, fieldLength);
                height = Truncate(Truncate(length, height), fieldHeight); // height truncates by length also.

                // special case- if there is any non-matrix connection, the output dynamic ports will also be nonMatrix.
                hasVector |= fieldHeight == 1;

                // precision and primitive type always promote.
                precision = Mathf.Max(precision, (int)GetPrecision(field));
                primitive = Mathf.Max(primitive, (int)GetPrimitive(field));
            }
            return hasVector;
        }

        #endregion

        public static void ResolveDynamicPorts(NodeHandler node)
        {
            // TODO: Resolve precision/primitive too.
            bool hasVector = CalcResolve(node, out var resolvedLength, out var resolvedHeight, out var resolvedPrecision, out var resolvedPrimitive);

            // foreach graphType port.
            foreach (var port in node.GetPorts())
            {
                if (port.GetTypeField().GetRegistryKey().Name.Equals(GraphType.kRegistryKey.Name, StringComparison.Ordinal))
                {
                    var field = port.GetTypeField();
                    var precision = GetPrecision(field);
                    var primitive = GetPrimitive(field);
                    GetDim(field, out var length, out var height);
                    GetDynamic(field, out var isLenDyn, out var isHgtDyn, out var isPrecisionDyn, out var isPrimDyn);

                    length = isLenDyn ? resolvedLength : length;
                    height = isHgtDyn ? resolvedHeight : height;
                    precision = isPrecisionDyn ? (GraphType.Precision)resolvedPrecision : precision;
                    primitive = isPrimDyn ? (GraphType.Primitive)resolvedPrimitive : primitive;

                    // resolvedHeight only applies for matrices-- if we are connected to a non-matrix we need to ignore it.
                    if (isHgtDyn)
                    {
                        var connectedField = port.IsInput ? port.GetConnectedPorts().FirstOrDefault()?.GetTypeField() : null;
                        if (!port.IsInput && hasVector || // output port will always resolve to vector if one of the input ports does.
                            port.IsInput && connectedField == null && hasVector || // as will disconnected input ports.
                            port.IsInput && connectedField != null && GetHeight(connectedField) == GraphType.Height.One)
                            height = 1;
                    }

                    InitGraphType(
                        field,
                        length: (GraphType.Length)length,
                        height: (GraphType.Height)height,
                        precision: precision,
                        primitive: primitive,
                        lengthDynamic: isLenDyn,
                        heightDynamic: isHgtDyn,
                        primitiveDynamic: isPrimDyn,
                        precisionDynamic: isPrecisionDyn);
                }
            }
        }

        public static void InitGraphType(
            FieldHandler field,
            GraphType.Length length = GraphType.Length.One,
            GraphType.Precision precision = GraphType.Precision.Single,
            GraphType.Primitive primitive = GraphType.Primitive.Float,
            GraphType.Height height = GraphType.Height.One,
            bool lengthDynamic = false,
            bool heightDynamic = false,
            bool primitiveDynamic = false,
            bool precisionDynamic = false)
        {
            var len = field.GetSubField<GraphType.Length>(GraphType.kLength);
            var hgt = field.GetSubField<GraphType.Height>(GraphType.kHeight);
            var pre = field.GetSubField<GraphType.Precision>(GraphType.kPrecision);
            var pri = field.GetSubField<GraphType.Primitive>(GraphType.kPrimitive);

            len.SetData(length);
            hgt.SetData(height);
            pre.SetData(precision);
            pri.SetData(primitive);

            len.GetSubField<bool>(GraphType.kDynamic).SetData(lengthDynamic);
            hgt.GetSubField<bool>(GraphType.kDynamic).SetData(heightDynamic);
            pre.GetSubField<bool>(GraphType.kDynamic).SetData(precisionDynamic);
            pri.GetSubField<bool>(GraphType.kDynamic).SetData(primitiveDynamic);

        }

        public static void GetDynamic(FieldHandler field, out bool length, out bool height, out bool precision, out bool primitive)
        {
            length = field.GetSubField(GraphType.kLength).GetSubField<bool>(GraphType.kDynamic).GetData();
            height = field.GetSubField(GraphType.kHeight).GetSubField<bool>(GraphType.kDynamic).GetData();
            precision = field.GetSubField(GraphType.kPrecision).GetSubField<bool>(GraphType.kDynamic).GetData();
            primitive = field.GetSubField(GraphType.kPrimitive).GetSubField<bool>(GraphType.kDynamic).GetData();
        }

        public static GraphType.Precision GetPrecision(FieldHandler field) =>
            field.GetSubField<GraphType.Precision>(GraphType.kPrecision).GetData();

        public static GraphType.Primitive GetPrimitive(FieldHandler field)
        {
            var primitiveField = field.GetSubField<GraphType.Primitive>(GraphType.kPrimitive);
            return primitiveField?.GetData() ?? GraphType.Primitive.Any;
        }

        public static GraphType.Length GetLength(FieldHandler field)
        {
            var lengthField = field.GetSubField<GraphType.Length>(GraphType.kLength);
            return lengthField?.GetData() ?? GraphType.Length.Any;
        }

        public static GraphType.Height GetHeight(FieldHandler field)
        {
            var heightField = field.GetSubField<GraphType.Height>(GraphType.kHeight);
            return heightField?.GetData() ?? GraphType.Height.Any;
        }

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

        public static Vector2 GetAsVec2(FieldHandler field, int col = 0) =>
            new(
                GetComponent(field, 0 + col * 2),
                GetComponent(field, 1 + col * 2)
            );

        public static Vector3 GetAsVec3(FieldHandler field, int col = 0) =>
            new(
                GetComponent(field, 0 + col * 3),
                GetComponent(field, 1 + col * 3),
                GetComponent(field, 2 + col * 3)
            );

        public static Vector4 GetAsVec4(FieldHandler field, int col = 0) =>
            new(
                GetComponent(field, 0 + col * 4),
                GetComponent(field, 1 + col * 4),
                GetComponent(field, 2 + col * 4),
                GetComponent(field, 3 + col * 4)
            );

        public static Matrix4x4 GetAsMat2(FieldHandler field) =>
            new(
                GetAsVec2(field, 0),
                GetAsVec2(field, 1),
                Vector4.zero,
                Vector4.zero
            );

        public static Matrix4x4 GetAsMat3(FieldHandler field) =>
            new(
                GetAsVec3(field, 0),
                GetAsVec3(field, 1),
                GetAsVec3(field, 2),
                Vector4.zero
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
            var sub = field.GetSubField<float>(GraphType.kC(idx)) ?? field.AddSubField(GraphType.kC(idx), val, true);
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

        public static void SetAsVec2(FieldHandler field, Vector2 val, int col = 0) =>
            SetComponents(field, col * 2, val.x, val.y);

        public static void SetAsVec3(FieldHandler field, Vector3 val, int col = 0) =>
            SetComponents(field, col * 3, val.x, val.y, val.z);

        public static void SetAsVec4(FieldHandler field, Vector4 val, int col = 0) =>
            SetComponents(field, col * 4, val.x, val.y, val.z, val.w);

        public static void SetAsMat2(FieldHandler field, Matrix4x4 val)
        {
            for (int i = 0; i < 2; ++i)
                SetAsVec2(field, val.GetColumn(i), i);
        }

        public static void SetAsMat3(FieldHandler field, Matrix4x4 val)
        {
            for (int i = 0; i < 3; ++i)
                SetAsVec3(field, val.GetColumn(i), i);
        }

        public static void SetAsMat4(FieldHandler field, Matrix4x4 val)
        {
            for (int i = 0; i < 4; ++i)
                SetAsVec4(field, val.GetColumn(i), i);
        }

        public static bool SetByManaged(FieldHandler f, object o)
        {
            GetDim(f, out int _, out int h);

            switch(o)
            {
                case Vector4 v4: SetAsVec4(f, v4); break;
                case Vector3 v3: SetAsVec3(f, v3); break;
                case Vector2 v2: SetAsVec2(f, v2); break;
                case float: case bool: case int:
                    SetAsFloat(f, System.Convert.ToSingle(o)); break;
                case IEnumerable<float> e:
                    SetComponents(f, 0, e.ToArray()); break;
                case Matrix4x4 m4:
                    switch(h)
                    {
                        case 1: SetAsVec4(f, m4.GetColumn(0)); break;
                        case 2: SetAsMat2(f, m4); break;
                        case 3: SetAsMat3(f, m4); break;
                        case 4: SetAsMat4(f, m4); break;
                    }
                    break;
                default:
                    return false;
            }
            return true;
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

        public enum Precision { Fixed = 1, Half = 2, Single = 3, Any = -1 }
        public enum Primitive { Bool = 1, Int = 2, Float = 3, Any = -1 }
        public enum Length { One = 1, Two = 2, Three = 3, Four = 4, Any = -1 }
        public enum Height { One = 1, Two = 2, Three = 3, Four = 4, Any = -1 }

        // TODO: This is used by node builders and is general to all ports,
        // TODO should be moved into a CLDS header when possible (metadata).
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
        public const string kDynamic = "Dynamic";

        // TODO: this is used by the interpreter and filled out by the context builder,
        // should be moved into a CLDS header when possible.
        public const string kEntry = "_Entry";
        public static string kC(int i) => $"c{i}";
        #endregion

        public void BuildType(FieldHandler field, Registry registry)
        {
            // TODO: Default initialization should be a non-dynamic scalar single precision float.
            field.AddSubField(kPrecision, Precision.Single, true).AddSubField(kDynamic, false, true);
            field.AddSubField(kPrimitive, Primitive.Float, true).AddSubField(kDynamic, false, true);
            field.AddSubField(kLength, Length.One, true).AddSubField(kDynamic, false, true);
            field.AddSubField(kHeight, Height.One, true).AddSubField(kDynamic, false, true);

            // ensure we have enough allocated.
            for (int i = 0; i < 16; ++i)
                GraphTypeHelpers.SetComponent(field, i, 0);
        }

        public void CopySubFieldData(FieldHandler src, FieldHandler dst)
        {
            var length = GraphTypeHelpers.GetLength(src);
            var height = GraphTypeHelpers.GetHeight(src);
            var primitive = GraphTypeHelpers.GetPrimitive(src);
            var precision = GraphTypeHelpers.GetPrecision(src);
            var data = GraphTypeHelpers.GetComponents(src).ToArray();
            GraphTypeHelpers.GetDynamic(src, out var lengthDynamic, out var heightDynamic, out var precisionDynamic, out var primitiveDynamic);


            GraphTypeHelpers.InitGraphType(
                dst,
                length: length,
                height: height,
                precision: precision,
                primitive: primitive,
                lengthDynamic: lengthDynamic,
                heightDynamic: heightDynamic,
                primitiveDynamic: precisionDynamic,
                precisionDynamic: primitiveDynamic);

            GraphTypeHelpers.SetComponents(dst, 0, data);
        }

        string ITypeDefinitionBuilder.GetInitializerList(FieldHandler data, Registry registry)
        {
            var height = GraphTypeHelpers.GetHeight(data);
            var length = GraphTypeHelpers.GetLength(data);
            int l = Mathf.Clamp((int)length, 1, 4);
            int h = Mathf.Clamp((int)height, 1, 4);
            string name = ((ITypeDefinitionBuilder)this).GetShaderType(data, new ShaderFoundry.ShaderContainer(), registry).Name;
            var values = GraphTypeHelpers.GetComponents(data).ToArray();

            return ParametricTypeUtils.ParametricToHLSL(name, l, h, values);
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
                        case Precision.Fixed: name = "float"; break;
                        case Precision.Half: name = "half"; break;
                    }
                    break;
            }

            var shaderType = ShaderFoundry.ShaderType.Scalar(container, name);

            if (h != 1 && l != 1)
            {
                shaderType = ShaderFoundry.ShaderType.Matrix(container, shaderType, l, h);
            }
            else if(h != 1 || l != 1)
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
            var srcHgt = (int)GraphTypeHelpers.GetHeight(src);
            var srcLen = (int)GraphTypeHelpers.GetLength(src);

            var dstHgt = (int)GraphTypeHelpers.GetHeight(dst);
            var dstLen = (int)GraphTypeHelpers.GetLength(dst);

            GraphTypeHelpers.GetDynamic(dst, out var dynLen, out var dynHgt, out _, out _);

            return
                srcHgt == 1 && srcLen == 1 || // scalars can always promote to anything.
                srcHgt == 1 && dstHgt == 1 || // vectors can truncate/zero-fill promote to other vectors.
                // otherwise we truncate per dimension unless one of those dimensions is dynamic.
                (srcLen >= dstLen || dynLen) && (srcHgt >= dstHgt || dynHgt);
            // TODO: Can we just allow universal convertibility w/zero fill?
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

            string body = "";
            if (srcType.IsScalar || dstType.IsScalar || srcSize >= dstSize)
            {
                //honestly HLSL automatic casting solves this for most cases
                body = $"Out = ({dstType.Name}) In; ";
            }
            else
            {
                body = $"Out = {dstType.Name} ( ";

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
                body += " );";
            }

            builder.AddLine(body);
            return builder.Build();
        }
    }
}
