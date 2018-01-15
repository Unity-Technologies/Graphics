using UnityEngine.Rendering;
using UnityEngine.Events;
using UnityEditor;
using UnityEngine;
using System.Collections;
using UnityEngine.TestTools;

namespace UnityEngine.Experimental.Rendering
{
	public class SetupSceneForRenderPipelineTest : MonoBehaviour, IMonoBehaviourTest
	{
		private RenderPipelineAsset m_OriginalAsset;
		public RenderPipelineAsset renderPipeline;
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
            if (m_OriginalAsset != renderPipeline) GraphicsSettings.renderPipelineAsset = renderPipeline;
		}

		public void TearDown()
		{
			if ( GraphicsSettings.renderPipelineAsset != m_OriginalAsset ) GraphicsSettings.renderPipelineAsset = m_OriginalAsset;

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
