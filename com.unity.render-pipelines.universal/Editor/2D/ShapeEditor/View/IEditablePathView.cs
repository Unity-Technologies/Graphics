using System;
using UnityEngine;
using UnityEditor.Rendering.Universal.Path2D.GUIFramework;

namespace UnityEditor.Rendering.Universal.Path2D
{
    internal interface IEditablePathView
    {
        IEditablePathController controller { get; set; }
        void Install(GUISystem guiSystem);
        void Uninstall(GUISystem guiSystem);
    }
}
