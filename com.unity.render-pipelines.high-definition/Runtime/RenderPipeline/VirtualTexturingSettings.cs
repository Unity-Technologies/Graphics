using System;
using System.IO;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "VirtualTexturing - Settings" + Documentation.endURL)]
    public sealed class VirtualTexturingSettings : ScriptableObject
    {
        public UnityEngine.Experimental.Rendering.VirtualTexturingSettings settings = new Experimental.Rendering.VirtualTexturingSettings();
    }
}
