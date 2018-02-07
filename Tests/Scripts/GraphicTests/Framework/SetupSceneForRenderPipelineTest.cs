using UnityEngine.Rendering;
using UnityEngine.Events;
using UnityEditor;
using UnityEngine;
using System.Collections;
using UnityEngine.TestTools;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEngine.Experimental.Rendering
{
	public class SetupSceneForRenderPipelineTest : MonoBehaviour, IMonoBehaviourTest
	{
		private RenderPipelineAsset m_OriginalAsset;
	    public RenderPipelineAsset[] renderPipelines;
        public Camera cameraToUse;
		public bool hdr = false;
	    public int msaaSamples = 1;

		public int width = 1280;
		public int height = 720;

        public UnityEvent thingToDoBeforeTest;

        [Header("Run Play Mode for test")]
        public int forcedFrameRate = 60;
        public int waitForFrames = 30;
        int waitedFrames = 0;
        bool _readyForCapture = false;
        public bool readyForCapture {  get { return _readyForCapture; } }

        public void Update()
        {
            if ( waitedFrames < waitForFrames )
            {
                ++waitedFrames;
            }
            else if (!_readyForCapture)
            {
                _readyForCapture = true;
            }
        }

	    public void Setup()
	    {
	        m_OriginalAsset = GraphicsSettings.renderPipelineAsset;
            Setup(0);
        }

        public void Setup(int index)
		{
		    if (m_OriginalAsset != renderPipelines[index])
		    {
                //Debug.Log("Set Render Pipeline: "+ renderPipelines[index]);
		        GraphicsSettings.renderPipelineAsset = renderPipelines[index];

                // Update Camera Frame Settings (HDRP)
                HDAdditionalCameraData additionalCameraData = cameraToUse.gameObject.GetComponent<HDAdditionalCameraData>();
                if (additionalCameraData != null)
                {
                    HDRenderPipelineAsset m_Asset = (HDRenderPipelineAsset) renderPipelines[index];
                    /*

                    FrameSettings srcFrameSettings;

                    additionalCameraData.UpdateDirtyFrameSettings(true, m_Asset.GetFrameSettings());
                    srcFrameSettings = additionalCameraData.GetFrameSettings();

                    FrameSettings m_FrameSettings = new FrameSettings();

                    // Get the effective frame settings for this camera taking into account the global setting and camera type
                    FrameSettings.InitializeFrameSettings(cameraToUse, m_Asset.GetRenderPipelineSettings(), srcFrameSettings, ref m_FrameSettings);
                    */

                    additionalCameraData.UpdateDirtyFrameSettings(true, m_Asset.GetFrameSettings() );
                }
            }
        }

		public void TearDown()
		{
            if (GraphicsSettings.renderPipelineAsset != m_OriginalAsset)
            {
                GraphicsSettings.renderPipelineAsset = m_OriginalAsset;
            }

            //EditorApplication.isPaused = false;
            //EditorApplication.isPlaying = false;
		}

        public bool IsTestFinished
        {
            get
            {
                return _readyForCapture;
            }
        }
	}
}
