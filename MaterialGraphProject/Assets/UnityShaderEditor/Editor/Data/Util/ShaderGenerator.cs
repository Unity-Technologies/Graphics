using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public static class ShaderGeneratorNames
    {
        private static string[] UV = {"uv0", "uv1", "uv2", "uv3"};
        public static int UVCount = 4;
        
        public const string ScreenPosition = "screenPosition";
        public const string VertexColor = "vertexColor";


        public static string GetUVName(this UVChannel channel)
        {
            return UV[(int) channel];
        }
    }

    public enum UVChannel
    {
        uv0 = 0,
        uv1 = 1,
        uv2 = 2,
        uv3 = 3,
    }

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
                var lines = shaderChunk.chunkString.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
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

        internal static string GetTemplatePath(string templateName)
        {
            var path = new List<string>
            {
                Application.dataPath,
                "UnityShaderEditor",
                "Editor",
                "Templates"
            };

            string result = path[0];
            for (int i = 1; i < path.Count; i++)
                result = Path.Combine(result, path[i]);

            result = Path.Combine(result, templateName);
            return result;
        }

        private const string kErrorString = @"ERROR!";

        public static string AdaptNodeOutput(AbstractMaterialNode node, int outputSlotId, ConcreteSlotValueType convertToType, bool textureSampleUVHack = false)
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
                            return string.Format("({0}{1})", rawOutput, ".xx");
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
                            return string.Format("({0}{1})", rawOutput, ".xxx");
                        case ConcreteSlotValueType.Vector4:
                            return string.Format("({0}.xyz)", rawOutput);
                        default:
                            return kErrorString;
                    }
                case ConcreteSlotValueType.Vector4:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("({0}{1})", rawOutput, ".xxxx");
                        default:
                            return kErrorString;
                    }
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

            // preview is always dimension 4, and we always ignore alpha
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
        
        public static string EmitTransform(string[] matrices, string[] invMatrices, string variable, bool isAffine, bool inverseTranspose)
        {
            if (inverseTranspose)
                matrices = invMatrices;

            if (isAffine)
            {
                variable = string.Format("float4({0},1.0)", variable);
            }

            foreach (var m in matrices)
            {
                var matrix = m;
                if (!isAffine)
                {
                    matrix = "(float3x3)" + matrix;
                }
                variable = inverseTranspose
                    ? string.Format("mul({1},{0})", matrix, variable)
                    : string.Format("mul({0},{1})", matrix, variable);
            }
            return variable;
        }

        public static string EmitTransform(string matrix, string invMatrix, string variable, bool isAffine, bool inverseTranspose)
        {
            return EmitTransform(new[] { matrix }, new[] { invMatrix }, variable, isAffine, inverseTranspose);
        }

        public static string ConvertBetweenSpace(
            string variable, 
            CoordinateSpace from, 
            CoordinateSpace to, 
            InputType inputType, 
            CoordinateSpace tangentMatrixSpace = CoordinateSpace.Object)
        {
            if (from == to)
            {
                // nothing to do
                return variable;
            }

            bool isNormal = false;
            bool affine = (inputType == InputType.Position);
            if (inputType == InputType.Normal)
                isNormal = true;

            if (from == CoordinateSpace.Tangent)
            {
                // if converting from tangent space, reuse the object space code for now
                from = tangentMatrixSpace;
                variable = EmitTransform("tangentSpaceTransform", "transpose(tangentSpaceTransform)", variable, affine, !isNormal);

                if (to == tangentMatrixSpace)
                {
                    return variable;
                }
            }


            switch (from)
            {
                case CoordinateSpace.Object:
                {
                    switch (to)
                    {
                        case CoordinateSpace.View:
                            return EmitTransform(new string[] { "unity_ObjectToWorld", "UNITY_MATRIX_V" }
                                , new string[] { "UNITY_MATRIX_I_V", "unity_WorldToObject" }
                                , variable, affine, isNormal);
                        case CoordinateSpace.World:
                            return EmitTransform("unity_ObjectToWorld", "unity_WorldToObject", variable, affine, isNormal);
                        case CoordinateSpace.Tangent:
                            return EmitTransform("tangentSpaceTransform", "transpose(tangentSpaceTransform)", variable, affine, isNormal);
                        default:
                            throw new ArgumentOutOfRangeException("from", @from, null);
                    }
                }

                case CoordinateSpace.View:
                {
                    switch (to)
                    {
                        case CoordinateSpace.Object:
                            return EmitTransform(new string[] { "UNITY_MATRIX_I_V", "unity_WorldToObject" }
                                , new string[] { "unity_ObjectToWorld", "UNITY_MATRIX_V" }
                                , variable, affine, isNormal);
                        case CoordinateSpace.World:
                            return EmitTransform("UNITY_MATRIX_I_V", "UNITY_MATRIX_V", variable, affine, isNormal);
                        case CoordinateSpace.Tangent:
                            return EmitTransform(new string[] { "UNITY_MATRIX_I_V", "unity_WorldToObject", "tangentSpaceTransform" },
                                new string[] { "transpose(tangentSpaceTransform)", "unity_ObjectToWorld", "UNITY_MATRIX_V" },
                                variable, affine, isNormal);
                        default:
                            throw new ArgumentOutOfRangeException("from", @from, null);
                    }
                }
                case CoordinateSpace.World:
                {
                    switch (to)
                    {
                        case CoordinateSpace.Object:
                            return EmitTransform("unity_WorldToObject", "unity_ObjectToWorld", variable, affine, isNormal);
                        case CoordinateSpace.View:
                            return EmitTransform("UNITY_MATRIX_V", "UNITY_MATRIX_I_V", variable, affine, isNormal);
                        case CoordinateSpace.Tangent:
                            return EmitTransform(new string[] { "unity_WorldToObject", "tangentSpaceTransform" },
                                new string[] { "transpose(tangentSpaceTransform)", "unity_ObjectToWorld" },
                                variable, affine, isNormal);
                        default:
                            throw new ArgumentOutOfRangeException("from", @from, null);
                    }
                }

                default:
                    throw new ArgumentOutOfRangeException("from", @from, null);
            }
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
                vertexShader.AddShaderChunk(string.Format("o.{0} = {1};", name, ConvertBetweenSpace("v.tangent", CoordinateSpace.Object, preferedCoordinateSpace, InputType.Vector)), false);
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
                vertexShader.AddShaderChunk(string.Format("o.{0} = {1};", name, ConvertBetweenSpace("ObjSpaceViewDir(v.vertex)", CoordinateSpace.Object, preferedCoordinateSpace, InputType.Vector)), false);
                pixelShader.AddShaderChunk(string.Format("float3 {0} = normalize(IN.{0});", name), false);
                interpolatorIndex++;
            }

            if (combinedRequierments.requiresPosition > 0)
            {
                var name = preferedCoordinateSpace.ToVariableName(InterpolatorType.Position);
                interpolators.AddShaderChunk(string.Format("float3 {0} : TEXCOORD{1};", name, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = {1};", name, ConvertBetweenSpace("v.vertex", CoordinateSpace.Object, preferedCoordinateSpace, InputType.Vector)), false);
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
                vertexShader.AddShaderChunk(string.Format("o.{0} = color", ShaderGeneratorNames.VertexColor), false);
                pixelShader.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.VertexColor), false);
            }

            if (combinedRequierments.requiresScreenPosition)
            {
                interpolators.AddShaderChunk(string.Format("float4 {0} : TEXCOORD{1};", ShaderGeneratorNames.ScreenPosition, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = ComputeScreenPos(UnityObjectToClipPos(v.vertex));", ShaderGeneratorNames.ScreenPosition), false);
                pixelShader.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.ScreenPosition), false);
                interpolatorIndex++;
            }

            foreach (var channel in combinedRequierments.requiresMeshUVs.Distinct())
            {
                interpolators.AddShaderChunk(string.Format("half4 {0} : TEXCOORD{1};", channel.GetUVName(), interpolatorIndex == 0 ? "" : interpolatorIndex.ToString()), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = v.texcoord{1};", channel.GetUVName(), (int)channel), false);
                pixelShader.AddShaderChunk(string.Format("float4 {0}  = IN.{0};", channel.GetUVName()), false);
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

            foreach (var channel in combinedRequierments.requiresMeshUVs.Distinct())
                surfaceInputs.AddShaderChunk(string.Format("surfaceInput.{0}  ={0};", channel.GetUVName()), false);
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

        public static void GenerateCopyToSurfaceInputs(
            NeededCoordinateSpace neededSpaces,
            ShaderGenerator pixelShader,
            string objectSpaceName,
            string viewSpaceName,
            string worldSpaceName,
            string tangentSpaceName)
        {
            if ((neededSpaces & NeededCoordinateSpace.Object) > 0)
                pixelShader.AddShaderChunk(string.Format("surfaceInput.{0} = {0};", objectSpaceName), false);

            if ((neededSpaces & NeededCoordinateSpace.World) > 0)
                pixelShader.AddShaderChunk(string.Format("surfaceInput.{0} = {0};", worldSpaceName), false);

            if ((neededSpaces & NeededCoordinateSpace.View) > 0)
                pixelShader.AddShaderChunk(string.Format("surfaceInput.{0} = {0};", viewSpaceName), false);

            if ((neededSpaces & NeededCoordinateSpace.Tangent) > 0)
                pixelShader.AddShaderChunk(string.Format("surfaceInput.{0} = {0}", tangentSpaceName), false);
        }

        public static string GetPreviewSubShader(AbstractMaterialNode node, ShaderGraphRequirements shaderGraphRequirements)
        {
            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, node);

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
            var outputSlot = node.GetOutputSlots<MaterialSlot>().FirstOrDefault();
            if (outputSlot != null)
            {
                var result = string.Format("surf.{0}", node.GetVariableNameForSlot(outputSlot.id));
                outputs.AddShaderChunk(string.Format("return {0};", AdaptNodeOutputForPreview(node, outputSlot.id, result)), true);
            }
            else
                outputs.AddShaderChunk("return 0;", true);

            var res = subShaderTemplate.Replace("${Interpolators}", interpolators.GetShaderString(0));
            res = res.Replace("${VertexShader}", vertexShader.GetShaderString(0));
            res = res.Replace("${LocalPixelShader}", pixelShader.GetShaderString(0));
            res = res.Replace("${SurfaceInputs}", surfaceInputs.GetShaderString(0));
            res = res.Replace("${SurfaceOutputRemap}", outputs.GetShaderString(0));
            return res;
        }

        private const string subShaderTemplate = @"
SubShader
{
    Tags { ""RenderType""=""Opaque"" }
    LOD 100

    Pass
    {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag

        #include ""UnityCG.cginc""

        struct GraphVertexOutput
        {
            float4 position : POSITION;
            ${Interpolators}
        };

        GraphVertexOutput vert (GraphVertexInput v)
        {
            v = PopulateVertexData(v);

            GraphVertexOutput o;
            o.position = UnityObjectToClipPos(v.vertex);
            ${VertexShader}
            return o;
        }

        fixed4 frag (GraphVertexOutput IN) : SV_Target
        {
            ${LocalPixelShader}

            SurfaceInputs surfaceInput = (SurfaceInputs)0;;
            ${SurfaceInputs}

            SurfaceDescription surf = PopulateSurfaceData(surfaceInput);
            ${SurfaceOutputRemap}
        }
        ENDCG
    }
}";
    }
}
