using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.Rendering.HighDefinition
{
    [InitializeOnLoad]
    public class ShaderGraphMaterialsUpdater
    {
        const string kMaterialFilter = "t:Material";

        static ShaderGraphMaterialsUpdater()
        {
            GraphData.onSaveGraph += OnShaderGraphSaved;
        }

        static void OnShaderGraphSaved(Shader shader)
        {
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
                            "Updating dependent materials...",
                            string.Format("{0} / {1} materials updated.", i, length),
                            i / (float)(length - 1));
                    }

                     // Get Material object
                    string materialPath = AssetDatabase.GUIDToAssetPath(materialGuids[i]);
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                     // Reset keywords
                    if (material.shader.name == shader.name)
                        HDShaderUtils.ResetMaterialKeywords(material);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}