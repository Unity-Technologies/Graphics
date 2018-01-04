using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.ShaderGraph.Drawing;


public class ResizeBorderFrame : VisualElement
{
    public ResizeBorderFrame(VisualElement target)
    {
        pickingMode = PickingMode.Ignore;

        AddToClassList("reszieBorderFrame");

        Add(new ResizeSideHandle(target, ResizeHandleAnchor.TopLeft));
        Add(new ResizeSideHandle(target, ResizeHandleAnchor.Top));
        Add(new ResizeSideHandle(target, ResizeHandleAnchor.TopRight));
        Add(new ResizeSideHandle(target, ResizeHandleAnchor.Right));
        Add(new ResizeSideHandle(target, ResizeHandleAnchor.BottomRight));
        Add(new ResizeSideHandle(target, ResizeHandleAnchor.Bottom));
        Add(new ResizeSideHandle(target, ResizeHandleAnchor.BottomLeft));
        Add(new ResizeSideHandle(target, ResizeHandleAnchor.Left));
    }
}
