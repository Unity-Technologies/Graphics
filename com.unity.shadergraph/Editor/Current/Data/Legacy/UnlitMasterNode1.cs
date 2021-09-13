using System;
using System.Collections.Generic;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Legacy
{
    [FormerName("UnityEditor.ShaderGraph.UnlitMasterNode")]
    class UnlitMasterNode1 : AbstractMaterialNode, IMasterNode1
    {
        public enum SurfaceType
        {
            Opaque,
            Transparent
        }

        public enum AlphaMode
        {
            Alpha,
            Premultiply,
            Additive,
            Multiply
        }

        public SurfaceType m_SurfaceType;
        public AlphaMode m_AlphaMode;
        public bool m_TwoSided;
        public string m_ShaderGUIOverride;
        public bool m_OverrideEnabled;
    }
}
