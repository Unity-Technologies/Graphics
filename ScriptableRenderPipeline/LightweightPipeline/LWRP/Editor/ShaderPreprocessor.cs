using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    class ShaderPreprocessor : IPreprocessShaders
    {
        int totalVariantsInputCount = 0;
        int totalVariantsOutputCount = 0;

        // Multiple callback may be implemented.
        // The first one executed is the one where callbackOrder is returning the smallest number.
        public int callbackOrder { get { return 0; } }

        private bool StripUnusedVariant(ShaderSnippetData snippetData, ShaderCompilerData compilerData)
        {
            if (snippetData.passName == "Meta")
                return true;

            return false;
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
           int initDataCount = data.Count;

            for (int i = 0; i < data.Count; ++i)
            {
                if (StripUnusedVariant(snippet, data[i]))
                {
                    data.RemoveAt(i);
                    --i;
                }
            }

            totalVariantsInputCount += initDataCount;
            totalVariantsOutputCount += data.Count;

            if (shader.name.Contains("LightweightPipeline/Standard (Physically Based)") || shader.name.Contains("LightweightPipeline/Standard (Simple Lighting)"))
            {
                float percentageCurrent = (float) data.Count / (float) initDataCount * 100f;
                float percentageTotal = (float) totalVariantsOutputCount / (float) totalVariantsInputCount * 100f;
                string result = "STRIPPING: " + shader.name + "(" + snippet.passName + " pass)" + "(" + snippet.shaderType.ToString() +
                                ") - Remaining shader variants = " + data.Count + " / " + initDataCount + " = " +
                                percentageCurrent + "% - Total = " + totalVariantsOutputCount + " / " +
                                totalVariantsInputCount + " = " + percentageTotal + "%";
                Debug.Log(result);
            }
        }
    }
}

