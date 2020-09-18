#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Deprecated, kept for backward compatibility with existing ForwardRendererData asset files.
    /// Use StandardRendererData instead.
    /// </summary>
    [System.Obsolete("ForwardRendererData has been deprecated. Use StandardRendererData instead (UnityUpgradable) -> StandardRendererData", true)]
    [Serializable, ReloadGroup, ExcludeFromPreset]
    public class ForwardRendererData : StandardRendererData
    {
    }
}
