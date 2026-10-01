using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

class DotsInstancingVariantStripper : IPreprocessShaders
{
    static readonly ShaderKeyword k_DotsInstancing = new ShaderKeyword("DOTS_INSTANCING_ON");

    public int callbackOrder => 0;

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        for (int i = data.Count - 1; i >= 0; --i)
        {
            if (data[i].shaderKeywordSet.IsEnabled(k_DotsInstancing))
                data.RemoveAt(i);
        }
    }
}
