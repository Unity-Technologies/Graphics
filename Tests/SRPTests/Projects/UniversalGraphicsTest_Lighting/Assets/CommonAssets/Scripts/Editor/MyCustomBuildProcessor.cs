using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

class MyCustomBuildProcessor : IPreprocessShaders
{
    ShaderKeyword m_GlobalKeywordBlue;

    public MyCustomBuildProcessor()
    {
        m_GlobalKeywordBlue = new ShaderKeyword("_BLUE");
    }

    public int callbackOrder { get { return 0; } }

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (!shader.name.StartsWith("Universal")
            || !shader.name.StartsWith("Hidden/Universal"))
            return;

        Debug.Log("YEAY! " + shader.name);

        for (int i = data.Count - 1; i >= 0; --i)
            data.RemoveAt(i);
    }
}
