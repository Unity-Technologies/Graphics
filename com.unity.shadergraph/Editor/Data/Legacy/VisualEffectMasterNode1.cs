using System;
using System.Collections.Generic;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Legacy
{
    [FormerName("UnityEditor.ShaderGraph.VfxMasterNode")]
    class VisualEffectMasterNode1 : AbstractMaterialNode, IMasterNode1
    {
        public bool m_Lit;
        public bool m_AlphaTest;
    }
}
