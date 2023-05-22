using UnityEditor.ShaderGraph;
using UnityEngine;
using static UnityEditor.Rendering.BuiltIn.ShaderUtils;
using UnityEditor.Rendering.BuiltIn;
using System;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Canvas.ShaderGraph
{
    internal class CanvasData : JsonObject
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

        [SerializeField]
        bool m_AlphaClip = false;
        public bool alphaClip
        {
            get => m_AlphaClip;
            set => m_AlphaClip = value;
        }
    }
}
