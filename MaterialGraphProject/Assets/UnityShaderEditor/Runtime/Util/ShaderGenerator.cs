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

        public static void GenerateSpaceTranslationPixelShader(
            NeededCoordinateSpace neededSpaces,
            ShaderGenerator pixelShader,
            string objectSpaceName,
            string viewSpaceName,
            string worldSpaceName,
            string tangentSpaceName,
            bool isNormal = false)
        {
            if ((neededSpaces & NeededCoordinateSpace.Object) > 0)
            {
                pixelShader.AddShaderChunk(string.Format("surfaceInput.{0} = {0};", objectSpaceName), false);
            }

            if ((neededSpaces & NeededCoordinateSpace.World) > 0)
            {
                if (isNormal)
                    pixelShader.AddShaderChunk(string.Format("surfaceInput.{0} = UnityObjectToWorldNormal({1});", worldSpaceName, objectSpaceName), false);
                else
                    pixelShader.AddShaderChunk(string.Format("surfaceInput.{0} = UnityObjectToWorldDir({1});", worldSpaceName, objectSpaceName), false);
            }

            if ((neededSpaces & NeededCoordinateSpace.View) > 0)
            {
                pixelShader.AddShaderChunk(string.Format("surfaceInput.{0} = UnityObjectToViewPos({1});", viewSpaceName, objectSpaceName), false);
            }

            if ((neededSpaces & NeededCoordinateSpace.Tangent) > 0)
            {
                pixelShader.AddShaderChunk(string.Format("surfaceInput.{0} = mul(tangentSpaceTransform, {1})", tangentSpaceName, objectSpaceName), false);
            }
        }

        public static string GetPreviewSubShader(AbstractMaterialNode node, ShaderGraphRequirements shaderGraphRequirements)
        {
            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, node);

            var interpolators = new ShaderGenerator();
            var vertexShader = new ShaderGenerator();
            var pixelShader = new ShaderGenerator();

            // bitangent needs normal for x product
            if (shaderGraphRequirements.requiresNormal > 0 || shaderGraphRequirements.requiresBitangent > 0)
            {
                interpolators.AddShaderChunk(string.Format("float3 {0} : NORMAL;", ShaderGeneratorNames.ObjectSpaceNormal), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = v.normal;", ShaderGeneratorNames.ObjectSpaceNormal), false);
                pixelShader.AddShaderChunk(string.Format("float3 {0} = normalize(IN.{0});", ShaderGeneratorNames.ObjectSpaceNormal), false);
            }

            if (shaderGraphRequirements.requiresTangent > 0 || shaderGraphRequirements.requiresBitangent > 0)
            {
                interpolators.AddShaderChunk(string.Format("float4 {0} : TANGENT;", ShaderGeneratorNames.ObjectSpaceTangent), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = v.tangent;", ShaderGeneratorNames.ObjectSpaceTangent), false);
                pixelShader.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.ObjectSpaceTangent), false);
                pixelShader.AddShaderChunk(string.Format("float4 {0} = normalize(cross(normalize(IN.{1}), normalize(IN.{2}.xyz)) * IN.{2}.w);",
                    ShaderGeneratorNames.ObjectSpaceBiTangent,
                    ShaderGeneratorNames.ObjectSpaceTangent,
                    ShaderGeneratorNames.ObjectSpaceNormal), false);
            }

            int interpolatorIndex = 0;
            if (shaderGraphRequirements.requiresViewDir > 0)
            {
                interpolators.AddShaderChunk(string.Format("float3 {0} : TEXCOORD{1};", ShaderGeneratorNames.ObjectSpaceViewDirection, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = ObjSpaceViewDir(v.vertex);", ShaderGeneratorNames.ObjectSpaceViewDirection), false);
                pixelShader.AddShaderChunk(string.Format("float3 {0} = normalize(IN.{0});", ShaderGeneratorNames.ObjectSpaceViewDirection), false);
                interpolatorIndex++;
            }

            if (shaderGraphRequirements.requiresPosition > 0)
            {
                interpolators.AddShaderChunk(string.Format("float4 {0} : TEXCOORD{1};", ShaderGeneratorNames.ObjectSpacePosition, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = v.vertex;", ShaderGeneratorNames.ObjectSpacePosition), false);
                pixelShader.AddShaderChunk(string.Format("float4 {0} = IN.{0};", ShaderGeneratorNames.ObjectSpacePosition), false);
                interpolatorIndex++;
            }

            if (shaderGraphRequirements.NeedsTangentSpace())
            {
                pixelShader.AddShaderChunk(string.Format("float3x3 tangentSpaceTransform = float3x3({0},{1},{2});",
                    ShaderGeneratorNames.ObjectSpaceTangent, ShaderGeneratorNames.ObjectSpaceBiTangent, ShaderGeneratorNames.ObjectSpaceNormal), false);
            }

            GenerateSpaceTranslationPixelShader(shaderGraphRequirements.requiresNormal, pixelShader,
                ShaderGeneratorNames.ObjectSpaceNormal, ShaderGeneratorNames.ViewSpaceNormal,
                ShaderGeneratorNames.WorldSpaceNormal, ShaderGeneratorNames.TangentSpaceNormal);

            GenerateSpaceTranslationPixelShader(shaderGraphRequirements.requiresTangent, pixelShader,
                ShaderGeneratorNames.ObjectSpaceTangent, ShaderGeneratorNames.ViewSpaceTangent,
                ShaderGeneratorNames.WorldSpaceTangent, ShaderGeneratorNames.TangentSpaceTangent);

            GenerateSpaceTranslationPixelShader(shaderGraphRequirements.requiresBitangent, pixelShader,
                ShaderGeneratorNames.ObjectSpaceBiTangent, ShaderGeneratorNames.ViewSpaceBiTangent,
                ShaderGeneratorNames.WorldSpaceBiTangent, ShaderGeneratorNames.TangentSpaceBiTangent);

            GenerateSpaceTranslationPixelShader(shaderGraphRequirements.requiresViewDir, pixelShader,
                ShaderGeneratorNames.ObjectSpaceViewDirection, ShaderGeneratorNames.ViewSpaceViewDirection,
                ShaderGeneratorNames.WorldSpaceViewDirection, ShaderGeneratorNames.TangentSpaceViewDirection);

            GenerateSpaceTranslationPixelShader(shaderGraphRequirements.requiresPosition, pixelShader,
                ShaderGeneratorNames.ObjectSpacePosition, ShaderGeneratorNames.ViewSpacePosition,
                ShaderGeneratorNames.WorldSpacePosition, ShaderGeneratorNames.TangentSpacePosition);

            if (shaderGraphRequirements.requiresVertexColor)
            {
                interpolators.AddShaderChunk(string.Format("float4 {0} : COLOR;", ShaderGeneratorNames.VertexColor), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = color", ShaderGeneratorNames.VertexColor), false);
                pixelShader.AddShaderChunk(string.Format("surfaceInput.{0} = IN.{0};", ShaderGeneratorNames.VertexColor), false);
            }

            if (shaderGraphRequirements.requiresScreenPosition)
            {
                interpolators.AddShaderChunk(string.Format("float4 {0} : TEXCOORD{1};;", ShaderGeneratorNames.ScreenPosition, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = ComputeScreenPos(UnityObjectToClipPos(v.vertex)", ShaderGeneratorNames.ScreenPosition), false);
                pixelShader.AddShaderChunk(string.Format("surfaceInput.{0} = IN.{0};", ShaderGeneratorNames.ScreenPosition), false);
                interpolatorIndex++;
            }

            for (int uvIndex = 0; uvIndex < ShaderGeneratorNames.UVCount; ++uvIndex)
            {
                var channel = (UVChannel) uvIndex;
                if (activeNodeList.OfType<IMayRequireMeshUV>().Any(x => x.RequiresMeshUV(channel)))
                {
                    interpolators.AddShaderChunk(string.Format("half4 meshUV{0} : TEXCOORD{1};", uvIndex, interpolatorIndex), false);
                    vertexShader.AddShaderChunk(string.Format("o.meshUV{0} = v.texcoord{1};", uvIndex, uvIndex), false);
                    pixelShader.AddShaderChunk(string.Format("surfaceInput.{0}  = IN.meshUV{1};", channel.GetUVName(), uvIndex), false);
                    interpolatorIndex++;
                }
            }

            var outputs = new ShaderGenerator();
            var outputSlot = node.GetOutputSlots<MaterialSlot>().FirstOrDefault();
            if (outputSlot != null)
            {
                var result = string.Format("surf.{0}", node.GetVariableNameForSlot(outputSlot.id));
                outputs.AddShaderChunk(string.Format("return {0};", AdaptNodeOutputForPreview(node, outputSlot.id, result)), true);
            }
            else
                outputs.AddShaderChunk("return 0;", true);

            var res = subShaderTemplate.Replace("{0}", interpolators.GetShaderString(0));
            res = res.Replace("{1}", vertexShader.GetShaderString(0));
            res = res.Replace("{2}", pixelShader.GetShaderString(0));
            res = res.Replace("{3}", outputs.GetShaderString(0));
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
            {0}
        };

        GraphVertexOutput vert (GraphVertexInput v)
        {
            v = PopulateVertexData(v);

            GraphVertexOutput o;
            o.position = UnityObjectToClipPos(v.vertex);
            {1}
            return o;
        }

        fixed4 frag (GraphVertexOutput IN) : SV_Target
        {
            SurfaceInputs surfaceInput;
            {2}

            SurfaceDescription surf = PopulateSurfaceData(surfaceInput);
            {3}
        }
        ENDCG
    }
}";
    }
}
