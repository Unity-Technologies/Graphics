using System;
using UnityEngine;

namespace UnityEditor.Graphs.Material
{
    class WrapLambertFunction : BaseLightFunction
    {
        public override string GetLightFunctionName() { return "WrapLambert"; }
        public override void GenerateLightFunctionBody(ShaderGenerator visitor)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk("half4 Lighting" + GetLightFunctionName() + " (SurfaceOutput s, half3 lightDir, half atten)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("half NdotL = dot (s.Normal, lightDir);", false);
            outputString.AddShaderChunk("half diff = NdotL * 0.5 + 0.5;", false);
            outputString.AddShaderChunk("half4 c;", false);
            outputString.AddShaderChunk("c.rgb = s.Albedo * _LightColor0.rgb * (diff * atten * 2);", false);
            outputString.AddShaderChunk("c.a = s.Alpha;", false);
            outputString.AddShaderChunk("return c;", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
