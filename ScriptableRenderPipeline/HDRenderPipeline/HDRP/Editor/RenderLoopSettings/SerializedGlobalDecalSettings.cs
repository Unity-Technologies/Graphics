using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering
{
	class SerializedGlobalDecalSettings
	{
		public SerializedProperty root;

		public SerializedProperty drawDistance;
		public SerializedProperty atlasSize;

		public SerializedGlobalDecalSettings(SerializedProperty root)
        {
            this.root = root;

			drawDistance = root.Find((GlobalDecalSettings s) => s.drawDistance);
			atlasSize = root.Find((GlobalDecalSettings s) => s.atlasSize);			
        }
	}
}
