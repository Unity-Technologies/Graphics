using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D.GUIFramework
{
    internal class GUISystem
    {
        private readonly int kControlIDCheckHashCode = "ControlIDCheckHashCode".GetHashCode();

        private List<Control> m_Controls = new List<Control>();
        private List<GUIAction> m_Actions = new List<GUIAction>();
        private IGUIState m_GUIState;
        private int m_PrevNearestControl = -1;
        private LayoutData m_PrevNearestLayoutData = LayoutData.zero;
        private int m_ControlIDCheck = -1;

        public GUISystem(IGUIState guiState)
        {
            m_GUIState = guiState;
        }

        public void AddControl(Control control)
        {
            if (control == null)
                throw new NullReferenceException("Control is null");

            m_Controls.Add(control);
        }

        public void RemoveControl(Control control)
        {
            m_Controls.Remove(control);
        }

        public void AddAction(GUIAction action)
        {
            if (action == null)
                throw new NullReferenceException("Action is null");

            m_Actions.Add(action);
        }

        public void RemoveAction(GUIAction action)
        {
            m_Actions.Remove(action);
        }

        public void OnGUI()
        {
            var controlIDCheck = m_GUIState.GetControlID(kControlIDCheckHashCode, FocusType.Passive);

            if (m_GUIState.eventType == EventType.Layout)
                m_ControlIDCheck = controlIDCheck;
            else if (m_GUIState.eventType != EventType.Used && m_ControlIDCheck != controlIDCheck)
                Debug.LogWarning("GetControlID at event " + m_GUIState.eventType + " returns a controlID different from the one in Layout event");
                
            var nearestLayoutData = LayoutData.zero;

            foreach (var control in m_Controls)
                control.GetControl(m_GUIState);

            if (m_GUIState.eventType == EventType.Layout)
            {
                foreach (var control in m_Controls)
                    control.BeginLayout(m_GUIState);

                foreach (var control in m_Controls)
                {
                    control.Layout(m_GUIState);
                    nearestLayoutData = LayoutData.Nearest(nearestLayoutData, control.layoutData);
                }

                foreach (var control in m_Controls)
                    m_GUIState.AddControl(control.ID, control.layoutData.distance);

                foreach (var control in m_Controls)
                    control.EndLayout(m_GUIState);

                if (m_PrevNearestControl == m_GUIState.nearestControl)
                {
                    if (nearestLayoutData.index != m_PrevNearestLayoutData.index)
                        m_GUIState.Repaint();
                }
                else
                {
                    m_PrevNearestControl = m_GUIState.nearestControl;
                    m_GUIState.Repaint();
                }

                m_PrevNearestLayoutData = nearestLayoutData;
            }

            if (m_GUIState.eventType == EventType.Repaint)
            {
                foreach (var action in m_Actions)
                    if (action.IsRepaintEnabled(m_GUIState))
                        action.PreRepaint(m_GUIState);

                foreach (var control in m_Controls)
                    control.Repaint(m_GUIState);
            }

            var repaintOnMouseMove = false;

            foreach (var action in m_Actions)
            {
                if (IsMouseMoveEvent())
                    repaintOnMouseMove |= action.IsRepaintOnMouseMoveEnabled(m_GUIState);

                action.OnGUI(m_GUIState);
            }

            if (repaintOnMouseMove)
                m_GUIState.UseCurrentEvent();
        }

        private bool IsMouseMoveEvent()
        {
            return m_GUIState.eventType == EventType.MouseMove || m_GUIState.eventType == EventType.MouseDrag;
        }
    }
}
