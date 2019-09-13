using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    class ShaderGenerator
    {
        private struct ShaderChunk
        {
            public ShaderChunk(int indentLevel, string shaderChunkString)
            {
                m_IndentLevel = indentLevel;
                m_ShaderChunkString = shaderChunkString;
            }

            private readonly int m_IndentLevel;
            private readonly string m_ShaderChunkString;

            public int chunkIndentLevel
            {
                get { return m_IndentLevel; }
            }

            public string chunkString
            {
                get { return m_ShaderChunkString; }
            }
        }

        private readonly List<ShaderChunk> m_ShaderChunks = new List<ShaderChunk>();
        private int m_IndentLevel;
        private string m_Pragma = string.Empty;

        public void AddPragmaChunk(string s)
        {
            m_Pragma += s;
        }

        public string GetPragmaString()
        {
            return m_Pragma;
        }

        public void AddNewLine()
        {
            m_ShaderChunks.Add(new ShaderChunk(m_IndentLevel, ""));
        }

        public void AddShaderChunk(string s, bool unique = false)
        {
            if (string.IsNullOrEmpty(s))
                return;

            if (unique && m_ShaderChunks.Any(x => x.chunkString == s))
                return;

            m_ShaderChunks.Add(new ShaderChunk(m_IndentLevel, s));
        }

        public void AddGenerator(ShaderGenerator generator)
        {
            foreach (ShaderChunk chunk in generator.m_ShaderChunks)
            {
                m_ShaderChunks.Add(new ShaderChunk(m_IndentLevel + chunk.chunkIndentLevel, chunk.chunkString));
            }
        }

        public void Indent()
        {
            m_IndentLevel++;
        }

        public void Deindent()
        {
            m_IndentLevel = Math.Max(0, m_IndentLevel - 1);
        }

        public string GetShaderString(int baseIndentLevel, bool finalNewline = true)
        {
            bool appendedNewline = false;
            var sb = new StringBuilder();
            foreach (var shaderChunk in m_ShaderChunks)
            {
                // TODO: regex split is a slow way to handle this .. should build an iterator instead
                var lines = Regex.Split(shaderChunk.chunkString, Environment.NewLine);
                for (int index = 0; index < lines.Length; index++)
                {
                    var line = lines[index];
                    for (var i = 0; i < shaderChunk.chunkIndentLevel + baseIndentLevel; i++)
                    {
                        //sb.Append("\t");
                        sb.Append("    "); // unity convention use space instead of tab...
                    }

                    sb.AppendLine(line);
                    appendedNewline = true;
                }
            }
            if (appendedNewline && !finalNewline)
            {
                // remove last newline if we didn't want it
                sb.Length -= Environment.NewLine.Length;
            }
            return sb.ToString();
        }

        private const string kErrorString = @"ERROR!";

        public static string AdaptNodeOutput(AbstractMaterialNode node, int outputSlotId, ConcreteSlotValueType convertToType)
        {
            var outputSlot = node.FindOutputSlot<MaterialSlot>(outputSlotId);

            if (outputSlot == null)
                return kErrorString;

            var convertFromType = outputSlot.concreteValueType;
            var rawOutput = node.GetVariableNameForSlot(outputSlotId);
            if (convertFromType == convertToType)
                return rawOutput;

            switch (convertToType)
            {
                case ConcreteSlotValueType.Vector1:
                    return string.Format("({0}).x", rawOutput);
                case ConcreteSlotValueType.Vector2:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("({0}.xx)", rawOutput);
                        case ConcreteSlotValueType.Vector3:
                        case ConcreteSlotValueType.Vector4:
                            return string.Format("({0}.xy)", rawOutput);
                        default:
                            return kErrorString;
                    }
                case ConcreteSlotValueType.Vector3:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("({0}.xxx)", rawOutput);
                        case ConcreteSlotValueType.Vector2:
                            return string.Format("($precision3({0}, 0.0))", rawOutput);
                        case ConcreteSlotValueType.Vector4:
                            return string.Format("({0}.xyz)", rawOutput);
                        default:
                            return kErrorString;
                    }
                case ConcreteSlotValueType.Vector4:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("({0}.xxxx)", rawOutput);
                        case ConcreteSlotValueType.Vector2:
                            return string.Format("($precision4({0}, 0.0, 1.0))", rawOutput);
                        case ConcreteSlotValueType.Vector3:
                            return string.Format("($precision4({0}, 1.0))", rawOutput);
                        default:
                            return kErrorString;
                    }
                case ConcreteSlotValueType.Matrix3:
                    return rawOutput;
                case ConcreteSlotValueType.Matrix2:
                    return rawOutput;
                default:
                    return kErrorString;
            }
        }

        public static string AdaptNodeOutputForPreview(AbstractMaterialNode node, int outputSlotId)
        {
            var rawOutput = node.GetVariableNameForSlot(outputSlotId);
            return AdaptNodeOutputForPreview(node, outputSlotId, rawOutput);
        }

        public static string AdaptNodeOutputForPreview(AbstractMaterialNode node, int slotId, string variableName)
        {
            var slot = node.FindSlot<MaterialSlot>(slotId);

            if (slot == null)
                return kErrorString;

            var convertFromType = slot.concreteValueType;

            // preview is always dimension 4
            switch (convertFromType)
            {
                case ConcreteSlotValueType.Vector1:
                    return string.Format("half4({0}, {0}, {0}, 1.0)", variableName);
                case ConcreteSlotValueType.Vector2:
                    return string.Format("half4({0}.x, {0}.y, 0.0, 1.0)", variableName);
                case ConcreteSlotValueType.Vector3:
                    return string.Format("half4({0}.x, {0}.y, {0}.z, 1.0)", variableName);
                case ConcreteSlotValueType.Vector4:
                    return string.Format("half4({0}.x, {0}.y, {0}.z, 1.0)", variableName);
                case ConcreteSlotValueType.Boolean:
                    return string.Format("half4({0}, {0}, {0}, 1.0)", variableName);
                default:
                    return "half4(0, 0, 0, 0)";
            }
        }

        public int numberOfChunks
        {
            get { return m_ShaderChunks.Count; }
        }

        public enum InputType
        {
            Position,
            Vector,
            Normal
        }

        public struct TransformDesc
        {
            public TransformDesc(string name)
            {
                this.name = name;
                transpose = false;
            }

            public TransformDesc(string name, bool transpose)
            {
                this.name = name;
                this.transpose = transpose;
            }

            public string name;
            public bool transpose;
        }

        static TransformDesc[, ][] m_transforms = null;

        static TransformDesc[] GetTransformPath(CoordinateSpace from, CoordinateSpace to)
        {
            if (m_transforms[(int)from, (int)to] != null)
            {
                return m_transforms[(int)from, (int)to];
            }
            var distance = new int[5];
            var prev = new CoordinateSpace ? [5];
            var queue = new List<CoordinateSpace>();
            foreach (var space in Enum.GetValues(typeof(CoordinateSpace)))
            {
                distance[(int)space] = int.MaxValue;
                prev[(int)space] = null;
                queue.Add((CoordinateSpace)space);
            }
            distance[(int)from] = 0;
            List<CoordinateSpace> path = null;
            while (queue.Count != 0)
            {
                queue.Sort((x, y) => distance[(int)x] - distance[(int)y]);
                var min = queue[0];
                queue.Remove(min);
                if (min == to)
                {
                    path = new List<CoordinateSpace>();
                    while (prev[(int)min] != null)
                    {
                        path.Add(min);
                        min = prev[(int)min].Value;
                    }
                    break;
                }
                if (distance[(int)min] == int.MaxValue)
                {
                    break;
                }
                foreach (var space in Enum.GetValues(typeof(CoordinateSpace)))
                {
                    int index = (int)space;
                    if (m_transforms[(int)min, index] != null)
                    {
                        var alt = distance[(int)min] + m_transforms[(int)min, index].Length;
                        if (alt < distance[index])
                        {
                            distance[index] = alt;
                            prev[index] = min;
                        }
                    }
                }
            }
            path.Reverse();
            var matrixList = new List<TransformDesc>();
            foreach (var node in path)
            {
                matrixList.AddRange(m_transforms[(int)from, (int)node]);
                from = node;
            }

            return matrixList.ToArray();
        }

        static void InitTransforms()
        {
            if (m_transforms == null)
            {
                m_transforms = new TransformDesc[5, 5][];
                m_transforms[(int)CoordinateSpace.Object, (int)CoordinateSpace.Object] = new TransformDesc[] {};
                m_transforms[(int)CoordinateSpace.View, (int)CoordinateSpace.View] = new TransformDesc[] {};
                m_transforms[(int)CoordinateSpace.World, (int)CoordinateSpace.World] = new TransformDesc[] {};
                m_transforms[(int)CoordinateSpace.Tangent, (int)CoordinateSpace.Tangent] = new TransformDesc[] {};
                m_transforms[(int)CoordinateSpace.AbsoluteWorld, (int)CoordinateSpace.AbsoluteWorld] = new TransformDesc[] {};
                m_transforms[(int)CoordinateSpace.Object, (int)CoordinateSpace.World]
                    = new TransformDesc[] {new TransformDesc(MatrixNames.Model)};
                m_transforms[(int)CoordinateSpace.View, (int)CoordinateSpace.World]
                    = new TransformDesc[] {new TransformDesc(MatrixNames.ViewInverse) };
                m_transforms[(int)CoordinateSpace.World, (int)CoordinateSpace.Object]
                    = new TransformDesc[] {new TransformDesc(MatrixNames.ModelInverse)};
                m_transforms[(int)CoordinateSpace.World, (int)CoordinateSpace.View]
                    = new TransformDesc[] {new TransformDesc(MatrixNames.View)};
                for (var from = CoordinateSpace.Object; from != CoordinateSpace.Tangent; from++)
                {
                    for (var to = CoordinateSpace.Object; to != CoordinateSpace.Tangent; to++)
                    {
                        if (m_transforms[(int)from, (int)to] == null)
                        {
                            m_transforms[(int)from, (int)to] = GetTransformPath(from, to);
                        }
                    }
                }
            }
            for (var k = CoordinateSpace.Object; k != CoordinateSpace.Tangent; k++)
            {
                m_transforms[(int)CoordinateSpace.Tangent, (int)k] = null;
                m_transforms[(int)k, (int)CoordinateSpace.Tangent] = null;
            }
        }

        public static string EmitTransform(TransformDesc[] matrices, TransformDesc[] invMatrices, string variable, bool isAffine, bool noMatrixCast, bool inverseTranspose)
        {
            // Use inverse transpose for situations where
            // scale needs to be considered (normals)
            if (inverseTranspose)
                matrices = invMatrices;

            if (isAffine)
            {
                variable = string.Format("float4({0},1.0)", variable);
            }
            foreach (var m in matrices)
            {
                var matrix = m.name;
                if (!isAffine && !noMatrixCast)
                {
                    matrix = "(float3x3)" + matrix;
                }

                // if the matrix is NOT a transpose type
                // invert the order of multiplication
                // it is implicit transpose.
                if (m.transpose)
                    inverseTranspose = !inverseTranspose;
                variable = inverseTranspose
                    ? string.Format("mul({1},{0})", matrix, variable)
                    : string.Format("mul({0},{1})", matrix, variable);
            }
            return variable;
        }

        public static string ConvertBetweenSpace(string variable, CoordinateSpace from, CoordinateSpace to,
            InputType inputType, CoordinateSpace tangentMatrixSpace = CoordinateSpace.Object)
        {
            if (from == to)
            {
                // nothing to do
                return variable;
            }
            // Ensure that the transform graph is initialized
            InitTransforms();
            bool isNormal = false;
            bool affine = (inputType == InputType.Position && to != CoordinateSpace.World);
            bool noMatrixCast = (inputType == InputType.Position && to == CoordinateSpace.World);
            if (inputType == InputType.Normal)
            {
                inputType = InputType.Vector;
                isNormal = true;
            }
            m_transforms[(int)CoordinateSpace.Tangent, (int)tangentMatrixSpace] =
                new[] {new TransformDesc("tangentSpaceTransform")};
            m_transforms[(int)tangentMatrixSpace, (int)CoordinateSpace.Tangent] = new[]
            {new TransformDesc("tangentSpaceTransform", true)};
            if (from == CoordinateSpace.Tangent)
            {
                // if converting from tangent space, reuse the underlying space
                from = tangentMatrixSpace;
                variable = EmitTransform(
                        GetTransformPath(CoordinateSpace.Tangent, tangentMatrixSpace),
                        GetTransformPath(tangentMatrixSpace, CoordinateSpace.Tangent),
                        variable, affine, noMatrixCast, !isNormal);
                if (to == tangentMatrixSpace)
                {
                    return variable;
                }
            }
            return EmitTransform(GetTransformPath(from, to), GetTransformPath(to, from), variable, affine, noMatrixCast, isNormal);
        }

        public static void GenerateSpaceTranslationSurfaceInputs(
            NeededCoordinateSpace neededSpaces,
            InterpolatorType interpolatorType,
            ShaderStringBuilder builder,
            string format = "float3 {0};")
        {
            if ((neededSpaces & NeededCoordinateSpace.Object) > 0)
                builder.AppendLine(format, CoordinateSpace.Object.ToVariableName(interpolatorType));

            if ((neededSpaces & NeededCoordinateSpace.World) > 0)
                builder.AppendLine(format, CoordinateSpace.World.ToVariableName(interpolatorType));

            if ((neededSpaces & NeededCoordinateSpace.View) > 0)
                builder.AppendLine(format, CoordinateSpace.View.ToVariableName(interpolatorType));

            if ((neededSpaces & NeededCoordinateSpace.Tangent) > 0)
                builder.AppendLine(format, CoordinateSpace.Tangent.ToVariableName(interpolatorType));
            
            if ((neededSpaces & NeededCoordinateSpace.AbsoluteWorld) > 0)
                builder.AppendLine(format, CoordinateSpace.AbsoluteWorld.ToVariableName(interpolatorType));
        }

        public enum Dimension
        {
            One,
            Two,
            Three,
            Four
        }

        private static string DimensionToString(Dimension d)
        {
            switch (d)
            {
                case Dimension.One:
                    return string.Empty;
                case Dimension.Two:
                    return "2";
                case Dimension.Three:
                    return "3";
                case Dimension.Four:
                    return "4";
            }
            return "error";
        }

        private static string DimensionToSwizzle(Dimension d)
        {
            switch (d)
            {
                case Dimension.One:
                    return "x";
                case Dimension.Two:
                    return "xy";
                case Dimension.Three:
                    return "xyz";
                case Dimension.Four:
                    return "xyzw";
            }
            return "error";
        }

        public static void GenerateSpaceTranslations(
            NeededCoordinateSpace neededSpaces,
            InterpolatorType type,
            CoordinateSpace from,
            InputType inputType,
            ShaderStringBuilder pixelShader,
            Dimension dimension)
        {
            if ((neededSpaces & NeededCoordinateSpace.Object) > 0 && from != CoordinateSpace.Object)
                pixelShader.AppendLine("float{0} {1} = {2}.{3};", DimensionToString(dimension),
                    CoordinateSpace.Object.ToVariableName(type), ConvertBetweenSpace(from.ToVariableName(type), from, CoordinateSpace.Object, inputType, from),
                    DimensionToSwizzle(dimension));

            if ((neededSpaces & NeededCoordinateSpace.World) > 0 && from != CoordinateSpace.World)
                pixelShader.AppendLine("float{0} {1} = {2}.{3};", DimensionToString(dimension),
                    CoordinateSpace.World.ToVariableName(type), ConvertBetweenSpace(from.ToVariableName(type), from, CoordinateSpace.World, inputType, from),
                    DimensionToSwizzle(dimension));

            if ((neededSpaces & NeededCoordinateSpace.View) > 0 && from != CoordinateSpace.View)
                pixelShader.AppendLine("float{0} {1} = {2}.{3};", DimensionToString(dimension),
                    CoordinateSpace.View.ToVariableName(type),
                    ConvertBetweenSpace(from.ToVariableName(type), from, CoordinateSpace.View, inputType, from),
                    DimensionToSwizzle(dimension));

            if ((neededSpaces & NeededCoordinateSpace.Tangent) > 0 && from != CoordinateSpace.Tangent)
                pixelShader.AppendLine("float{0} {1} = {2}.{3};", DimensionToString(dimension),
                    CoordinateSpace.Tangent.ToVariableName(type),
                    ConvertBetweenSpace(from.ToVariableName(type), from, CoordinateSpace.Tangent, inputType, from),
                    DimensionToSwizzle(dimension));

            if ((neededSpaces & NeededCoordinateSpace.AbsoluteWorld) > 0 && from != CoordinateSpace.AbsoluteWorld)
               pixelShader.AppendLine("float{0} {1} = GetAbsolutePositionWS({2}).{3};", DimensionToString(dimension),
                   CoordinateSpace.AbsoluteWorld.ToVariableName(type),
                   CoordinateSpace.World.ToVariableName(type),
                   DimensionToSwizzle(dimension));
        }

        public static SurfaceMaterialTags BuildMaterialTags(SurfaceType surfaceType)
        {
            SurfaceMaterialTags materialTags = new SurfaceMaterialTags();

            if (surfaceType == SurfaceType.Opaque)
            {
                materialTags.renderQueue = SurfaceMaterialTags.RenderQueue.Geometry;
                materialTags.renderType = SurfaceMaterialTags.RenderType.Opaque;
            }
            else
            {
                materialTags.renderQueue = SurfaceMaterialTags.RenderQueue.Transparent;
                materialTags.renderType = SurfaceMaterialTags.RenderType.Transparent;
            }

            return materialTags;
        }

        public static SurfaceMaterialOptions GetMaterialOptions(SurfaceType surfaceType, AlphaMode alphaMode, bool twoSided, bool offscreenTransparent = false)
        {
            var materialOptions = new SurfaceMaterialOptions();
            switch (surfaceType)
            {
                case SurfaceType.Opaque:
                    materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                    materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.Zero;
                    materialOptions.cullMode = twoSided ? SurfaceMaterialOptions.CullMode.Off : SurfaceMaterialOptions.CullMode.Back;
                    materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                    materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.On;
                    break;
                case SurfaceType.Transparent:
                    switch (alphaMode)
                    {
                        case AlphaMode.Alpha:
                            materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.SrcAlpha;
                            materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.OneMinusSrcAlpha;
                            if(offscreenTransparent)
                            {
                                materialOptions.alphaSrcBlend = SurfaceMaterialOptions.BlendMode.Zero;

                            }
                            else
                            {
                                materialOptions.alphaSrcBlend = SurfaceMaterialOptions.BlendMode.One;
                            }
                            materialOptions.alphaDstBlend = SurfaceMaterialOptions.BlendMode.OneMinusSrcAlpha;
                            materialOptions.cullMode = twoSided ? SurfaceMaterialOptions.CullMode.Off : SurfaceMaterialOptions.CullMode.Back;
                            materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                            materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                            break;
                        case AlphaMode.Premultiply:
                            materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                            materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.OneMinusSrcAlpha;
                            if (offscreenTransparent)
                            {
                                materialOptions.alphaSrcBlend = SurfaceMaterialOptions.BlendMode.Zero;

                            }
                            else
                            {
                                materialOptions.alphaSrcBlend = SurfaceMaterialOptions.BlendMode.One;
                            }
                            materialOptions.alphaDstBlend = SurfaceMaterialOptions.BlendMode.OneMinusSrcAlpha;
                            materialOptions.cullMode = twoSided ? SurfaceMaterialOptions.CullMode.Off : SurfaceMaterialOptions.CullMode.Back;
                            materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                            materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                            break;
                        case AlphaMode.Additive:
                            materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                            materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.One;
                            if (offscreenTransparent)
                            {
                                materialOptions.alphaSrcBlend = SurfaceMaterialOptions.BlendMode.Zero;

                            }
                            else
                            {
                                materialOptions.alphaSrcBlend = SurfaceMaterialOptions.BlendMode.One;
                            }
                            materialOptions.alphaDstBlend = SurfaceMaterialOptions.BlendMode.One;
                            materialOptions.cullMode = twoSided ? SurfaceMaterialOptions.CullMode.Off : SurfaceMaterialOptions.CullMode.Back;
                            materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                            materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                            break;
                        case AlphaMode.Multiply:
                            materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.DstColor;
                            materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.Zero;
                            materialOptions.cullMode = twoSided ? SurfaceMaterialOptions.CullMode.Off : SurfaceMaterialOptions.CullMode.Back;
                            materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                            materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                            break;
                    }
                    break;
            }

            return materialOptions;
        }
    }
}
