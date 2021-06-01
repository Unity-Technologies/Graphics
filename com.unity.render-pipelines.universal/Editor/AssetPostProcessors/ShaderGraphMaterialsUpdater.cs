using Unity.Rendering.Universal;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    class UniversalShaderGraphSaveContext
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
            // In case the shader is not Universal
            if (!(saveContext is UniversalShaderGraphSaveContext universalSaveContext))
                return;

            if (!universalSaveContext.updateMaterials)
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
                        ShaderUtils.UpdateMaterial(materials[i], ShaderUtils.MaterialUpdateType.ModifiedShader);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
