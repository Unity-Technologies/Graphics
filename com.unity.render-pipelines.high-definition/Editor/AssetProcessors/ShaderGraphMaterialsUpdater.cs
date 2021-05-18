using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDSaveContext
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
            // In case the shader is not HDRP
            if (!(saveContext is HDSaveContext hdSaveContext))
                return;

            HDRenderPipeline.currentPipeline?.ResetPathTracing();

            if (!hdSaveContext.updateMaterials)
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
                        HDShaderUtils.ResetMaterialKeywords(materials[i]);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
