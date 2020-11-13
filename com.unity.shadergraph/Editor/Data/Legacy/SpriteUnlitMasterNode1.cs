using System;
using System.Collections.Generic;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Legacy
{
    [FormerName("UnityEditor.Experimental.Rendering.Universal.SpriteUnlitMasterNode")]
    [FormerName("UnityEditor.Experimental.Rendering.LWRP.SpriteUnlitMasterNode")]
    class SpriteUnlitMasterNode1 : AbstractMaterialNode, IMasterNode1
    {
        public string m_ShaderGUIOverride;
        public bool m_OverrideEnabled;
    }
}
