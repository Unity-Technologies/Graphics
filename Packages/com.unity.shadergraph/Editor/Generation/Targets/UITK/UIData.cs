using UnityEditor.ShaderGraph;
using UnityEngine;
using static UnityEditor.Rendering.BuiltIn.ShaderUtils;
using UnityEditor.Rendering.BuiltIn;
using System;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.UITK.ShaderGraph
{
    internal class UIData : JsonObject
    {
        public enum Version
        {
            Initial,
        }

        [SerializeField] Version m_Version = Version.Initial;
        public Version version
        {
            get => m_Version;
            set => m_Version = value;
        }

        // When true (default), uie_custom_frag multiplies the final alpha by the per-element opacity.
        // When false, the author applies opacity instead (e.g. via the Element Color node's Opacity output).
        [SerializeField] bool m_AutomaticOpacity = true;
        public bool automaticOpacity
        {
            get => m_AutomaticOpacity;
            set => m_AutomaticOpacity = value;
        }
    }
}
