using System;
using System.Collections.Generic;
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

        private string subShaderTemplate = @"
SubShader
{
    Tags { ""RenderType""=""Opaque"" }
    LOD 100

    Pass
    {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        // make fog work
        #pragma multi_compile_fog

        #include ""UnityCG.cginc""

        GraphVertexOutput vert (GraphVertexInput v)
        {
            v = PopulateVertexData(v);

            GraphVertexOutput o;
            o.position = UnityObjectToClipPos(v.vertex);
            {0}
            return o;
        }

        fixed4 frag (GraphVertexOutput IN) : SV_Target
        {
            SurfaceInputs surfaceInput;
            {1}

            SurfaceDescription surf = PopulateSurfaceData(surfaceInput);

            {2}
        }
        ENDCG
    }
}";

        public string GetSubShader(bool requiresNormal)
        {
            var vertexShader = new ShaderGenerator();
            var pixelShader = new ShaderGenerator();

            vertexShader.AddShaderChunk("float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;", true);
            vertexShader.AddShaderChunk("float3 viewDir = UnityWorldSpaceViewDir(worldPos);", true);
            vertexShader.AddShaderChunk("float4 screenPos = ComputeScreenPos(UnityObjectToClipPos(v.vertex));", true);
            vertexShader.AddShaderChunk("float3 worldNormal = UnityObjectToWorldNormal(v.normal);", true);

            if (requiresNormal)
            {
                vertexShader.AddShaderChunk(string.Format("o.{0} = worldPos", ShaderGeneratorNames.WorldSpacePosition), false);
                pixelShader.AddShaderChunk(string.Format("surfaceInput.{0} = IN.worldPos;", ShaderGeneratorNames.WorldSpacePosition), false);
            }

            var outputs = new ShaderGenerator();
            outputs.AddShaderChunk(string.Format("return surf.{0};", FindSlot<MaterialSlot>(0).shaderOutputName), true);

            var res = subShaderTemplate.Replace("{0}", vertexShader.GetShaderString(0));
            res = res.Replace("{1}", pixelShader.GetShaderString(0));
            res = res.Replace("{2}", outputs.GetShaderString(0));
            return res;

        }
    }
}
