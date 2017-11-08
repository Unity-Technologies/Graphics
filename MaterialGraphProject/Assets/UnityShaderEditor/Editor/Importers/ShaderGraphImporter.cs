using UnityEditor.ShaderGraph;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;

[ScriptedImporter(2, "shadergraph")]
public class ShaderGraphImporter : ScriptedImporter
{
    private string errorShader = @"
Shader ""Hidden/GraphErrorShader2""
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #include ""UnityCG.cginc""

            struct appdata_t {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(1,0,1,1);
            }
            ENDCG
        }
    }
    Fallback Off
}";

    public override void OnImportAsset(AssetImportContext ctx)
    {

        var text = GetShaderText<MaterialGraph>(ctx.assetPath);
        if (text == null)
            text = errorShader;

        var shader = ShaderUtil.CreateShaderAsset(text);
        ctx.AddObjectToAsset("MainAsset", shader);
        ctx.SetMainObject(shader);
    }

    private static string GetShaderText<T>(string path) where T : IShaderGraph
    {
        try
        {
            var textGraph = File.ReadAllText(path, Encoding.UTF8);
            var graph = JsonUtility.FromJson<T>(textGraph);
            graph.LoadedFromDisk();

            var name = Path.GetFileNameWithoutExtension(path);

            List<PropertyCollector.TextureInfo> configuredTextures;
            var shaderString = graph.GetShader(string.Format("graphs/{0}", name), GenerationMode.ForReals, out configuredTextures);
            Debug.Log(shaderString);
            return shaderString;
        }
        catch (Exception)
        {
            // ignored
        }
        return null;
    }
}
