using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDSaveContext
    {
        public bool updateMaterials;
    }

    [InitializeOnLoad]
    class ShaderGraphMaterialsUpdater
    {
        const string kMaterialFilter = "t:Material";

        static ShaderGraphMaterialsUpdater()
        {
            GraphData.onSaveGraph += OnShaderGraphSaved;
        }

        static void OnShaderGraphSaved(Shader shader, object saveContext)
        {
            // In case the shader is not HDRP
            if (!(saveContext is HDSaveContext hdSaveContext))
                return;

            if (!hdSaveContext.updateMaterials)
                return;

            // Iterate all Materials
            string[] materialGuids = AssetDatabase.FindAssets(kMaterialFilter);
            try
            {
                for (int i = 0, length = materialGuids.Length; i < length; i++)
                {
                    // Only update progress bar every 10 materials
                    if (i % 10 == 9)
                    {
                        EditorUtility.DisplayProgressBar(
                            "Checking material dependencies...",
                            $"{i} / {length} materials.",
                            i / (float)(length - 1));
                    }

                     // Get Material object
                    string materialPath = AssetDatabase.GUIDToAssetPath(materialGuids[i]);
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                     // Reset keywords
                    if (material.shader.name == shader.name)
                        HDShaderUtils.ResetMaterialKeywords(material);

                    material = null;

                    // Free the materials every 200 iterations, on big project loading all materials in memory can lead to a crash
                    if ((i % 200 == 0) && i != 0)
                        EditorUtility.UnloadUnusedAssetsImmediate(true);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.UnloadUnusedAssetsImmediate(true);
            }
        }
    }
}
