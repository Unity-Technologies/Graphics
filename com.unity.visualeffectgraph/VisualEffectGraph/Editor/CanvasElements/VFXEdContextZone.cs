using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    public class VFXEdContextZone : CanvasElement
    {
        // TODO : Add logic to group same nodes into context zone
    }

    public enum VFXEdContext {
        None,
        Trigger,
        Initialize,
        Update,
        Output
    }
}


