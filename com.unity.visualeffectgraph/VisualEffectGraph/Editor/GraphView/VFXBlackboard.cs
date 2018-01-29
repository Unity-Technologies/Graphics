using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEditor.VFX;
using System.Collections.Generic;
using UnityEditor;

namespace  UnityEditor.VFX.UI
{
    class VFXBlackboard : Blackboard
    {
        Blackboard m_Blackboard;

        public Blackboard blackboard { get { return m_Blackboard; } }

        public VFXBlackboard()
        {
            m_Blackboard = new Blackboard();
        }
    }
}
