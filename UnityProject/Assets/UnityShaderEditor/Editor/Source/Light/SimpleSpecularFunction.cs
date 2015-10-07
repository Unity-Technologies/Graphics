using System;
using UnityEngine;

namespace UnityEditor.Graphs.Material
{
    class SimpleSpecularFunction : BaseLightFunction
    {
        public override string GetLightFunctionName() { return "SimpleSpecular"; }
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
    }
}
