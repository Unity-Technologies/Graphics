using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class HDRenderPipelineMenuItems
    {
        [MenuItem("HDRenderPipeline/Synchronize all Layered materials")]
        static void SynchronizeAllLayeredMaterial()
        {
            Object[] materials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (Object obj in materials)
            {
                Material mat = obj as Material;
                if (mat.shader.name == "HDRenderPipeline/LayeredLit" || mat.shader.name == "HDRenderPipeline/LayeredLitTessellation")
                {
                    LayeredLitGUI.SynchronizeAllLayers(mat);
                }
            }
        }

        [MenuItem("HDRenderPipeline/Update all materials keywords")]
        static void UpdateAllMaterialKeywords()
        {
            Object[] materials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (Object obj in materials)
            {
                Material mat = obj as Material;
                if (mat.shader.name == "HDRenderPipeline/LayeredLit" || mat.shader.name == "HDRenderPipeline/LayeredLitTessellation")
                {
                    LayeredLitGUI.SetupMaterialKeywords(mat);
                }
                else if (mat.shader.name == "HDRenderPipeline/Lit" || mat.shader.name == "HDRenderPipeline/LitTessellation")
                {
                    LitGUI.SetupMaterialKeywords(mat);
                }
            }
        }
    }
}
