using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityEditor.Rendering.BuiltIn
{
    class BuiltInShaderGraphSaveContext
    {
        public bool updateMaterials;
    }

    [InitializeOnLoad]
    class ShaderGraphMaterialsUpdater
    {
        static ShaderGraphMaterialsUpdater()
        {
            GraphData.onSaveGraph += OnShaderGraphSaved;
        }

        static void OnShaderGraphSaved(Shader shader, object saveContext)
        {
            // In case the shader is not BuiltIn
            if (!(saveContext is BuiltInShaderGraphSaveContext builtInShaderGraphSaveContext))
                return;

            if (!builtInShaderGraphSaveContext.updateMaterials)
                return;

            // Iterate over all loaded Materials
            Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
            try
            {
                for (int i = 0, length = materials.Length; i < length; i++)
                {
                    // Only update progress bar every 10 materials
                    if (i % 10 == 9)
                    {
                        EditorUtility.DisplayProgressBar(
                            "Checking material dependencies...",
                            $"{i} / {length} materials.",
                            i / (float)(length - 1));
                    }

                    // Reset keywords
                    if (materials[i].shader.name == shader.name)
                        ShaderUtils.ResetMaterialKeywords(materials[i]);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
