using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
 using UnityEngine;

 interface ISurfacecShaderPass
 {
     string GetPass();
 }
 
interface ISurfaceSubShader
{
    string GetSubShader(string surfaceShader);
    string GetAdditonalIncludes();
}

interface IOpaqueSurfaceSubShader : ISurfaceSubShader
{ }

 class UniversalForwardSurfaceShaderPass : ISurfacecShaderPass
 {
     private const string Guid = "6f9e55b20bbdf4c429b1437578e36933";

     public string GetPass()
     {
         string path = AssetDatabase.GUIDToAssetPath(Guid);
         return File.ReadAllText(path, Encoding.UTF8);
     }
 }

class UniversalDepthOnlySurfaceShaderPass : ISurfacecShaderPass
{
    private const string Guid = "9a2e027e95e0343458c375c75ee91317";

    public string GetPass()
    {
        string path = AssetDatabase.GUIDToAssetPath(Guid);
        return File.ReadAllText(path, Encoding.UTF8);
    }
}

class UniversalShadowCasterSurfaceShaderPass : ISurfacecShaderPass
{
    private const string Guid = "4a9665a8ebed2444fadc74a2f4e46293";

    public string GetPass()
    {
        string path = AssetDatabase.GUIDToAssetPath(Guid);
        return File.ReadAllText(path, Encoding.UTF8);
    }
}

class UniversalOpaqueSurfaceShader : IOpaqueSurfaceSubShader
{
    private UniversalForwardSurfaceShaderPass forwardPass = new UniversalForwardSurfaceShaderPass();
    private UniversalDepthOnlySurfaceShaderPass depthPass = new UniversalDepthOnlySurfaceShaderPass();
    private UniversalShadowCasterSurfaceShaderPass shadowCasterPass = new UniversalShadowCasterSurfaceShaderPass();


    public string GetAdditonalIncludes()
    {
        var sb = new StringBuilder();
        sb.AppendLine("#include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/CustomShading.hlsl\"");
        return sb.ToString();
    }

    public string GetSubShader(string surfaceShader)
    {
        Dictionary<string, string> tags = new Dictionary<string, string>();
        tags["RenderPipeline"] = "UniversalRenderPipeline";

        var subshader = new StringBuilder();
        subshader.AppendLine("Subshader");
        subshader.AppendLine("{");

        foreach (var tag in tags)
        {
            subshader.AppendLine($"Tags{{\"{tag.Key}\" = \"{tag.Value}\"}}");
        }

        subshader.AppendLine("HLSLINCLUDE");
        subshader.AppendLine(surfaceShader);
        subshader.AppendLine("ENDHLSL");

        subshader.AppendLine(forwardPass.GetPass());
        subshader.AppendLine(depthPass.GetPass());
        subshader.AppendLine(shadowCasterPass.GetPass());
        subshader.AppendLine("}");
        return subshader.ToString();
    }
}

[ScriptedImporter(1, Extension, 3)]
class SurfaceShaderImporter : ScriptedImporter
{
    public const string Extension = "surfaceshader";

    public const string k_ErrorShader = @"
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

    public const string k_GlobalIlluminationFunction = @"
    half3 GlobalIlluminationFunction(CustomSurfaceData surfaceData, half3 environmentLighting, half3 environmentReflections, half3 viewDirectionWS)
    {
        half3 NdotV = saturate(dot(surfaceData.normalWS, viewDirectionWS)) + HALF_MIN;
        environmentReflections *= EnvironmentBRDF(surfaceData.reflectance, surfaceData.roughness, NdotV);
        environmentLighting = environmentLighting * surfaceData.diffuse;

        return (environmentReflections + environmentLighting) * surfaceData.ao;
    }

";

    public const string k_LightingFunction = @"
    half3 LightingFunction(CustomSurfaceData surfaceData, LightingData lightingData, half3 viewDirectionWS)
    {
        half3 diffuse = surfaceData.diffuse * Lambert();

        // CookTorrance
        // inline D_GGX + V_SmithJoingGGX for better code generations
        half3 NdotV = saturate(dot(surfaceData.normalWS, viewDirectionWS)) + HALF_MIN;
        half DV = DV_SmithJointGGX(lightingData.NdotH, lightingData.NdotL, NdotV, surfaceData.roughness);

        // for microfacet fresnel we use H instead of N. In this case LdotH == VdotH, we use LdotH as it
        // seems to be more widely used convetion in the industry.
        half3 F = F_Schlick(surfaceData.reflectance, lightingData.LdotH);
        half3 specular = DV * F;
        half3 finalColor = (diffuse + specular) * lightingData.light.color * lightingData.NdotL;
        return finalColor;
    }

";

    string GetShaderName(string shaderSnippet)
    {
        int shaderNameTokenIndex = shaderSnippet.IndexOf("Shader", StringComparison.Ordinal);
        int startNameIndex = shaderSnippet.IndexOf("\"", shaderNameTokenIndex + "Shader".Length, StringComparison.Ordinal);
        int endNameIndex = shaderSnippet.IndexOf("\"", startNameIndex + 1, StringComparison.Ordinal);

        if (endNameIndex - startNameIndex <= 0)
            return "";
        
        return shaderSnippet.Substring(startNameIndex, endNameIndex - startNameIndex + 1);
    }

    string GetSurfaceShader(string shaderSnippet)
    {
        var surfaceShaderStart = shaderSnippet.IndexOf("SURFACESHADER", StringComparison.Ordinal) ;
        var surfaceShaderCount = shaderSnippet.IndexOf("ENDSURFACESHADER", StringComparison.Ordinal) - surfaceShaderStart;
        return shaderSnippet.Substring(surfaceShaderStart + "SURFACESHADER".Length, surfaceShaderCount - "SURFACESHADER".Length);
    }

    string GetDefaultFunctions(string shaderSnippet)
    {
        StringBuilder functions = new StringBuilder();
        if (shaderSnippet.IndexOf("GlobalIlluminationFunction", StringComparison.Ordinal) == -1)
            functions.Append(k_GlobalIlluminationFunction);
        
        if (shaderSnippet.IndexOf("LightingFunction", StringComparison.Ordinal) == -1)
            functions.Append(k_LightingFunction);

        return functions.ToString();
    }

    string GetShaderProperties(string shaderSnippet)
    {
        int shaderNameTokenIndex = shaderSnippet.IndexOf("Properties", StringComparison.Ordinal);
        int startNameIndex = shaderSnippet.IndexOf("{", shaderNameTokenIndex + "Properties".Length, StringComparison.Ordinal);
        int depth = 0;
        int endNameIndex = startNameIndex;
        while (endNameIndex < shaderSnippet.Length)
        {
            endNameIndex++;
            char c = shaderSnippet[endNameIndex];
            if (c == '}')
            {
                if (depth == 0)
                    break;

                depth--;
            }

            if (c == '{')
                depth++;
        }

        if (endNameIndex - startNameIndex <= 2)
            return "";
        
        return shaderSnippet.Substring(startNameIndex + 1, endNameIndex - startNameIndex - 1);
    }

    public override void OnImportAsset(AssetImportContext ctx)
    {
        var oldShader = AssetDatabase.LoadAssetAtPath<Shader>(ctx.assetPath);
        if (oldShader != null)
            ShaderUtil.ClearShaderMessages(oldShader);

        string path = ctx.assetPath;
        var sourceAssetDependencyPaths = new List<string>();

        UnityEngine.Object mainObject;

        var textGraph = File.ReadAllText(path, Encoding.UTF8);

        var shaderName = GetShaderName(textGraph);
        var shaderProperties = GetShaderProperties(textGraph);
        var surfaceShader = GetSurfaceShader(textGraph);
        var defaultFunctions = GetDefaultFunctions(textGraph);
        
        // Surface Shader Template
        var templatePath = AssetDatabase.GUIDToAssetPath("f9cb61ee5b9aa4521bce6ae21747d4dc");
        var templateText = File.ReadAllText(templatePath, Encoding.UTF8);

        templateText = templateText.Replace("$SHADERNAME", shaderName);
        templateText = templateText.Replace("$SHADERPROPERTIES", shaderProperties);
        templateText = templateText.Replace("$SURFACESHADER", surfaceShader);
        templateText = templateText.Replace("$SURFACEFUNCTIONS", defaultFunctions);

        // TODO:
        templateText = templateText.Replace("$SHADERRENDERSTATE", "");
        templateText = templateText.Replace("$SHADERPRAGMAS", "");
        templateText = templateText.Replace("$SHADERKEYWORDS", "");
        templateText = templateText.Replace("$SHADERINCLUDES", "");
        templateText = templateText.Replace("$SHADERFALLBACK", "");
        templateText = templateText.Replace("$SHADERCUSTOMEDITOR", "");

        File.WriteAllText(path + ".shader", templateText, Encoding.UTF8);
        var shader = ShaderUtil.CreateShaderAsset(templateText, false);
        mainObject = shader;
        
        Texture2D texture = Resources.Load<Texture2D>("Icons/sg_graph_icon@64");
        ctx.AddObjectToAsset("MainAsset", mainObject, texture);
        ctx.SetMainObject(mainObject);

        foreach (var sourceAssetDependencyPath in sourceAssetDependencyPaths.Distinct())
        {
            // Ensure that dependency path is relative to project
            if (!sourceAssetDependencyPath.StartsWith("Packages/") && !sourceAssetDependencyPath.StartsWith("Assets/"))
            {
                Debug.LogWarning($"Invalid dependency path: {sourceAssetDependencyPath}", mainObject);
                continue;
            }

            ctx.DependsOnSourceAsset(sourceAssetDependencyPath);
        }
    }
}