using System;
using UnityEngine;
using UnityEditor.Experimental.Rendering.LWRP.Path2D.GUIFramework;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal interface IShapeEditorDriver
    {
        IShapeEditorController controller { get; set; }
        void Install(GUISystem guiSystem);
        void Uninstall(GUISystem guiSystem);
    }
}
