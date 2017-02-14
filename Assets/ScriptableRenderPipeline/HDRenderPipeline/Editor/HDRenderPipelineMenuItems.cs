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
    }
}
