
using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Rendering.LWRP
{
    // This is to keep the namespace UnityEditor.Rendering.LWRP alive,
    // so that user scripts that have "using UnityEditor.Rendering.LWRP" in them still compile.
    internal class Deprecated
    {
    }
}

namespace UnityEditor.Rendering.Universal
{
    [Obsolete("ForwardRendererDataEditor has been deprecated. Use StandardRendererDataEditor instead (UnityUpgradable) -> StandardRendererDataEditor", true)]
    [MovedFrom("UnityEditor.Rendering.LWRP")]
    public class ForwardRendererDataEditor : ScriptableRendererDataEditor
    {
    	public override void OnInspectorGUI()
    	{
    		throw new NotSupportedException("ForwardRendererDataEditor has been deprecated. Use StandardRendererDataEditor instead");
    	}
    }
}
