using System;
using System.Collections.Generic;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Legacy
{
    [Serializable, FormerName("UnityEditor.Experimental.Rendering.Universal.SpriteUnlitMasterNode ")]
    class SpriteUnlitMasterNode1 : AbstractMaterialNode, IMasterNode
    {
        public string m_ShaderGUIOverride;
        public bool m_OverrideEnabled;
    }
}
