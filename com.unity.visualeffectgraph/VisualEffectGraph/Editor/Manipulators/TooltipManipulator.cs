using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    public class TooltipManipulator : IManipulate
    {
        public delegate List<string> GetTooltipCallback();
        private Vector2 m_Position;
        private List<string> m_Text;
        private GetTooltipCallback m_Callback;
        private bool m_bVisible;
        private Canvas2D m_Canvas;
        private PopupWindow m_PopupWindow;
        public TooltipManipulator(GetTooltipCallback callback)
        {
            m_bVisible = false;
            m_Callback = callback;
        }

        public void AttachTo(CanvasElement e)
        {
            e.MouseDown += E_MouseDown;
            e.MouseUp += E_MouseUp;

        }

        public void ShowTooltip(Canvas2D canvas)
        {
            m_bVisible = true;
            m_Text = m_Callback.Invoke();
            canvas.OnOverlay += RenderTooltip;
            canvas.MouseUp += E_MouseUp;
            canvas.AllEvents += E_MouseUpOutsideClientArea;
            canvas.Invalidate();
        }

        public void HideTooltip(Canvas2D canvas)
        {
            m_bVisible = false;
            canvas.OnOverlay -= RenderTooltip;
            canvas.MouseUp -= E_MouseUp;
            canvas.AllEvents -= E_MouseUpOutsideClientArea;
            canvas.Invalidate();
        }

        private bool E_MouseUpOutsideClientArea(CanvasElement element, Event e, Canvas2D parent)
        {
            if(!parent.clientRect.Contains(e.mousePosition))
            {
                HideTooltip(parent);
                e.Use();
                return true;
            }
            return false;
        }

        private bool E_MouseUp(CanvasElement element, Event e, Canvas2D parent)
        {
           if (e.button != 2)
                return false;

            if(m_bVisible)
            {
                HideTooltip(parent);
                e.Use();
            }

            return false;
        }

        private bool E_MouseDown(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.button != 2)
                return false;
            m_Position = e.mousePosition;
            ShowTooltip(parent);
            e.Use();
            return true;
        }

        public bool GetCaps(ManipulatorCapability cap)
        {
            return false;
        }

        private bool RenderTooltip(CanvasElement element, Event e, Canvas2D parent)
        {
            Color backup = GUI.color;
            int numlines = m_Text.Count;
            int lineheight = 16;

            float width = 40;
            foreach(string s in m_Text)
            {
                width = Mathf.Max(width, 24 + (s.Length * 8));
            }

            GUI.color = Color.white;
            Vector2 size = new Vector2(width, numlines * lineheight);
            Rect tooltipRect = new Rect(m_Position + new Vector2(-12,-12), size + new Vector2(24,24));

            if(parent.clientRect.x + parent.clientRect.width < tooltipRect.x + tooltipRect.width)
                tooltipRect.x = (parent.clientRect.x + parent.clientRect.width) - tooltipRect.width;

            if(parent.clientRect.y + parent.clientRect.height < tooltipRect.y + tooltipRect.height)
                tooltipRect.y = (parent.clientRect.y + parent.clientRect.height) - tooltipRect.height;

            GUI.Box(tooltipRect, "", VFXEditor.styles.Tooltip);

            Rect currentLineRect = new Rect(tooltipRect.x + 12, tooltipRect.y + 12, size.x, lineheight);
            foreach(string s in m_Text)
            {
                if (s != "---")
                    GUI.Label(currentLineRect, s, VFXEditor.styles.TooltipText);
                else
                    GUI.Box(currentLineRect, "" , VFXEditor.styles.TooltipLineBreak);

                currentLineRect.y += lineheight;
            }

            GUI.color = backup;
            return true;
        }


    }
}
