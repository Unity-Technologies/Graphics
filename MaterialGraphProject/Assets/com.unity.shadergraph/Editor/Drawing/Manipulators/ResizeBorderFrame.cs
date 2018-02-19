using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.ShaderGraph.Drawing;


public class ResizeBorderFrame : VisualElement
{
    List<ResizeSideHandle> m_ResizeSideHandles;

    bool m_StayWithinParentBounds;

    public bool stayWithinParentBounds
    {
        get { return m_StayWithinParentBounds; }
        set
        {
            m_StayWithinParentBounds = value;
            foreach (ResizeSideHandle resizeHandle in m_ResizeSideHandles)
            {
                resizeHandle.stayWithinParentBounds = value;
            }
        }
    }

    bool m_MaintainApsectRatio;

    public bool maintainAspectRatio
    {
        get { return m_MaintainApsectRatio; }
        set
        {
            m_MaintainApsectRatio = value;
            foreach (ResizeSideHandle resizeHandle in m_ResizeSideHandles)
            {
                resizeHandle.maintainAspectRatio = value;
            }
        }
    }

    public Action OnResizeFinished;

    public ResizeBorderFrame(VisualElement target)
    {
        pickingMode = PickingMode.Ignore;

        AddToClassList("reszieBorderFrame");

        m_ResizeSideHandles = new List<ResizeSideHandle>();

        m_ResizeSideHandles.Add(new ResizeSideHandle(target, ResizeHandleAnchor.TopLeft));
        m_ResizeSideHandles.Add(new ResizeSideHandle(target, ResizeHandleAnchor.Top));
        m_ResizeSideHandles.Add(new ResizeSideHandle(target, ResizeHandleAnchor.TopRight));
        m_ResizeSideHandles.Add(new ResizeSideHandle(target, ResizeHandleAnchor.Right));
        m_ResizeSideHandles.Add(new ResizeSideHandle(target, ResizeHandleAnchor.BottomRight));
        m_ResizeSideHandles.Add(new ResizeSideHandle(target, ResizeHandleAnchor.Bottom));
        m_ResizeSideHandles.Add(new ResizeSideHandle(target, ResizeHandleAnchor.BottomLeft));
        m_ResizeSideHandles.Add(new ResizeSideHandle(target, ResizeHandleAnchor.Left));

        foreach (ResizeSideHandle resizeHandle in m_ResizeSideHandles)
        {
            resizeHandle.OnResizeFinished += HandleResizefinished;
            Add(resizeHandle);
        }
    }

    void HandleResizefinished()
    {
        if (OnResizeFinished != null)
        {
            OnResizeFinished();
        }
    }
}
