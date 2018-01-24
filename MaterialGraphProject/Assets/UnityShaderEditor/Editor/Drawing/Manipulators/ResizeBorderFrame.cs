using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.ShaderGraph.Drawing;


public class ResizeBorderFrame : VisualElement
{
    public Action OnResizeFinished;

    public ResizeBorderFrame(VisualElement target)
    {
        pickingMode = PickingMode.Ignore;

        AddToClassList("reszieBorderFrame");

        ResizeSideHandle topLeft =  new ResizeSideHandle(target, ResizeHandleAnchor.TopLeft);
        ResizeSideHandle top =  new ResizeSideHandle(target, ResizeHandleAnchor.Top);
        ResizeSideHandle topRight =  new ResizeSideHandle(target, ResizeHandleAnchor.TopRight);
        ResizeSideHandle right = new ResizeSideHandle(target, ResizeHandleAnchor.Right);
        ResizeSideHandle bottomRight = new ResizeSideHandle(target, ResizeHandleAnchor.BottomRight);
        ResizeSideHandle bottom = new ResizeSideHandle(target, ResizeHandleAnchor.Bottom);
        ResizeSideHandle bottomLeft = new ResizeSideHandle(target, ResizeHandleAnchor.BottomLeft);
        ResizeSideHandle left = new ResizeSideHandle(target, ResizeHandleAnchor.Left);

        topLeft.OnResizeFinished += HandleResizefinished;
        top.OnResizeFinished += HandleResizefinished;
        topRight.OnResizeFinished += HandleResizefinished;
        right.OnResizeFinished += HandleResizefinished;
        bottomRight.OnResizeFinished += HandleResizefinished;
        bottom.OnResizeFinished += HandleResizefinished;
        bottomLeft.OnResizeFinished += HandleResizefinished;
        left.OnResizeFinished += HandleResizefinished;

        Add(topLeft);
        Add(top);
        Add(topRight);
        Add(right);
        Add(bottomRight);
        Add(bottom);
        Add(bottomLeft);
        Add(left);
    }

    void HandleResizefinished()
    {
        if (OnResizeFinished != null)
        {
            OnResizeFinished();
        }
    }
}
