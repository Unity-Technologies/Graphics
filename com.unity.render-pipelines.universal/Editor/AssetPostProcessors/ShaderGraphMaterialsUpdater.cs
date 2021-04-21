using Unity.Rendering.Universal;
using UnityEditor.ShaderGraph;
using UnityEngine;

// The purpose of this file is to handle when a Universal ShaderGraph is saved,
// and ensure all Materials that use that ShaderGraph
// have updated their keywords and other settings appropriately.
// This system should be removed once we can declare a Material to have
// an import dependency on the ShaderGraph artifact
// (which would have the same effect, but work faster)

namespace UnityEditor.Rendering.Universal
{
    class UniversalShaderGraphSaveContext
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
            // In case the shader is not Universal
            if (!(saveContext is UniversalShaderGraphSaveContext universalSaveContext))
                return;

            if (!universalSaveContext.updateMaterials)
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
                    if ((material != null) && material.shader.name == shader.name)
                        ShaderUtils.UpdateMaterial(material, ShaderUtils.MaterialUpdateType.ModifiedShader);

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
