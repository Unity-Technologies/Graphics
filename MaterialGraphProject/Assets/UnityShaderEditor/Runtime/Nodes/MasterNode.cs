using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    [Title("Master/Master")]
    public abstract class MasterNode : AbstractMaterialNode
    {
        [SerializeField]
        protected SurfaceMaterialOptions m_MaterialOptions = new SurfaceMaterialOptions();

        public MasterNode()
        {
            name = "MasterNode";
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
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

        public abstract string GetSubShader(ShaderGraphRequirements shaderGraphRequirements);
        /*{
            var tagsVisitor = new ShaderGenerator();
            var blendingVisitor = new ShaderGenerator();
            var cullingVisitor = new ShaderGenerator();
            var zTestVisitor = new ShaderGenerator();
            var zWriteVisitor = new ShaderGenerator();

            m_MaterialOptions.GetTags(tagsVisitor);
            m_MaterialOptions.GetBlend(blendingVisitor);
            m_MaterialOptions.GetCull(cullingVisitor);
            m_MaterialOptions.GetDepthTest(zTestVisitor);
            m_MaterialOptions.GetDepthWrite(zWriteVisitor);

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
                interpolators.AddShaderChunk(string.Format("float4 {0} : TEXCOORD{1};", ShaderGeneratorNames.ScreenPosition, interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = ComputeScreenPos(UnityObjectToClipPos(v.vertex)", ShaderGeneratorNames.ScreenPosition), false);
                pixelShader.AddShaderChunk(string.Format("surfaceInput.{0} = IN.{0};", ShaderGeneratorNames.ScreenPosition), false);
                interpolatorIndex++;
            }

            foreach (var channel in shaderGraphRequirements.requiresMeshUVs.Distinct())
            {
                interpolators.AddShaderChunk(string.Format("half4 {0} : TEXCOORD{1};", channel.GetUVName(), interpolatorIndex), false);
                vertexShader.AddShaderChunk(string.Format("o.{0} = v.texcoord{1};", channel.GetUVName(), interpolatorIndex == 0 ? "" : interpolatorIndex.ToString()), false);
                pixelShader.AddShaderChunk(string.Format("surfaceInput.{0}  = IN.{0};", channel.GetUVName()), false);
                interpolatorIndex++;
            }

            var outputs = new ShaderGenerator();
            var slot = FindSlot<MaterialSlot>(0);

            var result = string.Format("surf.{0}", slot.shaderOutputName);
            outputs.AddShaderChunk(string.Format("return {0};", ShaderGenerator.AdaptNodeOutputForPreview(this, 0, result)), true);

            var resultShader = subShaderTemplate.Replace("{0}", interpolators.GetShaderString(0));
            resultShader = resultShader.Replace("{1}", vertexShader.GetShaderString(0));
            resultShader = resultShader.Replace("{2}", pixelShader.GetShaderString(0));
            resultShader = resultShader.Replace("{3}", outputs.GetShaderString(0));

            resultShader = resultShader.Replace("${Tags}", tagsVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${Blending}", blendingVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${Culling}", cullingVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ZTest}", zTestVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ZWrite}", zWriteVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${LOD}", "" + m_MaterialOptions.lod);
            return resultShader;
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
}";*/
    }
}
