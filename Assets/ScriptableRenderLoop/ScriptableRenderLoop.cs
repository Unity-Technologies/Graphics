using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

namespace UnityEngine.ScriptableRenderLoop
{
	public abstract class ScriptableRenderLoop : ScriptableObject
	{
		public abstract void Render(Camera[] cameras, RenderLoop renderLoop);
	}
}