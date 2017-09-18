using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    [Title("Master/Master")]
    public class MasterNode : AbstractMaterialNode
    {
        public MasterNode()
        {
            name = "MasterNode";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(0, "Test", "Test", SlotType.Input, SlotValueType.Vector4, Vector4.one));
            RemoveSlotsNameNotMatching(new[] { 0 });
        }

        protected override bool generateDefaultInputs { get { return false; } }

        public override IEnumerable<ISlot> GetInputsWithNoConnection()
        {
            return new List<ISlot>();
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public override bool allowedInSubGraph
        {
            get { return false; }
        }

        public virtual bool has3DPreview()
        {
            return true;
        }

        public string GetSubShader(ShaderGraphRequirements shaderGraphRequirements)
        {
            var activeNodeList = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this);

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
                interpolators.AddShaderChunk(string.Format("float4 {0} : TEXCOORD{1};", ShaderGeneratorNames.ObjectSpaceViewDirection, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = ObjSpaceViewDir(v.vertex);", ShaderGeneratorNames.ObjectSpaceViewDirection), false);
                pixelShader.AddShaderChunk(string.Format("float4 {0} = normalize(IN.{0});", ShaderGeneratorNames.ObjectSpaceViewDirection), false);
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

            ShaderGenerator.GenerateSpaceTranslationPixelShader(shaderGraphRequirements.requiresNormal, pixelShader,
                ShaderGeneratorNames.ObjectSpaceNormal, ShaderGeneratorNames.ViewSpaceNormal,
                ShaderGeneratorNames.WorldSpaceNormal, ShaderGeneratorNames.TangentSpaceNormal);

            ShaderGenerator.GenerateSpaceTranslationPixelShader(shaderGraphRequirements.requiresTangent, pixelShader,
                ShaderGeneratorNames.ObjectSpaceTangent, ShaderGeneratorNames.ViewSpaceTangent,
                ShaderGeneratorNames.WorldSpaceTangent, ShaderGeneratorNames.TangentSpaceTangent);

            ShaderGenerator.GenerateSpaceTranslationPixelShader(shaderGraphRequirements.requiresBitangent, pixelShader,
                ShaderGeneratorNames.ObjectSpaceBiTangent, ShaderGeneratorNames.ViewSpaceBiTangent,
                ShaderGeneratorNames.WorldSpaceSpaceBiTangent, ShaderGeneratorNames.TangentSpaceBiTangent);

            ShaderGenerator.GenerateSpaceTranslationPixelShader(shaderGraphRequirements.requiresViewDir, pixelShader,
                ShaderGeneratorNames.ObjectSpaceViewDirection, ShaderGeneratorNames.ViewSpaceViewDirection,
                ShaderGeneratorNames.WorldSpaceViewDirection, ShaderGeneratorNames.TangentSpaceViewDirection);

            ShaderGenerator.GenerateSpaceTranslationPixelShader(shaderGraphRequirements.requiresPosition, pixelShader,
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
                var channel = (UVChannel)uvIndex;
                if (activeNodeList.OfType<IMayRequireMeshUV>().Any(x => x.RequiresMeshUV(channel)))
                {
                    interpolators.AddShaderChunk(string.Format("half4 meshUV{0} : TEXCOORD{1};", uvIndex, interpolatorIndex), false);
                    vertexShader.AddShaderChunk(string.Format("o.meshUV{0} = v.texcoord{1};", uvIndex, uvIndex == 0 ? "" : uvIndex.ToString()), false);
                    pixelShader.AddShaderChunk(string.Format("surfaceInput.{0}  = IN.meshUV{1};", channel.GetUVName(), uvIndex), false);
                    interpolatorIndex++;
                }
            }

            var outputs = new ShaderGenerator();
            outputs.AddShaderChunk(string.Format("return surf.{0};", FindSlot<MaterialSlot>(0).shaderOutputName), true);

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
