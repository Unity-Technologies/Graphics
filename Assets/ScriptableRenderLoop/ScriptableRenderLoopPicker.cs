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
		public ScriptableRenderLoop renderloop
		{
			get { return m_RenderLoop; }
			set { m_RenderLoop = value; }
		}

		[SerializeField]
		private ScriptableRenderLoop m_RenderLoop;

		void OnEnable ()
		{
			RenderLoop.renderLoopDelegate += Render;

			SyncRenderingFeatures();
		}

		void OnValidate()
		{
			SyncRenderingFeatures();
		}

		void SyncRenderingFeatures ()
		{
#if UNITY_EDITOR
			if (m_RenderLoop != null && isActiveAndEnabled)
				UnityEditor.SupportedRenderingFeatures.active = m_RenderLoop.GetSupportedRenderingFeatures();
			else
				UnityEditor.SupportedRenderingFeatures.active = UnityEditor.SupportedRenderingFeatures.Default;
#endif
		}

		void OnDisable ()
		{
			RenderLoop.renderLoopDelegate -= Render;

			#if UNITY_EDITOR
			UnityEditor.SupportedRenderingFeatures.active = UnityEditor.SupportedRenderingFeatures.Default;
			#endif
		}

		bool Render(Camera[] cameras, RenderLoop loop)
		{
			if (m_RenderLoop == null)
				return false;

			m_RenderLoop.Render (cameras, loop);
			return true;
		}
	}
}