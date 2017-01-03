using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.ScriptableRenderPipeline
{
	public class DefaultCameraProvider : ICameraProvider
	{
		public Camera overrideCamera { get; set; }

		public static void GetCamerasToRenderDefault(List<Camera> cameras)
		{
			cameras.Clear();
			foreach (var c in Camera.allCameras)
			{
				if (c.enabled)
					cameras.Add(c);
			}
		}

		public void GetCamerasToRender(List<Camera> cameras)
		{
			if (overrideCamera != null)
			{
				cameras.Clear();
				cameras.Add(overrideCamera);
			}
			else
			{
				GetCamerasToRenderDefault(cameras);
			}
		}
	}
}
