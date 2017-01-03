using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.ScriptableRenderPipeline
{
    public abstract class RenderPipeline : BaseRenderPipeline
    {
        private ICameraProvider m_CameraProvider;

        public override ICameraProvider cameraProvider
        {
            get
            {
                if (m_CameraProvider == null)
                    m_CameraProvider = ConstructCameraProvider();

                return m_CameraProvider;
            }
            set { m_CameraProvider = value; }
        }

        public override ICameraProvider ConstructCameraProvider()
        {
            return new DefaultCameraProvider();
        }

        public static void CleanCameras(IEnumerable<Camera> cameras)
        {
            foreach (var camera in cameras)
                camera.ClearIntermediateRenderers();
        }
    }
}
