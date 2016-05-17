using UnityEditor.Graphs;

namespace UnityEditor.MaterialGraph
{
    class SimpleSpecularFunction : BaseLightFunction
    {
        public const string kAlbedoSlotName = "Albedo";
        public const string kAlphaSlotName = "Alpha";

        public override string GetLightFunctionName() { return "SimpleSpecular"; }
        public override string GetSurfaceOutputStructureName() {return "SurfaceOutput";}

        public override void GenerateLightFunctionBody(ShaderGenerator visitor)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk("half4 Lighting" + GetLightFunctionName() + " (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("half3 h = normalize (lightDir + viewDir);", false);
            outputString.AddShaderChunk("half diff = max (0, dot (s.Normal, lightDir));", false);
            outputString.AddShaderChunk("half nh = max (0, dot (s.Normal, h));", false);
            outputString.AddShaderChunk("half spec = pow (nh, 48.0);", false);
            outputString.AddShaderChunk("half4 c; c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec) * (atten * 2);", false);
            outputString.AddShaderChunk("c.a = s.Alpha;", false);
            outputString.AddShaderChunk("return c;", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        public override void DoSlotsForConfiguration(PixelShaderNode node)
        {
            node.AddSlot(new MaterialGraphSlot(new Slot(name: SlotType.InputSlot, slotType: kAlbedoSlotName), SlotValueType.Vector3));
            node.AddSlot(new MaterialGraphSlot(new Slot(name: SlotType.InputSlot, slotType: kNormalSlotName), SlotValueType.Vector3));
            node.AddSlot(new MaterialGraphSlot(new Slot(name: SlotType.InputSlot, slotType: kAlphaSlotName), SlotValueType.Vector1));

            // clear out slot names that do not match the slots 
            // we support
            node.RemoveSlotsNameNotMatching(
                new[]
                {
                    kAlbedoSlotName,
                    kNormalSlotName,
                    kAlphaSlotName
                });
        }
    }
}
