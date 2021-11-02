using System;
using UnityEngine;

namespace UnityEditor.Rendering
{
    internal class ShaderStripExporter
    {
        const string s_TempShaderStripJson = "Temp/shader-strip.json";

        public static void Export(Shader shader, ShaderSnippetData snippetData, int variantIn, int variantOut, int totalVariantIn, int totalVariantOut)
        {
            try
            {
                System.IO.File.AppendAllText(
                            s_TempShaderStripJson,
                            $"{{ \"shader\": \"{shader?.name}\", \"pass\": \"{snippetData.passName ?? string.Empty}\", \"passType\": \"{snippetData.passType}\", \"shaderType\": \"{snippetData.shaderType}\", \"variantIn\": \"{variantIn}\", \"variantOut\": \"{variantOut}\", \"totalVariantIn\": \"{totalVariantIn}\", \"totalVariantOut\": \"{totalVariantOut}\" }}\r\n"
                        );
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
