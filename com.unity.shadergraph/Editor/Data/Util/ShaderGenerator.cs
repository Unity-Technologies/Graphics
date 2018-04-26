using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public class ShaderGenerator
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

        public void AddShaderChunk(string s, bool unique)
        {
            if (string.IsNullOrEmpty(s))
                return;

            if (unique && m_ShaderChunks.Any(x => x.chunkString == s))
                return;

            m_ShaderChunks.Add(new ShaderChunk(m_IndentLevel, s));
        }

        public void Indent()
        {
            m_IndentLevel++;
        }

        public void Deindent()
        {
            m_IndentLevel = Math.Max(0, m_IndentLevel - 1);
        }

        public string GetShaderString(int baseIndentLevel)
        {
            var sb = new StringBuilder();
            foreach (var shaderChunk in m_ShaderChunks)
            {
                var lines = Regex.Split(shaderChunk.chunkString, Environment.NewLine);
                for (int index = 0; index < lines.Length; index++)
                {
                    var line = lines[index];
                    for (var i = 0; i < shaderChunk.chunkIndentLevel + baseIndentLevel; i++)
                        sb.Append("\t");

                    sb.AppendLine(line);
                }
            }
            return sb.ToString();
        }

        public static string GetTemplatePath(string templateName)
        {
            var path = new List<string>
            {
                DefaultShaderIncludes.GetAssetsPackagePath() ?? Path.GetFullPath("Packages/com.unity.shadergraph"),
                "Editor",
                "Templates"
            };

            string result = path[0];
            for (int i = 1; i < path.Count; i++)
                result = Path.Combine(result, path[i]);

            result = Path.Combine(result, templateName);

            if (File.Exists(result))
                return result;

            return string.Empty;
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
                            return string.Format("({0}3({1}, 0.0))", node.precision, rawOutput);
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
                            return string.Format("({0}4({1}, 0.0, 1.0))", node.precision, rawOutput);
                        case ConcreteSlotValueType.Vector3:
                            return string.Format("({0}4({1}, 1.0))", node.precision, rawOutput);
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
                default:
                    return kErrorString;
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
            var distance = new int[4];
            var prev = new CoordinateSpace ? [4];
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
                m_transforms = new TransformDesc[4, 4][];
                m_transforms[(int)CoordinateSpace.Object, (int)CoordinateSpace.Object] = new TransformDesc[] {};
                m_transforms[(int)CoordinateSpace.View, (int)CoordinateSpace.View] = new TransformDesc[] {};
                m_transforms[(int)CoordinateSpace.World, (int)CoordinateSpace.World] = new TransformDesc[] {};
                m_transforms[(int)CoordinateSpace.Tangent, (int)CoordinateSpace.Tangent] = new TransformDesc[] {};
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
            ShaderGenerator surfaceInputs,
            string toReplace = "float3 {0};")
        {
            if ((neededSpaces & NeededCoordinateSpace.Object) > 0)
                surfaceInputs.AddShaderChunk(string.Format(toReplace, CoordinateSpace.Object.ToVariableName(interpolatorType)), false);

            if ((neededSpaces & NeededCoordinateSpace.World) > 0)
                surfaceInputs.AddShaderChunk(string.Format(toReplace, CoordinateSpace.World.ToVariableName(interpolatorType)), false);

            if ((neededSpaces & NeededCoordinateSpace.View) > 0)
                surfaceInputs.AddShaderChunk(string.Format(toReplace, CoordinateSpace.View.ToVariableName(interpolatorType)), false);

            if ((neededSpaces & NeededCoordinateSpace.Tangent) > 0)
                surfaceInputs.AddShaderChunk(string.Format(toReplace, CoordinateSpace.Tangent.ToVariableName(interpolatorType)), false);
        }

        public static void GenerateStandardTransforms(
            int interpolatorStartIndex,
            int maxInterpolators,
            ShaderGenerator interpolators,
            ShaderGenerator vertexShader,
            ShaderGenerator pixelShader,
            ShaderGenerator surfaceInputs,
            ShaderGraphRequirements graphRequiements,
            ShaderGraphRequirements modelRequiements,
            CoordinateSpace preferedCoordinateSpace)
        {
            if (preferedCoordinateSpace == CoordinateSpace.Tangent)
                preferedCoordinateSpace = CoordinateSpace.World;

            // step 1:
            // *generate needed interpolators
            // *generate output from the vertex shader that writes into these interpolators
            // *generate the pixel shader code that declares needed variables in the local scope
            var combinedRequierments = graphRequiements.Union(modelRequiements);

            int interpolatorIndex = interpolatorStartIndex;

            // bitangent needs normal for x product
            if (combinedRequierments.requiresNormal > 0 || combinedRequierments.requiresBitangent > 0)
            {
                var name = preferedCoordinateSpace.ToVariableName(InterpolatorType.Normal);
                interpolators.AddShaderChunk(string.Format("float3 {0} : TEXCOORD{1};", name, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = {1};", name, ConvertBetweenSpace("v.normal", CoordinateSpace.Object, preferedCoordinateSpace, InputType.Normal)), false);
                pixelShader.AddShaderChunk(string.Format("float3 {0} = normalize(IN.{0});", name), false);
                interpolatorIndex++;
            }

            if (combinedRequierments.requiresTangent > 0 || combinedRequierments.requiresBitangent > 0)
            {
                var name = preferedCoordinateSpace.ToVariableName(InterpolatorType.Tangent);
                interpolators.AddShaderChunk(string.Format("float3 {0} : TEXCOORD{1};", name, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = {1};", name, ConvertBetweenSpace("v.tangent.xyz", CoordinateSpace.Object, preferedCoordinateSpace, InputType.Vector)), false);
                pixelShader.AddShaderChunk(string.Format("float3 {0} = IN.{0};", name), false);
                interpolatorIndex++;
            }

            if (combinedRequierments.requiresBitangent > 0)
            {
                var name = preferedCoordinateSpace.ToVariableName(InterpolatorType.BiTangent);
                interpolators.AddShaderChunk(string.Format("float3 {0} : TEXCOORD{1};", name, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = normalize(cross(o.{1}, o.{2}.xyz) * {3});",
                        name,
                        preferedCoordinateSpace.ToVariableName(InterpolatorType.Normal),
                        preferedCoordinateSpace.ToVariableName(InterpolatorType.Tangent),
                        "v.tangent.w"), false);
                pixelShader.AddShaderChunk(string.Format("float3 {0} = IN.{0};", name), false);
                interpolatorIndex++;
            }

            if (combinedRequierments.requiresViewDir > 0)
            {
                var name = preferedCoordinateSpace.ToVariableName(InterpolatorType.ViewDirection);
                interpolators.AddShaderChunk(string.Format("float3 {0} : TEXCOORD{1};", name, interpolatorIndex), false);

                var worldSpaceViewDir = "SafeNormalize(_WorldSpaceCameraPos.xyz - mul(GetObjectToWorldMatrix(), float4(v.vertex.xyz, 1.0)).xyz)";
                vertexShader.AddShaderChunk(string.Format("o.{0} = {1};", name, ConvertBetweenSpace(worldSpaceViewDir, CoordinateSpace.World, preferedCoordinateSpace, InputType.Vector)), false);
                pixelShader.AddShaderChunk(string.Format("float3 {0} = normalize(IN.{0});", name), false);
                interpolatorIndex++;
            }

            if (combinedRequierments.requiresPosition > 0)
            {
                var name = preferedCoordinateSpace.ToVariableName(InterpolatorType.Position);
                interpolators.AddShaderChunk(string.Format("float3 {0} : TEXCOORD{1};", name, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = {1}.xyz;", name, ConvertBetweenSpace("v.vertex", CoordinateSpace.Object, preferedCoordinateSpace, InputType.Position)), false);
                pixelShader.AddShaderChunk(string.Format("float3 {0} = IN.{0};", name), false);
                interpolatorIndex++;
            }

            if (combinedRequierments.NeedsTangentSpace())
            {
                pixelShader.AddShaderChunk(string.Format("float3x3 tangentSpaceTransform = float3x3({0},{1},{2});",
                        preferedCoordinateSpace.ToVariableName(InterpolatorType.Tangent), preferedCoordinateSpace.ToVariableName(InterpolatorType.BiTangent), preferedCoordinateSpace.ToVariableName(InterpolatorType.Normal)), false);
            }

            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresNormal, InterpolatorType.Normal, preferedCoordinateSpace,
                InputType.Normal, pixelShader, Dimension.Three);
            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresTangent, InterpolatorType.Tangent, preferedCoordinateSpace,
                InputType.Vector, pixelShader, Dimension.Three);
            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresBitangent, InterpolatorType.BiTangent, preferedCoordinateSpace,
                InputType.Vector, pixelShader, Dimension.Three);

            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresViewDir, InterpolatorType.ViewDirection, preferedCoordinateSpace,
                InputType.Vector, pixelShader, Dimension.Three);
            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresPosition, InterpolatorType.Position, preferedCoordinateSpace,
                InputType.Position, pixelShader, Dimension.Three);

            if (combinedRequierments.requiresVertexColor)
            {
                interpolators.AddShaderChunk(string.Format("float4 {0} : COLOR;", ShaderGeneratorNames.VertexColor), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = v.color;", ShaderGeneratorNames.VertexColor), false);
                pixelShader.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.VertexColor), false);
            }

            if (combinedRequierments.requiresScreenPosition)
            {
                interpolators.AddShaderChunk(string.Format("float4 {0} : TEXCOORD{1};", ShaderGeneratorNames.ScreenPosition, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = ComputeScreenPos(mul(GetWorldToHClipMatrix(), mul(GetObjectToWorldMatrix(), v.vertex)), _ProjectionParams.x);", ShaderGeneratorNames.ScreenPosition), false);
                pixelShader.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.ScreenPosition), false);
                interpolatorIndex++;
            }

            foreach (var channel in combinedRequierments.requiresMeshUVs.Distinct())
            {
                interpolators.AddShaderChunk(string.Format("half4 {0} : TEXCOORD{1};", channel.GetUVName(), interpolatorIndex == 0 ? "" : interpolatorIndex.ToString()), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = v.texcoord{1};", channel.GetUVName(), (int)channel), false);
                pixelShader.AddShaderChunk(string.Format("float4 {0} = IN.{0};", channel.GetUVName()), false);
                interpolatorIndex++;
            }

            // step 2
            // copy the locally defined values into the surface description
            // structure using the requirements for ONLY the shader graph
            // additional requirements have come from the lighting model
            // and are not needed in the shader graph
            var replaceString = "surfaceInput.{0} = {0};";
            GenerateSpaceTranslationSurfaceInputs(graphRequiements.requiresNormal, InterpolatorType.Normal, surfaceInputs, replaceString);
            GenerateSpaceTranslationSurfaceInputs(graphRequiements.requiresTangent, InterpolatorType.Tangent, surfaceInputs, replaceString);
            GenerateSpaceTranslationSurfaceInputs(graphRequiements.requiresBitangent, InterpolatorType.BiTangent, surfaceInputs, replaceString);
            GenerateSpaceTranslationSurfaceInputs(graphRequiements.requiresViewDir, InterpolatorType.ViewDirection, surfaceInputs, replaceString);
            GenerateSpaceTranslationSurfaceInputs(graphRequiements.requiresPosition, InterpolatorType.Position, surfaceInputs, replaceString);

            if (graphRequiements.requiresVertexColor)
                surfaceInputs.AddShaderChunk(string.Format("surfaceInput.{0} = {0};", ShaderGeneratorNames.VertexColor), false);

            if (graphRequiements.requiresScreenPosition)
                surfaceInputs.AddShaderChunk(string.Format("surfaceInput.{0} = {0};", ShaderGeneratorNames.ScreenPosition), false);

            foreach (var channel in graphRequiements.requiresMeshUVs.Distinct())
                surfaceInputs.AddShaderChunk(string.Format("surfaceInput.{0} = {0};", channel.GetUVName()), false);
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

        public static void GenerateSpaceTranslationPixelShader(
            NeededCoordinateSpace neededSpaces,
            InterpolatorType type,
            CoordinateSpace from,
            InputType inputType,
            ShaderGenerator pixelShader,
            Dimension dimension)
        {
            if ((neededSpaces & NeededCoordinateSpace.Object) > 0 && from != CoordinateSpace.Object)
                pixelShader.AddShaderChunk(
                    string.Format("float{0} {1} = {2};", DimensionToString(dimension),
                        CoordinateSpace.Object.ToVariableName(type), ConvertBetweenSpace(from.ToVariableName(type), from, CoordinateSpace.Object, inputType, from)), false);

            if ((neededSpaces & NeededCoordinateSpace.World) > 0 && from != CoordinateSpace.World)
                pixelShader.AddShaderChunk(
                    string.Format("float{0} {1} = {2};", DimensionToString(dimension),
                        CoordinateSpace.World.ToVariableName(type), ConvertBetweenSpace(from.ToVariableName(type), from, CoordinateSpace.World, inputType, from)), false);

            if ((neededSpaces & NeededCoordinateSpace.View) > 0 && from != CoordinateSpace.View)
                pixelShader.AddShaderChunk(
                    string.Format("float{0} {1} = {2};", DimensionToString(dimension),
                        CoordinateSpace.View.ToVariableName(type),
                        ConvertBetweenSpace(from.ToVariableName(type), from, CoordinateSpace.View, inputType, from)), false);

            if ((neededSpaces & NeededCoordinateSpace.Tangent) > 0 && from != CoordinateSpace.Tangent)
                pixelShader.AddShaderChunk(
                    string.Format("float{0} {1} = {2};", DimensionToString(dimension),
                        CoordinateSpace.Tangent.ToVariableName(type),
                        ConvertBetweenSpace(from.ToVariableName(type), from, CoordinateSpace.Tangent, inputType, from)), false);
        }

        public static string GetPreviewSubShader(AbstractMaterialNode node, ShaderGraphRequirements shaderGraphRequirements)
        {
            var interpolators = new ShaderGenerator();
            var vertexShader = new ShaderGenerator();
            var pixelShader = new ShaderGenerator();
            var surfaceInputs = new ShaderGenerator();

            ShaderGenerator.GenerateStandardTransforms(
                0,
                16,
                interpolators,
                vertexShader,
                pixelShader,
                surfaceInputs,
                shaderGraphRequirements,
                ShaderGraphRequirements.none,
                CoordinateSpace.World);

            var outputs = new ShaderGenerator();
            if (node != null)
            {
                var outputSlot = node.GetOutputSlots<MaterialSlot>().FirstOrDefault();
                if (outputSlot != null)
                {
                    var result = string.Format("surf.{0}", node.GetVariableNameForSlot(outputSlot.id));
                    outputs.AddShaderChunk(string.Format("return {0};", AdaptNodeOutputForPreview(node, outputSlot.id, result)), true);
                }
                else
                    outputs.AddShaderChunk("return 0;", true);
            }
            else
            {
                outputs.AddShaderChunk("return surf.PreviewOutput;", false);
            }

            var res = subShaderTemplate.Replace("${Interpolators}", interpolators.GetShaderString(0));
            res = res.Replace("${VertexShader}", vertexShader.GetShaderString(0));
            res = res.Replace("${LocalPixelShader}", pixelShader.GetShaderString(0));
            res = res.Replace("${SurfaceInputs}", surfaceInputs.GetShaderString(0));
            res = res.Replace("${SurfaceOutputRemap}", outputs.GetShaderString(0));
            return res;
        }

        public static SurfaceMaterialOptions GetMaterialOptions(SurfaceType surfaceType, AlphaMode alphaMode, bool twoSided)
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
                    materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Geometry;
                    materialOptions.renderType = SurfaceMaterialOptions.RenderType.Opaque;
                    break;
                case SurfaceType.Transparent:
                    switch (alphaMode)
                    {
                        case AlphaMode.Alpha:
                            materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.SrcAlpha;
                            materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.OneMinusSrcAlpha;
                            materialOptions.cullMode = twoSided ? SurfaceMaterialOptions.CullMode.Off : SurfaceMaterialOptions.CullMode.Back;
                            materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                            materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                            materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Transparent;
                            materialOptions.renderType = SurfaceMaterialOptions.RenderType.Transparent;
                            break;
                        case AlphaMode.Premultiply:
                            materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                            materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.OneMinusSrcAlpha;
                            materialOptions.cullMode = twoSided ? SurfaceMaterialOptions.CullMode.Off : SurfaceMaterialOptions.CullMode.Back;
                            materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                            materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                            materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Transparent;
                            materialOptions.renderType = SurfaceMaterialOptions.RenderType.Transparent;
                            break;
                        case AlphaMode.Additive:
                            materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.One;
                            materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.One;
                            materialOptions.cullMode = twoSided ? SurfaceMaterialOptions.CullMode.Off : SurfaceMaterialOptions.CullMode.Back;
                            materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                            materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                            materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Transparent;
                            materialOptions.renderType = SurfaceMaterialOptions.RenderType.Transparent;
                            break;
                        case AlphaMode.Multiply:
                            materialOptions.srcBlend = SurfaceMaterialOptions.BlendMode.DstColor;
                            materialOptions.dstBlend = SurfaceMaterialOptions.BlendMode.Zero;
                            materialOptions.cullMode = twoSided ? SurfaceMaterialOptions.CullMode.Off : SurfaceMaterialOptions.CullMode.Back;
                            materialOptions.zTest = SurfaceMaterialOptions.ZTest.LEqual;
                            materialOptions.zWrite = SurfaceMaterialOptions.ZWrite.Off;
                            materialOptions.renderQueue = SurfaceMaterialOptions.RenderQueue.Transparent;
                            materialOptions.renderType = SurfaceMaterialOptions.RenderType.Transparent;
                            break;
                    }
                    break;
            }

            return materialOptions;
        }

        private const string subShaderTemplate = @"
SubShader
{
    Tags { ""RenderType""=""Opaque"" }
    LOD 100

    Pass
    {
        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag

        struct GraphVertexOutput
        {
            float4 position : POSITION;
            ${Interpolators}
        };

        GraphVertexOutput vert (GraphVertexInput v)
        {
            v = PopulateVertexData(v);

            GraphVertexOutput o;
            float3 positionWS = TransformObjectToWorld(v.vertex);
            o.position = TransformWorldToHClip(positionWS);
            ${VertexShader}
            return o;
        }

        float4 frag (GraphVertexOutput IN) : SV_Target
        {
            ${LocalPixelShader}

            SurfaceInputs surfaceInput = (SurfaceInputs)0;;
            ${SurfaceInputs}

            SurfaceDescription surf = PopulateSurfaceData(surfaceInput);
            ${SurfaceOutputRemap}
        }
        ENDHLSL
    }
}";
    }


}
