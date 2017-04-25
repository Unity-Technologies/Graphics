using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering
{
	[ExecuteInEditMode]
	public class SetupSceneForRenderPipelineTest : MonoBehaviour 
	{
		private RenderPipelineAsset m_OriginalAsset;
		public RenderPipelineAsset renderPipeline;
		public Camera cameraToUse;

		public int width = 1280;
		public int height = 720;

		void OnEnable()
		{
			m_OriginalAsset = GraphicsSettings.renderPipelineAsset;
			GraphicsSettings.renderPipelineAsset = renderPipeline;
		}

		void OnDisable()
		{
			GraphicsSettings.renderPipelineAsset = m_OriginalAsset;
		}
	}
}
