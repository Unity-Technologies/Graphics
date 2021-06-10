using System;
using System.Collections.Generic;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Legacy
{
    [FormerName("UnityEditor.Experimental.Rendering.Universal.SpriteLitMasterNode")]
    class SpriteLitMasterNode1 : AbstractMaterialNode, IMasterNode1
    {
        public string m_ShaderGUIOverride;
        public bool m_OverrideEnabled;
    }
}
