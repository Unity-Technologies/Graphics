using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
	public class SetupSceneForRenderPipelineTest : MonoBehaviour 
	{
		private RenderPipelineAsset m_OriginalAsset;
		public RenderPipelineAsset renderPipeline;
		public Camera cameraToUse;
		public bool hdr = false;

		public int width = 1280;
		public int height = 720;

		public void Setup()
		{
			m_OriginalAsset = GraphicsSettings.renderPipelineAsset;
			GraphicsSettings.renderPipelineAsset = renderPipeline;
		}

		public void TearDown()
		{
			GraphicsSettings.renderPipelineAsset = m_OriginalAsset;
		}
	}
}
