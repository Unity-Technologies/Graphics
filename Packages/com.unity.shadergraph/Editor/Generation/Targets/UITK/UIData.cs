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
    }
}
