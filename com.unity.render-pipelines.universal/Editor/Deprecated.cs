using System;
using System.Linq;
using UnityEngine;
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
    [Obsolete("ForwardRendererDataEditor has been deprecated. Use UniversalRendererDataEditor instead (UnityUpgradable) -> UniversalRendererDataEditor", true)]
    [MovedFrom("UnityEditor.Rendering.LWRP")]
    public class ForwardRendererDataEditor : ScriptableRendererDataEditor
    {
        public override void OnInspectorGUI()
        {
            throw new NotSupportedException("ForwardRendererDataEditor has been deprecated. Use UniversalRendererDataEditor instead");
        }
    }

    static partial class EditorUtils
    {
    }
}

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    public static partial class SimpleLitGUI
    {
        [Obsolete("This is obsolete, use SmoothnessSource instead, from BaseShaderGUI.cs", false)]
        public enum SmoothnessMapChannel
        {
            SpecularAlpha,
            AlbedoAlpha,
        }
    }


    public static partial class LitGUI
    {
        [Obsolete("This is obsolete, use SmoothnessSource instead", false)]
        public enum SmoothnessMapChannel
        {
            SpecularMetallicAlpha,
            AlbedoAlpha,
        }

        [Obsolete("This is obsolete, use GetSmoothnessSource instead", false)]
        public static SmoothnessMapChannel GetSmoothnessMapChannel(Material material)
        {
            int ch = (int)material.GetFloat("_SmoothnessTextureChannel");
            if (ch == (int)SmoothnessSource.AlbedoAlpha)
                return SmoothnessMapChannel.AlbedoAlpha;

            return SmoothnessMapChannel.SpecularMetallicAlpha;
        }
    }
}
