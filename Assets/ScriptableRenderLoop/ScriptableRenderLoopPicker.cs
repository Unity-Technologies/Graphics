using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System;

namespace UnityEngine.ScriptableRenderLoop
{
	//@TODO: This should be moved into GraphicsSettings
	[ExecuteInEditMode]
	public class ScriptableRenderLoopPicker : MonoBehaviour
	{
		public ScriptableRenderLoop renderLoop;

		void OnEnable ()
		{
			RenderLoop.renderLoopDelegate += Render;
		}

		void OnDisable ()
		{
			RenderLoop.renderLoopDelegate -= Render;
		}

		bool Render(Camera[] cameras, RenderLoop loop)
		{
			if (renderLoop == null)
				return false;

			renderLoop.Render (cameras, loop);
			return true;
		}

	}
}