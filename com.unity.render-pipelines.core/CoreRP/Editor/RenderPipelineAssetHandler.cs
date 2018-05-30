using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEditor;
using UnityEditor.Callbacks;

public class RenderPipelineAssetHandler
{
        // Assign RenderPipeline Asset on double click
        [OnOpenAssetAttribute()]
        public static bool OpenAsset(int instanceID, int line)
        {
			Object obj = EditorUtility.InstanceIDToObject(instanceID);

			RenderPipelineAsset rpAsset = obj as RenderPipelineAsset;
			if (rpAsset != null)
			{
				GraphicsSettings.renderPipelineAsset = rpAsset;
				return true;
			}
            return false; // we did not handle the open
        }
}
