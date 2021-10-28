using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering
{
    internal class ShaderStripExporter
    {
        const string s_TempShaderStripJson = "Temp/shader-strip.json";

        [Serializable]
        public class Data
        {
            public string shader;
            public string pass;
            public string passType;
            public string shaderType;
            public int variantIn;
            public int variantOut;
            public int totalVariantIn;
            public int totalVariantOut;

            public Data(Shader shader, ShaderSnippetData snippetData, IList<ShaderCompilerData> inputData)
            {
                this.shader = shader.name;
                pass = snippetData.passName;
                passType = snippetData.passType.ToString();
                shaderType = snippetData.shaderType.ToString();
                variantIn = inputData.Count;
            }
        }

        public static void Export(Data shaderExport)
        {
            try
            {
                System.IO.File.AppendAllText(s_TempShaderStripJson, $"{JsonUtility.ToJson(shaderExport)},{Environment.NewLine}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
