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

        public const string ObjectSpaceNormal = "objectSpaceNormal";
        public const string ViewSpaceNormal = "viewSpaceNormal";
        public const string WorldSpaceNormal = "worldSpaceNormal";
        public const string TangentSpaceNormal = "tangentSpaceNormal";

        public const string ObjectSpaceBiTangent = "objectSpaceBiTangent";
        public const string ViewSpaceBiTangent = "viewSpaceBiTangent";
        public const string WorldSpaceBiTangent = "worldSpaceBiTangent";
        public const string TangentSpaceBiTangent = "TangentSpaceBitangent";

        public const string ObjectSpaceTangent = "objectSpaceTangent";
        public const string ViewSpaceTangent = "viewSpaceTangent";
        public const string WorldSpaceTangent = "worldSpaceTangent";
        public const string TangentSpaceTangent = "tangentSpaceTangent";

        public const string ObjectSpaceViewDirection = "objectSpaceViewDirection";
        public const string ViewSpaceViewDirection = "viewSpaceViewDirection";
        public const string WorldSpaceViewDirection = "worldSpaceViewDirection";
        public const string TangentSpaceViewDirection = "tangentSpaceViewDirection";

        public const string ObjectSpacePosition = "objectSpacePosition";
        public const string ViewSpacePosition = "viewSpaceVPosition";
        public const string WorldSpacePosition = "worldSpacePosition";
        public const string TangentSpacePosition = "tangentSpacePosition";

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

        public static void GenerateStandardTransforms(
            int interpolatorStartIndex,
            ShaderGenerator interpolators,
            ShaderGenerator vertexShader,
            ShaderGenerator pixelShader,
            ShaderGenerator surfaceInputs,
            ShaderGraphRequirements externalGraphRequiements,
            ShaderGraphRequirements modelRequiements)
        {
            // step 1:
            // *generate needed interpolators
            // *generate output from the vertex shader that writes into these interpolators
            // *generate the pixel shader code that declares needed variables in the local scope

            var combinedRequierments = externalGraphRequiements.Union(modelRequiements);

            // bitangent needs normal for x product
            if (combinedRequierments.requiresNormal > 0 || combinedRequierments.requiresBitangent > 0)
            {
                interpolators.AddShaderChunk(string.Format("float3 {0} : NORMAL;", ShaderGeneratorNames.ObjectSpaceNormal), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = v.normal;", ShaderGeneratorNames.ObjectSpaceNormal), false);
                pixelShader.AddShaderChunk(string.Format("float3 {0} = normalize(IN.{0});", ShaderGeneratorNames.ObjectSpaceNormal), false);
            }

            if (combinedRequierments.requiresTangent > 0 || combinedRequierments.requiresBitangent > 0)
            {
                interpolators.AddShaderChunk(string.Format("float4 {0} : TANGENT;", ShaderGeneratorNames.ObjectSpaceTangent), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = v.tangent;", ShaderGeneratorNames.ObjectSpaceTangent), false);
                pixelShader.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.ObjectSpaceTangent), false);
                pixelShader.AddShaderChunk(string.Format("float3 {0} = normalize(cross(normalize(IN.{1}), normalize(IN.{2}.xyz)) * IN.{2}.w);",
                    ShaderGeneratorNames.ObjectSpaceBiTangent,
                    ShaderGeneratorNames.ObjectSpaceNormal,
                    ShaderGeneratorNames.ObjectSpaceTangent), false);
            }

            int interpolatorIndex = interpolatorStartIndex;
            if (combinedRequierments.requiresViewDir > 0)
            {
                interpolators.AddShaderChunk(string.Format("float3 {0} : TEXCOORD{1};", ShaderGeneratorNames.ObjectSpaceViewDirection, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = ObjSpaceViewDir(v.vertex);", ShaderGeneratorNames.ObjectSpaceViewDirection), false);
                pixelShader.AddShaderChunk(string.Format("float3 {0} = normalize(IN.{0});", ShaderGeneratorNames.ObjectSpaceViewDirection), false);
                interpolatorIndex++;
            }

            if (combinedRequierments.requiresPosition > 0)
            {
                interpolators.AddShaderChunk(string.Format("float4 {0} : TEXCOORD{1};", ShaderGeneratorNames.ObjectSpacePosition, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = v.vertex;", ShaderGeneratorNames.ObjectSpacePosition), false);
                pixelShader.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.ObjectSpacePosition), false);
                interpolatorIndex++;
            }

            if (combinedRequierments.NeedsTangentSpace())
            {
                pixelShader.AddShaderChunk(string.Format("float3x3 tangentSpaceTransform = float3x3({0},{1},{2});",
                    ShaderGeneratorNames.ObjectSpaceTangent, ShaderGeneratorNames.ObjectSpaceBiTangent, ShaderGeneratorNames.ObjectSpaceNormal), false);
            }

            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresNormal, pixelShader,
                ShaderGeneratorNames.ObjectSpaceNormal, ShaderGeneratorNames.ViewSpaceNormal,
                ShaderGeneratorNames.WorldSpaceNormal, ShaderGeneratorNames.TangentSpaceNormal, Dimension.Three, true);

            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresTangent, pixelShader,
                ShaderGeneratorNames.ObjectSpaceTangent, ShaderGeneratorNames.ViewSpaceTangent,
                ShaderGeneratorNames.WorldSpaceTangent, ShaderGeneratorNames.TangentSpaceTangent, Dimension.Three);

            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresBitangent, pixelShader,
                ShaderGeneratorNames.ObjectSpaceBiTangent, ShaderGeneratorNames.ViewSpaceBiTangent,
                ShaderGeneratorNames.WorldSpaceBiTangent, ShaderGeneratorNames.TangentSpaceBiTangent, Dimension.Three);

            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresViewDir, pixelShader,
                ShaderGeneratorNames.ObjectSpaceViewDirection, ShaderGeneratorNames.ViewSpaceViewDirection,
                ShaderGeneratorNames.WorldSpaceViewDirection, ShaderGeneratorNames.TangentSpaceViewDirection, Dimension.Three);

            ShaderGenerator.GenerateSpaceTranslationPixelShader(combinedRequierments.requiresPosition, pixelShader,
                ShaderGeneratorNames.ObjectSpacePosition, ShaderGeneratorNames.ViewSpacePosition,
                ShaderGeneratorNames.WorldSpacePosition, ShaderGeneratorNames.TangentSpacePosition, Dimension.Three);

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
            ShaderGenerator.GenerateCopyToSurfaceInputs(externalGraphRequiements.requiresNormal, surfaceInputs,
                ShaderGeneratorNames.ObjectSpaceNormal, ShaderGeneratorNames.ViewSpaceNormal,
                ShaderGeneratorNames.WorldSpaceNormal, ShaderGeneratorNames.TangentSpaceNormal);

            ShaderGenerator.GenerateCopyToSurfaceInputs(externalGraphRequiements.requiresTangent, surfaceInputs,
                ShaderGeneratorNames.ObjectSpaceTangent, ShaderGeneratorNames.ViewSpaceTangent,
                ShaderGeneratorNames.WorldSpaceTangent, ShaderGeneratorNames.TangentSpaceTangent);

            ShaderGenerator.GenerateCopyToSurfaceInputs(externalGraphRequiements.requiresBitangent, surfaceInputs,
                ShaderGeneratorNames.ObjectSpaceBiTangent, ShaderGeneratorNames.ViewSpaceBiTangent,
                ShaderGeneratorNames.WorldSpaceBiTangent, ShaderGeneratorNames.TangentSpaceBiTangent);

            ShaderGenerator.GenerateCopyToSurfaceInputs(externalGraphRequiements.requiresViewDir, surfaceInputs,
                ShaderGeneratorNames.ObjectSpaceViewDirection, ShaderGeneratorNames.ViewSpaceViewDirection,
                ShaderGeneratorNames.WorldSpaceViewDirection, ShaderGeneratorNames.TangentSpaceViewDirection);

            ShaderGenerator.GenerateCopyToSurfaceInputs(externalGraphRequiements.requiresPosition, surfaceInputs,
                ShaderGeneratorNames.ObjectSpacePosition, ShaderGeneratorNames.ViewSpacePosition,
                ShaderGeneratorNames.WorldSpacePosition, ShaderGeneratorNames.TangentSpacePosition);

            if (externalGraphRequiements.requiresVertexColor)
                surfaceInputs.AddShaderChunk(string.Format("surfaceInput.{0} = {0};", ShaderGeneratorNames.VertexColor), false);

            if (externalGraphRequiements.requiresScreenPosition)
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
            ShaderGenerator pixelShader,
            string objectSpaceName,
            string viewSpaceName,
            string worldSpaceName,
            string tangentSpaceName,
            Dimension dimension,
            bool isNormal = false)
        {
            if ((neededSpaces & NeededCoordinateSpace.World) > 0)
            {
                if (isNormal)
                    pixelShader.AddShaderChunk(string.Format("float{0} {1} = UnityObjectToWorldNormal({2});", DimensionToString(dimension), worldSpaceName, objectSpaceName), false);
                else
                    pixelShader.AddShaderChunk(string.Format("float{0} {1} = UnityObjectToWorldDir({2});", DimensionToString(dimension), worldSpaceName, objectSpaceName), false);
            }

            if ((neededSpaces & NeededCoordinateSpace.View) > 0)
            {
                pixelShader.AddShaderChunk(string.Format("float{0} {1} = UnityObjectToViewPos({2});", DimensionToString(dimension), viewSpaceName, objectSpaceName), false);
            }

            if ((neededSpaces & NeededCoordinateSpace.Tangent) > 0)
            {
                pixelShader.AddShaderChunk(string.Format("float{0} {1} = mul(tangentSpaceTransform, {2})", DimensionToString(dimension), tangentSpaceName, objectSpaceName), false);
            }
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
                interpolators,
                vertexShader,
                pixelShader,
                surfaceInputs,
                shaderGraphRequirements,
                ShaderGraphRequirements.none);

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
