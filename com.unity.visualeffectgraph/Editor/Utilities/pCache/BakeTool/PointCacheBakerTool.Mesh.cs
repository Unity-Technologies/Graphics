using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEditor.VFX.Utils
{
    public partial class PointCacheBakeTool : EditorWindow
    {
        Mesh m_Mesh;
        
        string m_ProgressBar_Title = "pCache bake tool";
        string m_ProgressBar_CapturingData = "Capturing data...";
        string m_ProgressBar_SaveFile = "Saving pCache file";
    }
}